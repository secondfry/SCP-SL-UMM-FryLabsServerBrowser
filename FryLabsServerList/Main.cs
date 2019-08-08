using Harmony;
using System.Reflection;
using UnityModManagerNet;


namespace FryLabsServerList
{
#if DEBUG
  [EnableReloading]
#endif
  static class Main
  {
    // UnityModManager
    public static bool enabled;
    public static UnityModManager.ModEntry mod;

    static bool Load(UnityModManager.ModEntry modEntry)
    {
      UI.status = "Loading...";

      Main.mod = modEntry;
      modEntry.OnToggle = OnToggle;

      var harmony = HarmonyInstance.Create(modEntry.Info.Id);
      harmony.PatchAll(Assembly.GetExecutingAssembly());

      FryLabsServerList.UI.Load();

      UI.status = "Loaded!";

#if DEBUG
      modEntry.OnUnload = Main.Unload;
#endif

      return true;
    }

#if DEBUG
    static bool Unload(UnityModManager.ModEntry modEntry)
    {
      var harmony = HarmonyInstance.Create(modEntry.Info.Id);
      harmony.UnpatchAll();

      return true;
    }
#endif

    static bool OnToggle(UnityModManager.ModEntry modEntry, bool value)
    {
      Main.enabled = value;

      return true;
    }
  }

  [HarmonyPatch(typeof(ServerListManager))]
  [HarmonyPatch("Refresh")]
  static class ServerListManager_Refresh_Patch
  {
    static bool Prefix()
    {
      FryLabsServerList.UI.Instance.IsModEnabled = Main.enabled;
      FryLabsServerList.UI.Instance.Opened = true;

      if (ServerList.serversDictionary.Count == 0)
        ServerList.Search();

      // Don't run original function if our browser is enabled
      return !Main.enabled;
    }
  }
}
