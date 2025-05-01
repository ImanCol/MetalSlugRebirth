

using System.Reflection;
using System.Runtime.InteropServices;
using BepInEx.Logging;
using Google.Protobuf;
using HarmonyLib;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using MonoMod.Utils;
using MSALoader.Components;
using MSALoader.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class MSLoader
{
    public static ManualLogSource Logger;
    public static Harmony _harmony;
    public static testHook testHook2;

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Logger.LogWarning($"Scene loaded: {scene.name} in mode: {mode}");
        //CheckSceneChange();
    }

    private static void OnSceneUnloaded(Scene scene)
    {
        Logger.LogWarning($"Scene unloaded: {scene.name}");
    }


    private static void SubscribeToSceneLoaded()
    {
        try
        {
            //TestUnityAction();
            //return;
            // Obtener el MethodInfo del método add_sceneLoaded
            var methodInfo = typeof(SceneManager).GetMethod("add_sceneLoaded", BindingFlags.Public | BindingFlags.Static);
            if (methodInfo == null)
            {
                Logger.LogError("No se pudo encontrar el método add_sceneLoaded.");
                return;
            }


            // Crear un delegado Action<Scene, LoadSceneMode>

            //usar UnityEngine.Events.UnityAction
            //var actionHandler = new UnityEngine.Events.UnityAction<Scene, LoadSceneMode>(OnSceneLoadedStatic);

            //Action<Scene, LoadSceneMode> actionHandler = OnSceneLoadedStatic;


            var _OnSceneLoadedStatic = typeof(MSLoader).GetMethod("OnSceneLoadedStatic", BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic);
            int token = _OnSceneLoadedStatic.MetadataToken;
            Action<Scene, LoadSceneMode> actionHandler = (Action<Scene, LoadSceneMode>)Delegate.CreateDelegate(typeof(Action<Scene, LoadSceneMode>), null, _OnSceneLoadedStatic);
            IntPtr functionPointer = Marshal.GetFunctionPointerForDelegate(new NonGenericDelegate((scene, mode) => actionHandler(scene, mode)));
            UnityAction<Scene, LoadSceneMode> unityAction = new UnityAction<Scene, LoadSceneMode>(functionPointer);

            Logger.LogWarning($"Tipo de UnityAction: {typeof(UnityAction<Scene, LoadSceneMode>)}");
            Logger.LogWarning($"Es delegado: {typeof(UnityAction<Scene, LoadSceneMode>).IsSubclassOf(typeof(Delegate))}");
            //var unityAction = Delegate.CreateDelegate(typeof(UnityAction<Scene, LoadSceneMode>), null, _OnSceneLoadedStatic);

            actionHandler.Invoke(new Scene(), LoadSceneMode.Single);
            //methodInfo.Invoke(null, new object[] { OnSceneLoadedStatic });

            return;
            //UnityAction<Scene, LoadSceneMode> unityAction = actionHandler;
            //actionHandler.TryCastDelegate(out unityAction);
            //actionHandler.Invoke(new Scene(), LoadSceneMode.Single);
            //actionHandler.Target.GetType().GetMethod("Invoke").CreateDelegate(typeof(UnityAction<Scene, LoadSceneMode>));
            Logger.LogWarning($"Token del método: {token}");

            IntPtr _intPtr = IL2CPP.GetIl2CppMethodByToken(Il2CppClassPointerStore<MSLoader>.NativeClassPtr, token);
            Logger.LogWarning($"Il2CppMethodPointer: {_intPtr}");
            //Logger.LogWarning($"Type: {_intPtr.AsDelegate<UnityEngine.Object>()}");
            //UnityAction<Scene, LoadSceneMode> actionHandler2 = (UnityAction<Scene, LoadSceneMode>)_intPtr.GetType().GetMethod("Invoke").CreateDelegate(typeof(UnityAction<Scene, LoadSceneMode>));
            //Logger.LogWarning($"ActionHandler2: {actionHandler2}");
            //actionHandler.Invoke(new Scene(), LoadSceneMode.Single);
            Logger.LogWarning("Invoking method to add scene loaded handler.");
            //methodInfo.Invoke(null, new object[] { _OnSceneLoadedStatic });



            return;
            // Convertir el delegado estándar de C# en uno compatible con IL2CPP
            //UnityAction<Scene, LoadSceneMode> unityActionHandler =
            //DelegateSupport.ConvertDelegate<UnityAction<Scene, LoadSceneMode>>(actionHandler);

            // Invocar el método add_sceneLoaded para suscribirse al evento
            //methodInfo.Invoke(null, new object[] { unityActionHandler });
            //SceneManager.add_sceneLoaded(unityActionHandler);
            //Logger.LogInfo("Suscripción al evento realizada mediante reflexión.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Error al suscribirse al evento: {ex}");
        }
    }

    // Delegado no genérico auxiliar
    private delegate void NonGenericDelegate(Scene scene, LoadSceneMode mode);

    public static string version;
    //La unica manera de evitar objetos nullos o ejecutar los mismos metodos iniciales varias veces es usando un GameObject y un MonoBehaviour como PostProcessDebug

    public static async Task Main(ManualLogSource log)
    {
        log.LogWarning("MSLoader.Main invoked.");
        Logger = log;

        //GameObject newGameObject = new GameObject("SimpleGui");
        //
        //newGameObject.AddComponent<SimpleGui>();

        //while (true)
        //{
        //    SimpleGui.OnGUI();
        //    Logger.LogWarning("SimpleGui.OnGUI invoked 2.");
        //    await Task.Delay(3000);
        //}

        //SubscribeToSceneLoaded();

        //HarmonyHooks();

        await Task.Delay(1000);


        Logger.LogWarning("[MSLoader] Iniciando...");


        //si la version del juego es igual o inferior a 1.5.4, ejecuta RegisterAssembly()
        //usa <= para verificar la version

        // Check if object already exists
        GameObject gameObject = GameObject.Find("PostProcessDebug");
        if (gameObject == null)
        {
            version = Application.version;
            Logger.LogWarning($"Versión del juego: {version}");
            //nombre del juego
            Logger.LogWarning($"Nombre del juego: {Application.productName}");


            if (version == "1.5.4")
            {
                Logger.LogWarning("Versión del juego es 1.5.4, registrando Componentes externos...");
                RegisterAssembly();
            }
            else if (version == "1.8.4") //S3
            {
                Logger.LogWarning("Versión del juego es 1.5.4, registrando Hooks externos...");
            }
            else
            {
                Logger.LogWarning("Versión del juego es mayor a 1.5.4, puede experimentar limitaciones...");
            }

            MSLoader.Logger.LogWarning("[MSLoader] Iniciando el Init...");
            await testHook.Init();
            MSLoader.Logger.LogWarning("[MSLoader] Done.");
            if (Application.productName == "Playit-Test")
            {
                //Logger.LogWarning("Nombre del juego es MSALoader, registrando Componentes externos...");
                RegisterAssembly();
                MakeObject();
            }
            else
            {
                MSLoader.Logger.LogWarning("[MSLoader] Iniciando el hook...");
                await testHook.Hook();
                MSLoader.Logger.LogWarning("Done.");
            }

            //MSLoader.Logger.LogWarning("[MSLoader] Creando objeto PostProcessDebug...");
            await Task.Delay(1000);
            gameObject = new GameObject("PostProcessDebug");
            var target = gameObject.AddComponent<UnityEngine.Rendering.PostProcessing.PostProcessDebug>();
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
        }
        else
        {
            MSLoader.Logger.LogWarning("[MSLoader] PostProcessDebug ya existe, iniciando...");
            MSLoader.Logger.LogWarning("[MSLoader] Iniciando el Init...");
            await testHook.Init();
            MSLoader.Logger.LogWarning("[MSLoader] Done.");
        }
        if (MSBootstrap.Instance == null)
        {
            //RegisterAssembly();
            //MakeObject();
        }
        else
        {
            //Logger.LogWarning("MSBootstrap ya existe, iniciando...");
            //UnityEngine.Object.Destroy(MSBootstrap.Instance);
            //RegisterAssembly();
            //MakeObject();
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

        //if (!ClassInjector.IsTypeRegisteredInIl2Cpp<CameraDebugController>())
        //{
        //    Logger.LogWarning("Registrando CameraDebugController...");
        //    ClassInjector.RegisterTypeInIl2Cpp<CameraDebugController>();
        //}

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


    public static void Unload()
    {
        // Unload and unpatch everything before reloading the script
        //detener todas las courutine, whiles, etc

        Logger.LogWarning($"se eliminaran {_harmony.GetPatchedMethods().Count()} parches");
        foreach (var item in _harmony.GetPatchedMethods())
        {
            Logger.LogWarning($"Se desparcheara: {item.Name}");
        }
        _harmony.UnpatchSelf();

        //StopallCoroutines();
        Logger.LogWarning("MSLoader.Unload invoked.");
    }

}

internal class AssemblyLoader
{
}