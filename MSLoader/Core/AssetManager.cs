using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Il2CppInterop.Runtime;
using MSALoader.Core;
using UnityEngine;
using UnityEngine.Playables;


public class AssetManager : MonoBehaviour
{
    private string assetsPath = Application.streamingAssetsPath;
    private Dictionary<string, AssetBundle> assetBundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, UnityEngine.Object> assetCache = new Dictionary<string, UnityEngine.Object>();
    private List<string> availableScenes = new List<string>();
    private AssetBundle currentSceneBundle;

    public List<string> GetAvailableScenes => availableScenes;
    public Dictionary<string, UnityEngine.Object> GetAvailableAssets => assetCache;
    public AssetBundle GetavailableSceneBundle => currentSceneBundle;

    public Dictionary<string, AnimationClip> resourcesAnimations = new Dictionary<string, AnimationClip>();

    public void LoadAnimationsFromResources()
    {
        // Cargar todas las animaciones desde Resources
        var clips = Resources.FindObjectsOfTypeAll<AnimationClip>()
                             .Where(clip => clip != null)
                             .ToList();

        foreach (var clip in clips)
        {
            if (!resourcesAnimations.ContainsKey(clip.name))
            {
                resourcesAnimations.Add(clip.name, clip);
                MSLoader.Logger.LogInfo($"Animación cargada desde Resources: {clip.name}");
            }
        }

        MSLoader.Logger.LogInfo($"Se cargaron {resourcesAnimations.Count} animaciones desde Resources.");
    }

    public List<RuntimeAnimatorController> availableControllers = new List<RuntimeAnimatorController>();

    public void ListAvailableRuntimeAnimatorControllers()
    {
        // Limpiar la lista actual
        availableControllers.Clear();

        // Buscar todos los RuntimeAnimatorController en Resources
        RuntimeAnimatorController[] controllers = Resources.FindObjectsOfTypeAll<RuntimeAnimatorController>();

        if (controllers.Length == 0)
        {
            Debug.LogWarning("No se encontraron RuntimeAnimatorController en Resources.");
            return;
        }

        Debug.Log($"Se encontraron {controllers.Length} RuntimeAnimatorController:");

        foreach (var controller in controllers)
        {
            if (controller != null)
            {
                availableControllers.Add(controller);
                Debug.Log($"Nombre: {controller.name}, Tipo: {controller.GetType()}");
            }
        }
    }

    public List<PlayableAsset> availablePlayableAssets = new List<PlayableAsset>();

    public void ListAvailablePlayableGraphs()
    {
        // Limpiar la lista actual
        availablePlayableAssets.Clear();

        // Buscar todos los RuntimeAnimatorController en Resources
        PlayableAsset[] playableAssets = Resources.FindObjectsOfTypeAll<PlayableAsset>();

        if (playableAssets.Length == 0)
        {
            Debug.LogWarning("No se encontraron PlayableAsset en Resources.");
            return;
        }

        Debug.Log($"Se encontraron {playableAssets.Length} PlayableAsset:");

        foreach (var playableAsset in playableAssets)
        {
            if (playableAsset != null)
            {
                availablePlayableAssets.Add(playableAsset);
                Debug.Log($"Nombre: {playableAsset.name}, Tipo: {playableAsset.GetType()}");
            }
        }
    }

    internal AssetBundle GetLoadedBundle(string bundleName)
    {
        if (assetBundles.TryGetValue(bundleName, out AssetBundle bundle))
        {
            return bundle;
        }
        return null;
    }

    public IEnumerator LoadSceneBundle(string bundleName)
    {
        if (assetBundles.ContainsKey(bundleName))
        {
            MSLoader.Logger.LogWarning($"Bundle de escenas ya cargado: {bundleName}");
            yield break;
        }

        string bundlePath = Path.Combine(assetsPath, $"{bundleName}.bundle");
        if (!File.Exists(bundlePath))
        {
            MSLoader.Logger.LogError($"Bundle no encontrado: {bundlePath}");
            yield break;
        }

        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
        yield return request;

        currentSceneBundle = request.assetBundle;
        if (currentSceneBundle == null)
        {
            MSLoader.Logger.LogError($"Error cargando bundle: {bundleName}");
            yield break;
        }

        assetBundles[bundleName] = currentSceneBundle;
        UpdateSceneList(bundleName);
    }

    private void UpdateSceneList(string bundleName)
    {
        if (!assetBundles.ContainsKey(bundleName))
        {
            MSLoader.Logger.LogError($"Bundle no cargado: {bundleName}");
            return;
        }

        AssetBundle bundle = assetBundles[bundleName];
        availableScenes.Clear();
        string[] scenePaths = bundle.GetAllScenePaths();
        foreach (string path in scenePaths)
        {
            availableScenes.Add(Path.GetFileNameWithoutExtension(path));
        }
    }

    //public IEnumerator LoadAssetBundle(string bundleName)


    public IEnumerator LoadAssetBundle(string bundleName)
    {
        MSLoader.Logger.LogWarning($"Intentando cargar: {bundleName}");

        if (assetBundles.ContainsKey(bundleName))
        {
            MSLoader.Logger.LogWarning($"Bundle de assets ya cargado: {bundleName}");
            yield break;
        }
        MSLoader.Logger.LogWarning($"Siguiente: {bundleName}");

        string bundlePath = Path.Combine(assetsPath, $"{bundleName}.bundle");
        MSLoader.Logger.LogWarning($"Path: {bundlePath}");

        if (!File.Exists(bundlePath))
        {
            MSLoader.Logger.LogError($"Bundle no encontrado: {bundlePath}");
            yield break;
        }
        MSLoader.Logger.LogWarning($"Comenzando request: {bundlePath}");

        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(bundlePath);
        //yield return request;
        MSLoader.Logger.LogWarning($"Done?: {request.isDone}");

        while (!request.isDone)
        {
            MSLoader.Logger.LogInfo($"Progreso carga: {request.progress * 100}%");
            yield return null;
        }


        AssetBundle bundle = request.assetBundle;
        if (bundle == null)
        {
            MSLoader.Logger.LogError($"Error cargando bundle: {bundleName}");
            yield break;
        }

        assetBundles[bundleName] = bundle;
        LoadAllAssets(bundleName).Wait();
    }

    private async Task LoadAllAssets(string bundleName)
    {
        if (!assetBundles.ContainsKey(bundleName))
        {
            MSLoader.Logger.LogError($"AssetBundle no cargado: {bundleName}");
            return;
        }

        AssetBundle bundle = assetBundles[bundleName];
        string[] assetNames = bundle.GetAllAssetNames();

        foreach (string assetName in assetNames)
        {
            // Cargar el asset principal
            UnityEngine.Object asset = bundle.LoadAsset(assetName);
            if (asset != null)
            {
                // Almacenar el asset principal en el caché
                assetCache[asset.name] = asset;
                MSLoader.Logger.LogInfo($"Asset cargado: {asset.name} ({asset.GetIl2CppType().Name})");
            }

            // Cargar los sub-assets asociados
            var subAssets = bundle.LoadAssetWithSubAssets(assetName);
            MSLoader.Logger.LogWarning($"Cantidad de sub-assets: {subAssets.Length}");

            foreach (var subAsset in subAssets)
            {
                // Almacenar cada sub-asset en el caché
                if (subAsset != null)
                {
                    assetCache[subAsset.name] = subAsset;
                    MSLoader.Logger.LogInfo($"Sub-asset cargado: {subAsset.name} ({subAsset.GetType().Name})");
                }
            }
        }

        await Task.Yield();
    }

    //packed_data\actor\artactor\playerhd.pm_01.s008@pm_01_marco_h
    //packed_data\actor\artactor\playerhd.pm_01.s008@pm_01_marco_h
    //S008@PM_01_Marco_2H@Stand01
    //S008@PM_01_Marco_2H@Stand01
    //S008@PM_01_Marco_H@Stand02    
    public IEnumerator InstantiateAssetFromBundle(string bundleName, string assetInfo)
    {
        MSLoader.Logger.LogWarning($"Instantiate Asset From Bundle: {bundleName} - {assetInfo}");
        string assetName = assetInfo.Split(' ')[0];

        // Verificar si el asset está en el caché
        if (assetCache.TryGetValue(assetName, out UnityEngine.Object asset))
        {
            // Si el asset es un GameObject, instanciarlo directamente
            if (asset.GetType() == typeof(GameObject) || asset.GetIl2CppType().FullName == "UnityEngine.GameObject")
            {
                UnityEngine.Object.Instantiate(asset, Vector3.zero, Quaternion.identity);
                MSLoader.Logger.LogInfo($"Instanciado: {assetName}");
            }
            // Si el asset es un AnimationClip, aplicarlo al Animator
            else if (asset.GetType() == typeof(AnimationClip) || asset.GetIl2CppType().FullName == "UnityEngine.AnimationClip")
            {

                AnimationClip animationClip = asset.TryCast<AnimationClip>();

                if (animationClip != null)
                {
                    MSLoader.Logger.LogWarning($"AnimationClip cargado: {animationClip.name}");
                    yield return testHook.animatorController.ChangeAnimationFromBundle(animationClip);
                }
                else
                {
                    MSLoader.Logger.LogError($"No se pudo convertir el asset a AnimationClip: {asset.GetIl2CppType().FullName}");
                }
            }
            else
            {
                MSLoader.Logger.LogWarning($"El asset {assetName} es de tipo {asset.GetType().Name}, no es un GameObject ni AnimationClip.");
            }
        }
        else
        {
            MSLoader.Logger.LogError($"Asset {assetName} no encontrado en el caché.");
        }
        yield return null;
    }

    public IEnumerator UnloadAssetBundle(string bundleName)
    {
        if (assetBundles.TryGetValue(bundleName, out AssetBundle bundle))
        {
            bundle.Unload(true);
            yield return null;
            assetBundles.Remove(bundleName);
            MSLoader.Logger.LogInfo($"Bundle descargado: {bundleName}");

            var assetsToRemove = assetCache.Where(kv => kv.Value.name.StartsWith(bundleName)).ToList();
            foreach (var asset in assetsToRemove)
            {
                assetCache.Remove(asset.Key);
            }
        }
        yield return null;
    }

    private AssetBundleManifest manifest;

    public IEnumerator LoadAssetBundleManifest(string manifestBundleName)
    {
        string manifestPath = Path.Combine(assetsPath, $"{manifestBundleName}.bundle");
        if (!File.Exists(manifestPath))
        {
            MSLoader.Logger.LogError($"Manifest bundle no encontrado: {manifestPath}");
            yield break;
        }

        AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(manifestPath);
        yield return request;

        AssetBundle manifestBundle = request.assetBundle;
        if (manifestBundle == null)
        {
            MSLoader.Logger.LogError($"Error cargando manifest bundle: {manifestBundleName}");
            yield break;
        }

        // Opción 1: Usar conversión explícita con Il2CppType
        manifest = (AssetBundleManifest)manifestBundle.LoadAsset("AssetBundleManifest", Il2CppType.From(typeof(AssetBundleManifest)));

        // Opción 2: Usar la versión genérica de LoadAsset
        // manifest = manifestBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");


        if (manifest == null)
        {
            MSLoader.Logger.LogError("No se pudo cargar el AssetBundleManifest.");
            manifestBundle.Unload(true);
            yield break;
        }

        manifestBundle.Unload(false); // Descargar el bundle del manifest una vez cargado
        MSLoader.Logger.LogInfo("AssetBundleManifest cargado correctamente.");
    }

    public IEnumerator LoadAssetWithDependencies(string bundleName, string assetName)
    {
        // Verificar si el manifest está cargado
        if (manifest == null)
        {
            MSLoader.Logger.LogError("AssetBundleManifest no está cargado. Carga el manifest primero.");
            yield break;
        }

        // Obtener las dependencias del bundle
        string[] dependencies = manifest.GetAllDependencies(bundleName);
        foreach (string dependency in dependencies)
        {
            MSLoader.Logger.LogInfo($"Dependencia encontrada: {dependency}");

            // Cargar cada dependencia si no está cargada
            if (!assetBundles.ContainsKey(dependency))
            {
                yield return LoadAssetBundle(dependency);
            }
        }

        // Cargar el bundle principal si no está cargado
        if (!assetBundles.ContainsKey(bundleName))
        {
            yield return LoadAssetBundle(bundleName);
        }

        // Instanciar el asset
        testHook.StartCoroutine(InstantiateAssetFromBundle(bundleName, assetName)).GetAwaiter().GetResult();
    }

    public IEnumerator LoadAnimatorFromBundle(string bundleName, string assetName)
    {
        if (!assetBundles.ContainsKey(bundleName))
        {
            MSLoader.Logger.LogError($"Bundle no cargado: {bundleName}");
            yield break;
        }

        AssetBundle bundle = assetBundles[bundleName];
        UnityEngine.Object asset = bundle.LoadAsset(assetName);

        if (asset == null)
        {
            MSLoader.Logger.LogError($"Asset no encontrado: {assetName} en {bundleName}");
            yield break;
        }

        if (asset is RuntimeAnimatorController animatorController)
        {
            // Almacenar el AnimatorController en el sistema
            MSLoader.Logger.LogInfo($"AnimatorController cargado: {assetName}");
            yield return animatorController;
        }
        else if (asset is AnimationClip animationClip)
        {
            // Almacenar la animación en el sistema
            MSLoader.Logger.LogInfo($"AnimationClip cargada: {assetName}");
            yield return animationClip;
        }
        else
        {
            MSLoader.Logger.LogError($"El asset {assetName} no es un AnimatorController ni un AnimationClip.");
        }
    }

    private Dictionary<string, AnimationClip> loadedAnimationClips = new Dictionary<string, AnimationClip>();

    public IEnumerator LoadAnimationClipsFromBundle(string bundleName)
    {
        if (!assetBundles.ContainsKey(bundleName))
        {
            MSLoader.Logger.LogError($"Bundle no cargado: {bundleName}");
            yield break;
        }

        AssetBundle bundle = assetBundles[bundleName];
        UnityEngine.Object[] allAssets = bundle.LoadAllAssets();

        foreach (UnityEngine.Object asset in allAssets)
        {
            if (asset is AnimationClip clip)
            {
                if (!loadedAnimationClips.ContainsKey(clip.name))
                {
                    loadedAnimationClips[clip.name] = clip;
                    MSLoader.Logger.LogInfo($"AnimationClip cargado: {clip.name}");
                }
            }
        }
    }

    public async Task<List<string>> GetAvailableAnimationClipsFromBundle()
    {
        var animations = new List<string>();

        foreach (AssetBundle item in assetBundles.Values)
        {
            UnityEngine.Object[] allAssets = item.LoadAllAssets();

            foreach (UnityEngine.Object asset in allAssets)
            {
                if (asset is AnimationClip clip)
                {
                    if (!loadedAnimationClips.ContainsKey(clip.name))
                    {
                        if (!animations.Contains(clip.name))
                        {
                            MSLoader.Logger.LogWarning($"anim: {clip.name} \n");
                            animations.Add(clip.name);
                        }
                    }
                }
                await Task.Yield();
            }
            await Task.Yield();
        }

        // Animaciones externas cargadas
        animations.AddRange(assetBundles.Keys.Except(animations));

        return animations;
    }
}
