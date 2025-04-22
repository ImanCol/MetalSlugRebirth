

using System.Reflection;
using BepInEx.Logging;
using HarmonyLib;
using Il2CppInterop.Runtime.Injection;
using MSALoader.Components;
using MSALoader.Core;
using UnityEngine;

public static class MSLoader
{
    public static ManualLogSource Logger;
    public static Harmony _harmony;
    public static async Task Main(ManualLogSource log)
    {
        HarmonyHooks();
        await Task.Delay(3000);

        log.LogInfo("MSLoader.Main invoked.");
        Logger = log;
        Logger.LogWarning("Creando objeto...");

        if (MSBootstrap.Instance == null)
        {
            RegisterAssembly();
            MakeObject();
        }
        else
        {
            Logger.LogWarning("MSBootstrap ya existe, removiendo...");
            UnityEngine.Object.Destroy(MSBootstrap.Instance);
            //RegisterAssembly();
            MakeObject();
        }
    }

    public static void MakeObject()
    {
        GameObject gameObject = new GameObject("MSALoader");
        UnityEngine.Object.DontDestroyOnLoad(gameObject);
        Logger.LogWarning("Agregando MSBootstrap...");
        MSBootstrap.Instance = gameObject.AddComponent<MSBootstrap>();
    }
    public static void RegisterAssembly()
    {

        Logger.LogWarning("Encontrado.");
        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<MSBootstrap>())
        {
            Logger.LogWarning("MSBootstrap ya existe, removiendo...");
            UnityEngine.Object.Destroy(MSBootstrap.Instance);
            Logger.LogWarning("Registrando MSBootstrap...");
            ClassInjector.RegisterTypeInIl2Cpp<MSBootstrap>();
        }

        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<CameraDebugController>())
        {
            Logger.LogWarning("Registrando CameraDebugController...");
            ClassInjector.RegisterTypeInIl2Cpp<CameraDebugController>();
        }

    }

    public enum HookType
    {
        Pre,
        Post,
        Transpiler
    }

    public static void Hookear(Type _metrodInfo, string _internalMethod, Type _harmonyMethod, string _externalMethod, HookType _hookType = HookType.Pre)
    {
        var _Method = _metrodInfo
            .GetMethod(_internalMethod, BindingFlags.Public | BindingFlags.Instance);

        if (_Method != null)
        {
            var prefix = _harmonyMethod.GetMethod(_externalMethod);

            //tipo de hook
            if (_hookType == HookType.Pre)
            {
                _harmony.Patch(_Method, prefix: new HarmonyMethod(prefix));
            }
            else if (_hookType == HookType.Post)
            {
                _harmony.Patch(_Method, postfix: new HarmonyMethod(prefix));
            }
            else if (_hookType == HookType.Transpiler)
            {
                _harmony.Patch(_Method, transpiler: new HarmonyMethod(prefix));
            }

            Logger.LogInfo($"Hook para {_internalMethod} aplicado");
        }
    }

    public static void HarmonyHooks()
    {
        _harmony = new Harmony("com.tu.nombre.packetsniffer.il2cpp");
        //AssetBundleHook.Hook();
    }


    public static void Unload()
    {
        // Unload and unpatch everything before reloading the script
        Logger.LogInfo("MSLoader.Unload invoked.");
    }

}