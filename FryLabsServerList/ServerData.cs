using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace FryLabsServerList
{
  public struct ServerInfo
  {
    public string text;
    public string pastebin;
    public string versionVanilla;
    public string versionSmod;
    public string discord { get; set; }

    public static ServerInfo Parse(string base64)
    {
      // Base64 decode
      var bytes = System.Convert.FromBase64String(base64);
      var dirty = System.Text.Encoding.UTF8.GetString(bytes);

      // Extract parts
      var parts = dirty.Split(new string[] { ":[:BREAK:]:" }, StringSplitOptions.RemoveEmptyEntries);
      var html = parts[0];
      var pastebin = parts[1];
      var versionVanilla = parts[2];

      // Extracting version info
      // Welcome to this magic – https://regex101.com/r/fDiDGW/1
      var versionInfoMatches = Regex.Matches(html, "<color=#[0-9a-fA-F]{6}00>(?><[^>]*?>)*?([^<]*?)(?></[^>]*?>)*?</color>");
      List<string> versionInfos = new List<string>();
      foreach (Match match in versionInfoMatches)
      {
        versionInfos.Add(match.Groups[1].Value);
      }
      var versionSmod = String.Join(", ", versionInfos);

      // Extracting discord link
      var discordMatch = Regex.Match(html, "discord.(?>me|gg)/[0-9a-zA-Z]+");
      var discord = "";
      if (discordMatch.Groups.Count > 0)
      {
        discord = discordMatch.Groups[0].Value;
      }

      // Removing version info
      var step = Regex.Replace(html, "<color=#[0-9a-fA-F]{6}00>.*?</color>", "");

      // Removing discord link
      step = Regex.Replace(step, "discord.(?>me|gg)/[0-9a-zA-Z]+", "");

      // Removing orphaned brackets
      step = Regex.Replace(step, "\\[\\]", "");

      // Removing all tags
      step = Regex.Replace(step, "<[^>]*?>", "");

      // Replace line breaks
      step = Regex.Replace(step, "[\r ]+", " ");
      var pieces = step.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
      step = String.Join("\n", pieces);

      // Trim
      var clean = step.Trim();

      // Return time!
      var ret = new ServerInfo();
      ret.text = clean;
      ret.pastebin = pastebin;
      ret.versionVanilla = versionVanilla;
      ret.versionSmod = versionSmod;
      ret.discord = discord;
      return ret;
    }
  }

  public struct ServerPlayers
  {
    public int current;
    public int total;

    public static ServerPlayers Parse(string data)
    {
      var parts = data.Split('/');
      return new ServerPlayers
      {
        current = int.Parse(parts[0]),
        total = int.Parse(parts[1])
      };
    }
  }

  public struct ServerData
  {
    public bool IsFavourite { get; set; }
    public string IP { get; set; }
    public int port { get; set; }
    public ServerInfo serverInfo { get; set; }
    public ServerPlayers serverPlayers { get; set; }
    public long pingICMP { get; set; }
    public double pingTCP { get; set; }
    public int counter { get; set; }

    // TODO
    // public bool IsFavourite {
    //   get { return this._isFavourite; }
    //   set {
    //     if (this._isFavourite != value) {
    //      this._isFavourite = value;
    //       NotifyPropertyChanged();
    //     }
    //   }
    // }

    public string Project
    {
      // FIXME add project info via DB or smth
      get { return this.serverInfo.discord != "" ? this.serverInfo.discord : this.IP; }
    }

    public string Address
    {
      get { return this.IP + ':' + this.port; }
    }

    public string Discord
    {
      get { return this.serverInfo.discord; }
    }

    public string Info
    {
      get { return this.serverInfo.text; }
    }

    public string Players
    {
      get { return this.serverPlayers.current + " / " + this.serverPlayers.total; }
    }

    public double Ping
    {
      get { return this.pingTCP != 0 ? this.pingTCP : this.pingICMP; }
    }

    // TODO
    // public event PropertyChangedEventHandler PropertyChanged;

    // TODO
    // private void NotifyPropertyChanged([CallerMemberName] String propertyName = "") {
    //   if (PropertyChanged != null) {
    //     PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
    //   }
    // }
  }
}
