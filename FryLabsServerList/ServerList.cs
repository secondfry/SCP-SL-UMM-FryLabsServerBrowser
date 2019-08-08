using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FryLabsServerList
{
  class ServerList
  {
    // Endpoint
    public static Uri endpointLobby = new Uri("https://api.scpslgame.com/lobbylist.php");

    // Servers
    private static string _serversRaw;
    public static Dictionary<string, ServerData> serversDictionary = new Dictionary<string, ServerData>();
    public static List<ServerData> serversListProcessed = new List<ServerData>();

    // Ping
    private const int PING_THRESHOLD = 500;
    private const int SHOW_THRESHOLD = 150;
    private static List<Task> unityPingTasks = new List<Task>();
    private static CancellationTokenSource cts;

    // etc
    private static Stopwatch _stopwatch = new Stopwatch();
    private static int counter = 0;

    public static void Search()
    {
      UI.status = "Requesting server data...";

      ServerList.serversDictionary.Clear();
      ServerList.serversListProcessed.Clear();
      ServerList.unityPingTasks.Clear();

      ServerList.counter = 0;

      using (var client = new WebClient())
      {
        client.DownloadStringCompleted += (s, e) =>
        {
          if (e.Error != null)
          {
            UI.status = "Requesting server data... Error!";
            return;
          }

          UI.status = "Requesting server data... Complete!";
          ServerList._serversRaw = e.Result;
          ServerList.AddHardcoded();
          ServerList.ServersParse();
          ServerList.ServersPingCheck();
        };
        client.DownloadStringAsync(ServerList.endpointLobby);
      }
    }

    private static async void ServersParse()
    {
      var strings = ServerList._serversRaw.Split(new string[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
      foreach (var data in strings)
      {
        var parts = data.Split(';');

        var sData = new ServerData();
        sData.counter = ServerList.counter;
        ServerList.counter++;

        sData.IP = parts[0];
        sData.port = int.Parse(parts[1]);

        var fav = false;
        // TODO
        // try
        // {
        //   if (this._window.favourites.TryGetValue(sData.Address, out ServerData cache))
        //   {
        //     fav = cache.IsFavourite;
        //   };
        // }
        // catch (NullReferenceException e) { }
        sData.IsFavourite = fav;

        sData.serverInfo = ServerInfo.Parse(parts[2]);
        sData.serverPlayers = ServerPlayers.Parse(parts[3]);

        sData.ping = ServerList.PING_THRESHOLD;

        ServerList.serversDictionary.Add(sData.Address, sData);
        await Task.Delay(0);
      }
    }

    private static void AddHardcoded()
    {
      var sInfo = new ServerInfo();
      sInfo.discord = "discord.gg/frylabs";
      sInfo.text = "[RU] Fry Labs [default hardcoded server @ secondfry.ru:2212]";
      var sData = new ServerData();
      sData.counter = ServerList.counter++;
      sData.IP = "secondfry.ru";
      sData.port = 2212;
      sData.serverInfo = sInfo;
      ServerList.serversDictionary.Add(sData.Address, sData);
    }

    private static async void ServersPingCheck()
    {
      ServerList.cts?.Dispose();
      ServerList.cts = new CancellationTokenSource();

      foreach (var server in ServerList.serversDictionary.Values)
      {
        ServerList.PingUnity(server, ServerList.PING_THRESHOLD);
        await Task.Delay(0);
      }
    }

    private static void PingStop()
    {
      ServerList.cts.Cancel();
    }

    private static async void PingUnity(ServerData sData, int pingTimeout)
    {
      string IP = sData.IP;
      try
      {
        IPAddress.Parse(IP);
      }
      catch (FormatException)
      {
        IPHostEntry hostInfo = Dns.GetHostEntry(sData.IP);
        IP = hostInfo.AddressList.First().ToString();
      }

      Task<int> pingTask = ServerList.PingUnityAsync(IP);
      ServerList.unityPingTasks.Add(pingTask);

      try
      {
        sData.ping = await pingTask;
      }
      catch (OperationCanceledException)
      {
        sData.ping = pingTimeout;
      }

      ServerList.serversDictionary.Remove(sData.Address);
      if (sData.ping < ServerList.SHOW_THRESHOLD)
      {
        ServerList.serversListProcessed.Add(sData);
      }
      ServerList.ShakeData();
    }

    private static async Task<int> PingUnityAsync(string IP)
    {
      Ping ping = new Ping(IP);
      while (!ping.isDone)
      {
        await Task.Delay(1000, ServerList.cts.Token);
        ServerList.cts.Token.ThrowIfCancellationRequested();
      }
      return ping.time;
    }

    public static void ShakeData()
    {
      ServerList.serversListProcessed.Sort((a, b) =>
      {
        var diffPing = (int)(a.ping - b.ping);
        if (diffPing != 0)
          return diffPing;

        if (a.serverInfo.discord == "" && b.serverInfo.discord != "")
          return 1;
        if (a.serverInfo.discord != "" && b.serverInfo.discord == "")
          return -1;

        var diffProject = string.Compare(a.serverInfo.discord, b.serverInfo.discord);
        if (diffProject != 0)
        {
          // We are kinda respecting original sort order
          return a.counter - b.counter;
        }

        var diffName = string.Compare(a.Info, b.Info);
        if (diffName != 0)
          return diffName;

        return a.serverPlayers.current - b.serverPlayers.current;
      });

      if (ServerList.serversListProcessed.Count > 100)
      {
        ServerList.PingStop();
        // ServerList.servers.RemoveRange(100, ServerList.servers.Count - 100);
      }
    }

    public static void Discord(ServerData sData)
    {
      var part = sData.Discord;
      if (part == "")
      {
        UI.status = "This server has no discord information in the title :(";
        return;
      }

      Process.Start("https://" + part);
      UI.status = "Opened [" + part + "] in default browser!";
    }

    public static void Connect(ServerData sData)
    {
      if (CrashDetector.Show())
        return;

      CustomNetworkManager customNetworkManager = UnityEngine.Object.FindObjectOfType<CustomNetworkManager>();
      if (NetworkClient.active)
      {
        customNetworkManager.StopClient();
      }
      NetworkServer.Reset();
      CustomNetworkManager.ConnectionIp = sData.IP;
      customNetworkManager.networkAddress = sData.IP;
      customNetworkManager.networkPort = sData.port;
      customNetworkManager.ShowLog(13, string.Empty, string.Empty);
      customNetworkManager.StartClient();
      GameConsole.Console.singleton.AddLog(String.Format("Connecting to {0}:{1}!", sData.IP, sData.port), new Color32(182, 182, 182, byte.MaxValue), false);
    }
  }
}
