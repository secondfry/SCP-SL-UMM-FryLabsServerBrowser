using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using Ping = System.Net.NetworkInformation.Ping;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Networking;

namespace FryLabsServerList
{
  public class SortServerDataClass : IComparer<ServerData>
  {
    int IComparer<ServerData>.Compare(ServerData a, ServerData b)
    {
      return (int)(a.Ping - b.Ping);
    }
  }

  class ServerList
  {
    // Endpoint
    public static Uri endpointLobby = new Uri("https://api.scpslgame.com/lobbylist.php");

    // Servers
    private static string _serversRaw;
    public static List<ServerData> servers = new List<ServerData>();

    // Ping
    private const int PING_THRESHOLD = 500;
    private const int SHOW_THRESHOLD = 500;
    private static List<Ping> pings = new List<Ping>();

    // etc
    private static Stopwatch _stopwatch = new Stopwatch();
    private static int counter = 0;

    public static void Search()
    {
      UI.status = "Requesting server data...";

      ServerList.servers.Clear();
      ServerList.pings.Clear();
      ServerList.counter = 0;

      using (var client = new WebClient())
      {
        client.DownloadStringCompleted += (s, e) => {
          if (e.Error != null)
          {
            UI.status = "Requesting server data... Error!";
            return;
          }

          UI.status = "Requesting server data... Complete!";
          ServerList._serversRaw = e.Result;
          ServerList.ServersParse();
        };
        client.DownloadStringAsync(ServerList.endpointLobby);
      }
    }

    private static void ServersParse()
    {
      // Prepare ping
      string pingData = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
      byte[] pingBuffer = Encoding.ASCII.GetBytes(pingData);
      PingOptions pingOptions = new PingOptions(64, true);

      var strings = ServerList._serversRaw.Split(new string[] { "<br>" }, StringSplitOptions.RemoveEmptyEntries);
      foreach (var data in strings)
      {
        var parts = data.Split(';');

        var ret = new ServerData();
        ret.counter = ServerList.counter;
        ServerList.counter++;

        ret.IP = parts[0];
        ret.port = int.Parse(parts[1]);

        var fav = false;
        // try
        // {
        //   if (this._window.favourites.TryGetValue(ret.Address, out ServerData cache))
        //   {
        //     fav = cache.IsFavourite;
        //   };
        // }
        // catch (NullReferenceException e) { }
        ret.IsFavourite = fav;

        ret.serverInfo = ServerInfo.Parse(parts[2]);
        ret.serverPlayers = ServerPlayers.Parse(parts[3]);

        // ServerList.PingTCP(ret, ServerList.PING_THRESHOLD);
        ServerList.PingICMP(ret, ServerList.PING_THRESHOLD, pingBuffer, pingOptions);

        Thread.Sleep(0);
      }
    }

    private static void PingStop()
    {
      foreach (var p in ServerList.pings)
      {
        p.SendAsyncCancel();
      }
    }

    private static void PingICMP(ServerData ret, int pingTimeout, byte[] pingBuffer, PingOptions pingOptions)
    {
      using (var ping = new Ping())
      {
        ServerList.pings.Add(ping);

        ping.PingCompleted += (s, e) => {
          if (e.Cancelled || e.Error != null)
          {
            ((AutoResetEvent)e.UserState).Set();
            return;
          }

          if (e.Reply.Status == IPStatus.TimedOut)
          {
            ret.pingICMP = pingTimeout;
          }
          else
          {
            ret.pingICMP = e.Reply.RoundtripTime;
          }

          if (ret.pingICMP < ServerList.SHOW_THRESHOLD)
          {
            ServerList.servers.Add(ret);
            ServerList.ShakeData();
          }
          else
          {
            Console.WriteLine(String.Format(
              "[P {0}][IP:p {1}:{2}] Server is out of reach. Ping: {3}",
              ret.Project,
              ret.IP,
              ret.port,
              ret.pingICMP
            ));
          }
          
          ((AutoResetEvent)e.UserState).Set();
        };

        AutoResetEvent pingWaiter = new AutoResetEvent(false);
        try
        {
          ping.SendAsync(IPAddress.Parse(ret.IP), pingTimeout, pingBuffer, pingOptions, pingWaiter);
          return;
        }
        catch (FormatException e) {
          // server IP was probably server NS
          // we will retry call directly
        }
        catch (SocketException e)
        {
          Console.WriteLine(e.ToString());
          return;
        }

        try
        {
          ping.SendAsync(ret.IP, pingTimeout, pingBuffer, pingOptions, pingWaiter);
          return;
        }
        catch (SocketException e)
        {
          Console.WriteLine(e.ToString());
          return;
        }
      }
    }

    // FIXME IDK why this doesn't work
    private static void PingTCP(ServerData ret, int pingTimeout)
    {
      var times = new List<double>();

      for (int i = 0; i < 4; i++)
      {
        using (var sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
        {
          sock.Blocking = true;
          sock.SendTimeout = pingTimeout;
          sock.ReceiveTimeout = pingTimeout;
          bool failed = false;

          ServerList._stopwatch.Reset();
          ServerList._stopwatch.Start();
          try
          {
            sock.Connect(ret.IP, ret.port);
          }
          catch (SocketException e)
          {
            failed = true;
          }
          ServerList._stopwatch.Stop();

          double time = failed ? 500 : ServerList._stopwatch.ElapsedMilliseconds;
          times.Add(time);

          sock.Close();

          Thread.Sleep(0);
        }
      }

      ret.pingTCP = times.Average();
      if (ret.pingTCP < ServerList.SHOW_THRESHOLD)
      {
        ServerList.servers.Add(ret);
        ServerList.ShakeData();
      }
      else
      {
        Console.WriteLine(String.Format(
          "[P {0}][IP:p {1}:{2}] Server is out of reach. Ping: {3}",
          ret.Project,
          ret.IP,
          ret.port,
          ret.pingICMP
        ));
      }
    }

    public static void ShakeData()
    {
      ServerList.servers.Sort((a, b) => {
        var diffPing = (int)(a.Ping - b.Ping);
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

      if (ServerList.servers.Count > 100)
      {
        ServerList.PingStop();
        ServerList.servers.RemoveRange(100, ServerList.servers.Count - 100);
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
