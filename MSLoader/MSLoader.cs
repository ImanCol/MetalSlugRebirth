// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MSLoader
{
    private static ManualLogSource log;

    // UnityEngine.Debug causes System.AccessViolationException exception, inject ManualLogSource as a workaround
    public static void Main(ManualLogSource log)
    {
        log.LogInfo("MSLoader.Main invoked.");
        MSLoader.log = log;
    }

    public static void Unload()
    {
        // Unload and unpatch everything before reloading the script
        log.LogInfo("MSLoader.Unload invoked.");
    }

}
