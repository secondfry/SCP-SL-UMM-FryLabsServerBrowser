using System;
using UnityEngine;
using UnityModManagerNet;

namespace FryLabsServerList
{
  class UI : MonoBehaviour
  {
    // Singleton-ish
    internal static bool Load()
    {
      try
      {
        new GameObject(typeof(UI).FullName, typeof(UI));

        return true;
      }
      catch (Exception e)
      {
        Debug.LogException(e);
      }

      return false;
    }

    // Singleton-ish
    private static UI mInstance = null;
    public static UI Instance
    {
      get { return mInstance; }
    }

    // Check if we are opened
    public bool Opened = false;

    // Check if we are allowed
    public bool IsModEnabled = true;

    // Window related stuff
    private static Vector2 mWindowSize = new Vector2(Mathf.Min(960, Screen.width), Mathf.Min(720, Screen.height));
    private Rect mWindowRect = new Rect((Screen.width - mWindowSize.x) / 2f, (Screen.height - mWindowSize.y) / 2f, mWindowSize.x, mWindowSize.y);

    // Real public GUI stuff
    public static string status;
    public static GUIStyle textCenter;
    public static GUIStyle textLeft;
    public static GUIStyle textInfo;
    private static bool isButtonStyleInitialized = false;
    public static GUIStyle button;
    public static GUIStyle separator;
    private static Vector2 scrollPos = Vector2.zero;

    // Called by Unity after creation?
    private void Awake()
    {
      mInstance = this;
      DontDestroyOnLoad(this);

      UI.textCenter = new GUIStyle();
      UI.textCenter.normal.textColor = Color.white;
      UI.textCenter.alignment = TextAnchor.MiddleCenter;

      UI.textLeft = new GUIStyle();
      UI.textLeft.normal.textColor = Color.white;
      UI.textLeft.alignment = TextAnchor.MiddleLeft;

      UI.textInfo = new GUIStyle();
      UI.textInfo.normal.textColor = Color.white;
      UI.textInfo.alignment = TextAnchor.MiddleLeft;
      UI.textInfo.wordWrap = true;

      UI.separator = new GUIStyle();
      UI.separator.border.top = 1;
      UI.separator.margin.top = 1;
      UI.separator.padding.top = 1;
    }

    // Called by Unity in loop?
    private void OnGUI()
    {
      if (!this.IsModEnabled)
        return;

      // BlockGameUI(this.Opened);
      if (!this.Opened)
        return;

      mWindowRect = GUILayout.Window(10, mWindowRect, WindowFunction, "", UnityModManager.UI.window, GUILayout.Width(mWindowSize.x), GUILayout.Height(mWindowSize.y));
    }

    // Called by window in loop?
    private void WindowFunction(int windowId)
    {
      if (Input.GetAxisRaw("Mouse ScrollWheel") != 0)
      {
        UI.scrollPos.y += Input.GetAxisRaw("Mouse ScrollWheel");
      }

      if (!UI.isButtonStyleInitialized)
      {
        UI.button = new GUIStyle(GUI.skin.button);
        UI.button.alignment = TextAnchor.MiddleCenter;
        UI.button.margin = new RectOffset(0, 0, 0, 0);
        UI.isButtonStyleInitialized = true;
      }

      GUILayout.Label(
        String.Format(
          "{0} v{1}",
          FryLabsServerList.Main.mod.Info.DisplayName,
          FryLabsServerList.Main.mod.Info.Version
        ),
        UnityModManager.UI.h1
      );
      GUILayout.Space(5);

      UI.scrollPos = GUILayout.BeginScrollView(UI.scrollPos, GUILayout.MinWidth(mWindowSize.x), GUILayout.MaxWidth(mWindowSize.x));
      GUILayout.BeginVertical();

      foreach (ServerData sData in ServerList.servers)
      {
        GUILayout.Space(3);

        GUILayout.BeginHorizontal(GUILayout.MinWidth(mWindowSize.x - 15), GUILayout.MaxWidth(mWindowSize.x - 15));

        GUILayout.Label(sData.Project, UI.textLeft, GUILayout.Width(150), GUILayout.ExpandWidth(false));
        GUILayout.Label(sData.Info, UI.textInfo, GUILayout.MaxWidth(mWindowSize.x - 440));
        GUILayout.Label(sData.Players, UI.textCenter, GUILayout.Width(50), GUILayout.ExpandWidth(false));
        GUILayout.Label(sData.Ping.ToString(), UI.textCenter, GUILayout.Width(25), GUILayout.ExpandWidth(false));

        if (GUILayout.Button("Discord", UI.button, GUILayout.ExpandWidth(false)))
        {
          ServerList.Discord(sData);
        }

        if (GUILayout.Button("Rules", UI.button, GUILayout.ExpandWidth(false)))
        {
          ToggleWindow(false);
          global::ServerInfo.ShowInfo(sData.serverInfo.pastebin);
        }

        if (GUILayout.Button("Connect", UI.button, GUILayout.ExpandWidth(false)))
        {
          ToggleWindow(false);
          ServerList.Connect(sData);
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(3);
        GUILayout.Label(GUIContent.none, UI.separator, GUILayout.ExpandWidth(true), GUILayout.Height(1f));
      }

      GUILayout.EndVertical();
      GUILayout.EndScrollView();

      GUILayout.FlexibleSpace();
      GUILayout.Space(5);

      GUILayout.BeginHorizontal();
      GUILayout.Label(UI.status);
      GUILayout.FlexibleSpace();
      if (GUILayout.Button("Close", UnityModManager.UI.button, GUILayout.ExpandWidth(false)))
      {
        ToggleWindow(false);
      }
      if (GUILayout.Button("Refresh", UnityModManager.UI.button, GUILayout.ExpandWidth(false)))
      {
        ServerList.Search();
      }
      GUILayout.EndHorizontal();
    }

    public void ToggleWindow()
    {
      ToggleWindow(!Opened);
    }

    public void ToggleWindow(bool open)
    {
      if (open == Opened)
        return;

      try
      {
        Opened = open;
        // BlockGameUI(open);
      }
      catch (Exception e)
      {
        Console.WriteLine("ToggleWindow failed");
        Console.WriteLine(e.ToString());
      }
    }
  }
}
