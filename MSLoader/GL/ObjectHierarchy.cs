using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MSALoader.Core
{
    public class ObjectHierarchy
    {
        // Variables para manejar la jerarquía

        // Evento para notificar la selección de un objeto
        public event Action<GameObject> OnObjectSelected;

        private Dictionary<string, bool> expandedObjects = new Dictionary<string, bool>();
        private Dictionary<string, List<GameObject>> sceneObjectsCache = new Dictionary<string, List<GameObject>>();
        private Vector2 scrollPosition;
        private GameObject selectedObject;

        /// <summary>
        /// Dibuja la jerarquía completa de objetos.
        /// </summary>
        public void DrawHierarchy()
        {
            try
            {
                UpdateCachedObjects().GetAwaiter().GetResult();
                float totalHeight = CalculateTotalHierarchyHeight().GetAwaiter().GetResult();

                GUI.Box(new Rect(10, 10, 300, 400), "Jerarquía de Objetos");
                scrollPosition = GUI.BeginScrollView(new Rect(20, 50, 280, 330), scrollPosition, new Rect(0, 0, 260, totalHeight));

                float yPos = 0;

                // Escenas cargadas
                for (int i = 0; i < SceneManager.sceneCount; i++)
                {
                    Scene scene = SceneManager.GetSceneAt(i);
                    if (!scene.isLoaded) continue;

                    string sceneKey = $"{scene.name}_{scene.handle}";
                    bool isExpanded = expandedObjects.TryGetValue(sceneKey, out bool expanded) && expanded;

                    GUI.contentColor = Color.white;
                    if (GUI.Button(new Rect(0, yPos, 260, 20), $"[Escena {i}] {scene.name}"))
                    {
                        expandedObjects[sceneKey] = !isExpanded;
                    }

                    yPos += 20;

                    if (isExpanded && sceneObjectsCache.TryGetValue(scene.name, out var objects))
                    {
                        foreach (var obj in objects.Where(o => o != null))
                        {
                            RenderObjectHierarchy(obj, ref yPos);
                            //await Task.Yield();
                        }
                    }
                }

                // DontDestroyOnLoad
                GUI.contentColor = Color.yellow;
                bool ddolExpanded = expandedObjects.TryGetValue("DDOL", out bool ddolExp) && ddolExp;
                if (GUI.Button(new Rect(0, yPos, 260, 20), "[DontDestroyOnLoad]"))
                {
                    expandedObjects["DDOL"] = !ddolExpanded;
                }

                yPos += 20;

                if (ddolExpanded && sceneObjectsCache.TryGetValue("DDOL", out var ddolObjects))
                {
                    foreach (var obj in ddolObjects.Where(o => o != null))
                    {
                        RenderObjectHierarchy(obj, ref yPos);
                    }
                }

                // Objetos no clasificados
                GUI.contentColor = Color.magenta;
                bool unclassifiedExpanded = expandedObjects.TryGetValue("UNCLASSIFIED", out bool unclExp) && unclExp;
                if (GUI.Button(new Rect(0, yPos, 260, 20), "[No Clasificados]"))
                {
                    expandedObjects["UNCLASSIFIED"] = !unclassifiedExpanded;
                }

                yPos += 20;

                if (unclassifiedExpanded && sceneObjectsCache.TryGetValue("UNCLASSIFIED", out var unclassified))
                {
                    foreach (var obj in unclassified.Where(o => o != null))
                    {
                        RenderObjectHierarchy(obj, ref yPos);
                    }
                }

                GUI.EndScrollView();
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error dibujando jerarquía: {ex}");
            }
        }

        /// <summary>
        /// Actualiza el caché de objetos en la escena.
        /// </summary>
        public async Task UpdateCachedObjects()
        {
            try
            {
                sceneObjectsCache.Clear();

                // Obtener todos los objetos visibles
                var allObjects = Resources.FindObjectsOfTypeAll<GameObject>()
                    .Where(obj => obj != null && obj.hideFlags == HideFlags.None)
                    .ToList();

                // Clasificar por escenas cargadas
                foreach (var scene in SceneManager.GetAllScenes().Where(s => s.isLoaded))
                {
                    var roots = scene.GetRootGameObjects()
                        .Where(obj => obj.hideFlags == HideFlags.None && obj.scene == scene)
                        .ToList();

                    sceneObjectsCache[scene.name] = roots;
                }

                // Clasificar DontDestroyOnLoad
                var ddolObjects = (await GetTrueDontDestroyOnLoadObjects())
                    .Where(obj => obj.transform.parent == null && obj.scene.name == null)
                    .ToList();

                sceneObjectsCache["DDOL"] = ddolObjects;

                // Objetos no clasificados (no están en ninguna escena ni en DDOL)
                var unclassified = allObjects
                    .Where(obj =>
                        !sceneObjectsCache.Values.Any(list => list.Contains(obj)) &&
                        obj.transform.parent == null &&
                        obj.scene.name != null
                    )
                    .ToList();

                sceneObjectsCache["UNCLASSIFIED"] = unclassified;
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error actualizando caché de objetos: {ex}");
            }
        }

        /// <summary>
        /// Calcula la altura total necesaria para mostrar la jerarquía.
        /// </summary>
        public async Task<float> CalculateTotalHierarchyHeight()
        {
            float totalHeight = 0;

            // Escenas
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded) continue;

                string sceneKey = $"{scene.name}_{scene.handle}";
                bool isExpanded = expandedObjects.TryGetValue(sceneKey, out bool expanded) && expanded;
                //
                totalHeight += 20; // Botón de escena
                //
                if (isExpanded && sceneObjectsCache.TryGetValue(scene.name, out var objects))
                {
                    foreach (var obj in objects)
                    {
                        totalHeight += await CalculateObjectHierarchyHeight(obj);
                        //await Task.Yield();
                    }
                }
                //await Task.Yield();
            }

            // DontDestroyOnLoad
            totalHeight += 20;
            if (expandedObjects.TryGetValue("DDOL", out bool ddolExpanded) && ddolExpanded)
            {
                if (sceneObjectsCache.TryGetValue("DDOL", out var ddolObjects))
                {
                    foreach (var obj in ddolObjects)
                    {
                        totalHeight += await CalculateObjectHierarchyHeight(obj);
                        //await Task.Yield();
                    }
                }
            }

            // No clasificados
            totalHeight += 20;
            if (expandedObjects.TryGetValue("UNCLASSIFIED", out bool unclassifiedExpanded) && unclassifiedExpanded)
            {
                if (sceneObjectsCache.TryGetValue("UNCLASSIFIED", out var unclassified))
                {
                    foreach (var obj in unclassified)
                    {
                        totalHeight += await CalculateObjectHierarchyHeight(obj);
                        //await Task.Yield();
                    }
                }
            }

            return totalHeight;
        }

        /// <summary>
        /// Calcula la altura de un objeto y sus hijos.
        /// </summary>
        private async Task<float> CalculateObjectHierarchyHeight(GameObject obj)
        {
            float height = 20; // Altura del objeto actual

            if (expandedObjects.TryGetValue(obj.GetInstanceID().ToString(), out var isExpanded) && isExpanded)
            {
                Transform parentTransform = obj.transform;
                int childCount = parentTransform.childCount;

                for (int i = 0; i < childCount; i++)
                {
                    Transform child = parentTransform.GetChild(i);
                    if (child != null && child.gameObject != null)
                    {
                        height += await CalculateObjectHierarchyHeight(child.gameObject);
                    }
                    //await Task.Yield();
                }
            }
            //await Task.Yield();
            return height;
        }

        /// <summary>
        /// Dibuja la jerarquía de un objeto específico.
        /// </summary>
        private void RenderObjectHierarchy(GameObject obj, ref float yPos)
        {
            try
            {
                if (obj == null) return;

                GUI.contentColor = obj.activeSelf ? Color.white : new Color(0.7f, 0.7f, 0.7f, 1f);
                string statusIcon = obj.activeSelf ? "■" : "□";

                if (GUI.Button(new Rect(0, yPos, 260, 20), $"{statusIcon} {obj.name}"))
                {
                    selectedObject = obj;

                    // Notificar que se ha seleccionado un objeto
                    OnObjectSelected?.Invoke(obj);

                    SelectObject(obj).GetAwaiter().GetResult();
                    string key = obj.GetInstanceID().ToString();
                    expandedObjects[key] = !expandedObjects.GetValueOrDefault(key, false);
                }

                yPos += 20;


                if (expandedObjects.GetValueOrDefault(obj.GetInstanceID().ToString(), false))
                {
                    Transform parentTransform = obj.transform;
                    int childCount = parentTransform.childCount;

                    for (int i = 0; i < childCount; i++)
                    {
                        Transform child = parentTransform.GetChild(i);
                        if (child != null && child.gameObject != null)
                        {
                            RenderObjectHierarchy(child.gameObject, ref yPos);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error dibujando jerarquía de objetos: {ex}");
            }
            return;
        }

        /// <summary>
        /// Obtiene los objetos en DontDestroyOnLoad.
        /// </summary>
        private async Task<List<GameObject>> GetTrueDontDestroyOnLoadObjects()
        {
            List<GameObject> ddolObjects = new List<GameObject>();
            GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

            foreach (var obj in allObjects)
            {
                if (obj.scene.name == null && obj.transform.parent == null)
                {
                    ddolObjects.Add(obj);
                }
                //await Task.Yield();
            }
            //await Task.Yield();
            return ddolObjects.Distinct().ToList();
        }


        // Propiedad para almacenar el objeto seleccionado
        public GameObject SelectedObject { get; private set; }

        // Método para actualizar el objeto seleccionado
        public async Task SelectObject(GameObject obj)
        {
            if (obj == null) return;

            SelectedObject = obj;
            MSLoader.Logger.LogInfo($"Objeto seleccionado: {obj.name}");
            await Task.Yield();
        }

    }
}
