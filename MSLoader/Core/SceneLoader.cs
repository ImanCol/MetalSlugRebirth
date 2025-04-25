using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MSALoader.Core
{
    public class SceneLoader
    {
        // Variables para almacenar información sobre AssetBundles y escenas
        private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private string assetsPath;
        public Scene LastScene { get; set; }


        /// <summary>
        /// Constructor para inicializar la ruta de los assets.
        /// </summary>
        public SceneLoader(string customAssetsPath = null)
        {
            assetsPath = customAssetsPath ?? Application.streamingAssetsPath;
            //this.assetsPath = assetsPath;

        }


        //mover a AssetBundle??
        /// <summary>
        /// Lista todas las escenas disponibles en un AssetBundle.
        /// </summary>
        /// <param name="bundleName">Nombre del AssetBundle.</param>
        /// <returns>Una lista de nombres de escenas.</returns>
        public List<string> GetScenesInBundle(string bundleName)
        {
            if (!loadedBundles.ContainsKey(bundleName))
            {
                MSLoader.Logger.LogError($"AssetBundle no cargado: {bundleName}");
                return new List<string>();
            }

            AssetBundle bundle = loadedBundles[bundleName];
            if (!bundle.isStreamedSceneAssetBundle)
            {
                MSLoader.Logger.LogError("El AssetBundle no contiene escenas.");
                return new List<string>();
            }

            string[] scenePaths = bundle.GetAllScenePaths();
            List<string> sceneNames = new List<string>();

            foreach (string path in scenePaths)
            {
                sceneNames.Add(Path.GetFileNameWithoutExtension(path));
            }

            MSLoader.Logger.LogInfo($"Escenas encontradas en {bundleName}: {sceneNames.Count}");
            return sceneNames;
        }

        /// <summary>
        /// Carga una escena específica desde un AssetBundle.
        /// </summary>
        /// <param name="bundleName">Nombre del AssetBundle.</param>
        /// <param name="sceneName">Nombre de la escena.</param>
        /// <param name="mode">Modo de carga (Single o Additive).</param>
        public IEnumerator LoadSceneFromBundle(AssetBundle assetBundle, string bundleName, string sceneName, LoadSceneMode mode)
        {
            //string bundlePath = Path.Combine(assetsPath, bundleName + ".bundle");

            //bundle = AssetBundle.LoadFromFile(bundlePath);
            if (assetBundle == null)
            {
                MSLoader.Logger.LogError($"Error cargando bundle: {bundleName}");
                yield break;
            }

            try
            {
                string[] scenePaths = assetBundle.GetAllScenePaths();
                string targetScene = scenePaths.FirstOrDefault(p =>
                    Path.GetFileNameWithoutExtension(p).Equals(sceneName, StringComparison.OrdinalIgnoreCase));

                if (string.IsNullOrEmpty(targetScene))
                {
                    MSLoader.Logger.LogError($"Escena {sceneName} no encontrada");
                    yield break;
                }

                AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(
                    Path.GetFileNameWithoutExtension(targetScene),
                    mode
                );

                while (!asyncLoad.isDone)
                {
                    MSLoader.Logger.LogInfo($"Progreso carga: {asyncLoad.progress * 100}%");
                    yield return null;
                }
            }
            finally
            {

                assetBundle.Unload(false);
            }
        }

        //no usado?
        /// <summary>
        /// Descarga todas las escenas cargadas excepto la activa.
        /// </summary>
        public void UnloadAllScenesExceptActive()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene == SceneManager.GetActiveScene()) continue;

                MSLoader.Logger.LogInfo($"Descargando escena: {scene.name}");
                SceneManager.UnloadSceneAsync(scene);
            }
        }

        //mover a AssetManager?
        public void CacheAllSceneSources()
        {
            // Lógica para cachear escenas desde Build Settings o StreamingAssets
            MSLoader.Logger.LogInfo("Escenas cacheadas exitosamente.");
        }

    }
}
