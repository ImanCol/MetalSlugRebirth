using System;
using System.Collections;
using System.Threading.Tasks;
using BepInEx.Unity.IL2CPP.Utils;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

namespace MSALoader.Core
{
    public class GUIManager
    {
        // Variables para controlar la visibilidad de los paneles
        private bool showHierarchyPanel = true;
        private bool showInspectorPanel = true;
        private bool showCameraControlsPanel = true;
        private bool showAnimationSelectorPanel = true;

        // Scroll positions para los paneles
        private Vector2 hierarchyScrollPosition;
        private Vector2 inspectorScrollPosition;
        private Vector2 cameraControlsScrollPosition;
        private Vector2 animationSelectorScrollPosition;

        // Colores para la GUI
        private Color activeColor = Color.white;
        private Color inactiveColor = new Color(0.7f, 0.7f, 0.7f, 1f);

        // Referencias a otros sistemas
        private AssetManager assetManager;
        private SceneLoader sceneLoader;
        private ObjectHierarchy objectHierarchy;
        private CameraManager cameraManager;
        private AnimatorController animatorController;

        private string bundleName = ""; // Nombre del AssetBundle ingresado por el usuario
        private Vector2 scrollPos = Vector2.zero; // Posición de scroll para el área de contenido
        private Vector2 scrollPosAssets;


        /// <summary>
        /// Constructor para inicializar referencias.
        /// </summary>
        public GUIManager(AssetManager assetManager, SceneLoader sceneLoader, ObjectHierarchy objectHierarchy, CameraManager cameraManager, AnimatorController animatorController)
        {
            this.assetManager = assetManager;
            this.sceneLoader = sceneLoader;
            this.objectHierarchy = objectHierarchy;
            // Suscribirse al evento de selección de objetos
            objectHierarchy.OnObjectSelected += UpdateSelectedObject;

            this.cameraManager = cameraManager;
            this.animatorController = animatorController;
        }

        /// <summary>
        /// Dibuja la interfaz gráfica completa.
        /// </summary>
        public void DrawGUI()
        {
            try
            {
                if (MSLoader.version == "1.5.4")
                {
                    //fix this
                    DrawHierarchyPanel();
                    DrawInspectorPanel();
                    DrawCameraControlsPanel();
                    DrawAnimationSelectorPanel();
                    DrawBundleLoader(); // Nuevo panel de gestión de bundles
                }
                else if (MSLoader.version == "1.8.4") //S3
                {
                    DrawHierarchyPanel();
                    //DrawInspectorPanel();
                    //DrawCameraControlsPanel();
                    //DrawAnimationSelectorPanel();
                    //await DrawBundleLoader(); // Nuevo panel de gestión de bundles
                }
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error en DrawGUI: {ex}");
            }
        }

        /// <summary>
        /// Dibuja el panel de jerarquía.
        /// </summary>
        private void DrawHierarchyPanel()
        {
            if (!showHierarchyPanel) return;
            GUI.Box(new Rect(10, 10, 300, Screen.height - 20), "Jerarquía de Objetos");

            hierarchyScrollPosition = GUI.BeginScrollView(
                new Rect(20, 40, 280, Screen.height - 60),
                hierarchyScrollPosition,
                new Rect(0, 0, 260, objectHierarchy.CalculateTotalHierarchyHeight().GetAwaiter().GetResult())
            );

            objectHierarchy.DrawHierarchy();
            GUI.EndScrollView();
        }

        /// <summary>
        /// Dibuja el panel de inspector.
        /// </summary>
        // En GUIManager.cs
        private void DrawInspectorPanel()
        {
            if (!showInspectorPanel) return;

            GameObject selectedObject = objectHierarchy.SelectedObject;

            if (selectedObject == null) return;

            GUI.Box(new Rect(320, 10, 400, Screen.height - 20), $"Inspector: {selectedObject.name}");

            inspectorScrollPosition = GUI.BeginScrollView(
                new Rect(330, 40, 380, Screen.height - 60),
                inspectorScrollPosition,
                new Rect(0, 0, 360, 1000)
            );

            float yPos = 0;


            // Botón para activar/desactivar el GameObject
            bool isActive = selectedObject.activeSelf;
            bool newActive = GUI.Toggle(new Rect(100, yPos, 120, 20), isActive, isActive ? "DESACTIVAR" : "ACTIVAR");
            if (newActive != isActive)
            {
                selectedObject.SetActive(newActive);
                MSLoader.Logger.LogInfo($"GameObject {selectedObject.name} estado: {newActive}");
            }

            yPos += 30;

            // Inspeccionar Transform
            DrawTransformInspector(selectedObject.transform, ref yPos);

            // Inspeccionar RectTransform (si existe)
            RectTransform rectTransform = selectedObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                DrawRectTransformInspector(rectTransform, ref yPos);
            }

            // Mostrar controles del Animator si existe
            Animator animator = selectedObject.GetComponent<Animator>();
            if (animator != null)
            {
                DrawAnimatorControls(animator, ref yPos);
            }
            GUI.EndScrollView();

            return;
        }

        /// <summary>
        /// Dibuja todos los componentes del objeto seleccionado.
        private async Task<float> DrawAllComponents(GameObject obj, float yPos)
        {
            if (obj == null) return yPos;

            Component[] components = obj.GetComponents<Component>();

            foreach (var component in components)
            {
                if (component == null) continue;

                try
                {
                    // Filtrar componentes innecesarios
                    string componentFullName = component.GetIl2CppType().FullName;
                    if (componentFullName == "UnityEngine.Transform" || componentFullName == "UnityEngine.RectTransform") continue;

                    // Nombre del componente
                    GUI.Label(new Rect(10, yPos, 360, 20), $"Componente: {component.GetIl2CppType().FullName}");
                    yPos += 20;

                    // Toggle para habilitar/deshabilitar (solo para Behaviour)
                    if (component is Behaviour behaviour)
                    {
                        bool isEnabled = behaviour.enabled;
                        bool newEnabled = GUI.Toggle(new Rect(20, yPos, 150, 20), isEnabled, "Habilitado");
                        if (newEnabled != isEnabled)
                        {
                            behaviour.enabled = newEnabled;
                            MSLoader.Logger.LogInfo($"{component.GetIl2CppType().Name} estado: {newEnabled}");
                        }
                        yPos += 25;
                    }

                    // Mostrar campos editables del componente
                    var fields = component.GetIl2CppType().GetFields(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                    foreach (var field in fields)
                    {
                        string fieldName = field.Name;
                        Il2CppSystem.Object fieldValue = field.GetValue(component);

                        GUI.Label(new Rect(20, yPos, 150, 20), $"{fieldName}:");
                        string fieldType = field.FieldType.ToString(); // Usar ToString() para comparar tipos

                        if (fieldType == "System.Int32") // Comparar con el nombre completo del tipo
                        {
                            int value = ConvertToInt(fieldValue);
                            value = (int)GUI.HorizontalSlider(new Rect(170, yPos, 100, 20), value, 0, 100);
                            field.SetValue(component, value);
                        }
                        else if (fieldType == "System.Single") // Comparar con el nombre completo del tipo
                        {
                            float value = ConvertToFloat(fieldValue);
                            value = GUI.HorizontalSlider(new Rect(170, yPos, 100, 20), value, 0, 100);
                            field.SetValue(component, value);
                        }
                        else if (fieldType == "System.Boolean") // Comparar con el nombre completo del tipo
                        {
                            bool value = ConvertToBool(fieldValue);
                            value = GUI.Toggle(new Rect(170, yPos, 20, 20), value, "");
                            field.SetValue(component, value);
                        }
                        else if (fieldType == "System.String") // Comparar con el nombre completo del tipo
                        {
                            string value = ConvertToString(fieldValue);
                            value = GUI.TextField(new Rect(170, yPos, 100, 20), value);
                            field.SetValue(component, value);
                        }

                        yPos += 25;
                    }

                    // Mostrar métodos del componente
                    var methods = component.GetIl2CppType().GetMethods(Il2CppSystem.Reflection.BindingFlags.Public | Il2CppSystem.Reflection.BindingFlags.Instance);
                    foreach (var method in methods)
                    {
                        if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                        {
                            continue; // Ignorar getters y setters
                        }

                        if (method.GetParameters().Length == 0)
                        {
                            if (GUI.Button(new Rect(20, yPos, 150, 20), $"Ejecutar {method.Name}"))
                            {
                                method.Invoke(component, null);
                                MSLoader.Logger.LogInfo($"Método ejecutado: {method.Name}");
                            }
                            yPos += 25;
                        }
                    }

                    yPos += 10; // Espaciado entre componentes
                }
                catch (Exception ex)
                {
                    MSLoader.Logger.LogError($"Error dibujando componente: {ex}");
                }
                await Task.Yield();
            }
            return yPos;
        }

        private int ConvertToInt(Il2CppSystem.Object value)
        {
            return value != null && int.TryParse(value.ToString(), out int result) ? result : 0;
        }

        private float ConvertToFloat(Il2CppSystem.Object value)
        {
            return value != null && float.TryParse(value.ToString(), out float result) ? result : 0f;
        }

        private bool ConvertToBool(Il2CppSystem.Object value)
        {
            return value != null && bool.TryParse(value.ToString(), out bool result) ? result : false;
        }

        private string ConvertToString(Il2CppSystem.Object value)
        {
            return value?.ToString() ?? string.Empty;
        }

        /// <summary>
        /// Dibuja el panel de controles de cámara.
        /// </summary>
        private void DrawCameraControlsPanel()
        {
            if (!showCameraControlsPanel) return;

            GUI.Box(new Rect(Screen.width - 310, 10, 300, 250), "Control de Cámaras");

            cameraControlsScrollPosition = GUI.BeginScrollView(
                new Rect(Screen.width - 300, 40, 280, 200),
                cameraControlsScrollPosition,
                new Rect(0, 0, 260, 500)
            );

            float yPos = 0;

            // Botón para crear cámara dedicada
            if (GUI.Button(new Rect(0, yPos, 260, 30), "Crear Cámara Dedicada"))
            {
                //cameraManager.CreateDedicatedCamera().Wait(); //verificar si funciona asi?
                cameraManager.CreateDedicatedCamera().GetAwaiter().GetResult(); //verificar si funciona asi?
            }
            yPos += 40;

            // Botón para cambiar cámara
            if (GUI.Button(new Rect(0, yPos, 260, 30), "Cambiar Cámara"))
            {
                cameraManager.SwitchCamera().GetAwaiter().GetResult();
            }
            yPos += 40;

            // Lista de cámaras disponibles
            foreach (var camera in cameraManager.AllCameras)
            {
                if (camera == null) continue;

                bool isSelected = camera == cameraManager.SelectedCamera;
                if (GUI.Button(new Rect(0, yPos, 260, 20), $"{(isSelected ? "● " : "")}{camera.name}"))
                {
                    cameraManager.SelectCamera(camera).GetAwaiter().GetResult();
                }
                yPos += 25;
            }

            GUI.EndScrollView();
        }

        /// <summary>
        /// Dibuja el panel de selector de animaciones.
        /// </summary>
        // En GUIManager.cs
        private void DrawAnimationSelectorPanel()
        {
            if (!showAnimationSelectorPanel) return;

            GUI.Box(new Rect(Screen.width - 310, 270, 300, 250), "Selector de Animaciones");

            animationSelectorScrollPosition = GUI.BeginScrollView(
                new Rect(Screen.width - 300, 300, 280, 200),
                animationSelectorScrollPosition,
                new Rect(0, 0, 260, 500)
            );

            float yPos = 0;

            // Obtener animaciones disponibles desde AnimatorController
            List<string> animations = animatorController.GetAvailableAnimations().GetAwaiter().GetResult();

            foreach (var animationName in animations)
            {
                if (GUI.Button(new Rect(0, yPos, 260, 20), animationName))
                {
                    animatorController.ChangeAnimation(animationName).GetAwaiter().GetResult();
                }
                yPos += 25;
            }

            // Botón para cargar AnimationClips desde un AssetBundle
            if (GUI.Button(new Rect(0, yPos, 260, 30), "Cargar AnimationClips desde Bundle"))
            {

                foreach (var item in assetManager.GetAvailableAnimationClipsFromBundle().GetAwaiter().GetResult())
                {
                    MSLoader.Logger.LogWarning($"AnimationCLips Bundle: {item}");
                    if (GUI.Button(new Rect(0, yPos, 260, 20), item))
                    {
                        testHook.StartCoroutine(assetManager.LoadAnimatorFromBundle(bundleName, item)).GetAwaiter().GetResult();
                    }
                    yPos += 25;
                }

                //testHook.StartCoroutine(LoadAnimationClipsFromBundle());
            }

            GUI.EndScrollView();
        }

        private IEnumerator LoadAnimationClipsFromBundle()
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                MSLoader.Logger.LogError("Nombre del bundle no especificado.");
                yield break;
            }

            // Cargar el bundle
            //yield return assetManager.LoadAssetBundle(bundleName);

            // Cargar AnimationClips desde el bundle
            yield return assetManager.LoadAnimationClipsFromBundle(bundleName);
        }

        private IEnumerator LoadAnimatorFromBundle()
        {
            if (string.IsNullOrEmpty(bundleName))
            {
                MSLoader.Logger.LogError("Nombre del bundle no especificado.");
                yield break;
            }

            // Cargar el bundle
            //yield return assetManager.LoadAssetBundle(bundleName);

            // Obtener el AnimatorController del bundle
            AssetBundle bundle = assetManager.GetLoadedBundle(bundleName);
            if (bundle == null)
            {
                MSLoader.Logger.LogError($"Bundle no cargado: {bundleName}");
                yield break;
            }

            string[] assetNames = bundle.GetAllAssetNames();
            foreach (string assetName in assetNames)
            {
                MSLoader.Logger.LogWarning($"Asset: {assetName}");
                if (assetName.EndsWith(".controller"))
                {
                    yield return assetManager.LoadAnimatorFromBundle(bundleName, assetName);
                    break; // Solo cargar el primer AnimatorController encontrado
                }
            }
        }

        /// <summary>
        /// Alterna la visibilidad de un panel específico.
        /// </summary>
        public void TogglePanel(string panelName)
        {
            switch (panelName.ToLower())
            {
                case "jerarquia":
                    showHierarchyPanel = !showHierarchyPanel;
                    break;
                case "inspector":
                    showInspectorPanel = !showInspectorPanel;
                    break;
                case "camara":
                    showCameraControlsPanel = !showCameraControlsPanel;
                    break;
                case "animacion":
                    showAnimationSelectorPanel = !showAnimationSelectorPanel;
                    break;
                default:
                    MSLoader.Logger.LogWarning($"Panel desconocido: {panelName}");
                    break;
            }
        }

        /// <summary>
        /// Dibuja el inspector de Transform.
        /// </summary>
        private void DrawTransformInspector(Transform transform, ref float yPos)
        {
            GUI.Label(new Rect(10, yPos, 360, 20), "Transform");
            yPos += 20;

            // Position
            Vector3 position = transform.localPosition;
            GUI.Label(new Rect(20, yPos, 100, 20), "Position:");
            position = Vector3GUI(yPos, position);
            transform.localPosition = position;
            yPos += 20;

            // Rotación
            Vector3 rotation = transform.localEulerAngles;
            GUI.Label(new Rect(20, yPos, 100, 20), "Rotation:");
            rotation = Vector3GUI(yPos, rotation);
            transform.localEulerAngles = rotation;
            yPos += 20;

            // Escala
            Vector3 scale = transform.localScale;
            GUI.Label(new Rect(20, yPos, 100, 20), "Scale:");
            scale = Vector3GUI(yPos, scale);
            transform.localScale = scale;
            yPos += 20;
        }
        private Vector3 Vector3GUI(float yPos, Vector3 vector)
        {
            float[] values = new float[3] { vector.x, vector.y, vector.z };
            for (int i = 0; i < 3; i++)
            {
                string input = GUI.TextField(new Rect(120 + (i * 80), yPos, 70, 20), values[i].ToString());
                values[i] = float.TryParse(input, out float result) ? result : values[i];
            }
            return new Vector3(values[0], values[1], values[2]);
        }

        private Vector2 Vector2GUI(float yPos, Vector2 vector)
        {
            float[] values = new float[2] { vector.x, vector.y };
            for (int i = 0; i < 2; i++)
            {
                string input = GUI.TextField(new Rect(120 + (i * 80), yPos, 70, 20), values[i].ToString());
                values[i] = float.TryParse(input, out float result) ? result : values[i];
            }
            return new Vector2(values[0], values[1]);
        }

        /// <summary>
        /// Dibuja el inspector de RectTransform.
        /// </summary>
        private void DrawRectTransformInspector(RectTransform rectTransform, ref float yPos)
        {
            if (rectTransform == null) return;

            GUI.Label(new Rect(10, yPos, 360, 20), "RectTransform");
            yPos += 20;

            // Anchored Position
            Vector2 anchoredPosition = rectTransform.anchoredPosition;
            GUI.Label(new Rect(20, yPos, 100, 20), "Position:");
            anchoredPosition = Vector2GUI(yPos, anchoredPosition);
            rectTransform.anchoredPosition = anchoredPosition;
            yPos += 25;

            // Size Delta
            Vector2 sizeDelta = rectTransform.sizeDelta;
            GUI.Label(new Rect(20, yPos, 100, 20), "Size:");
            sizeDelta = Vector2GUI(yPos, sizeDelta);
            rectTransform.sizeDelta = sizeDelta;
            yPos += 25;

            // Pivot
            Vector2 pivot = rectTransform.pivot;
            GUI.Label(new Rect(20, yPos, 100, 20), "Pivot:");
            pivot = Vector2GUI(yPos, pivot);
            rectTransform.pivot = pivot;
            yPos += 25;

            // Anchors
            GUI.Label(new Rect(20, yPos, 100, 20), "Anchors:");
            Vector2 anchorMin = rectTransform.anchorMin;
            Vector2 anchorMax = rectTransform.anchorMax;
            anchorMin = Vector2GUI(yPos, anchorMin);
            yPos += 20;
            anchorMax = Vector2GUI(yPos, anchorMax);
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;

        }

        private void DrawAnimatorControls(Animator animator, ref float yPos)
        {
            if (animator == null) return;

            // Botón para habilitar/deshabilitar
            bool isEnabled = animator.enabled;
            bool newEnabled = GUI.Toggle(new Rect(25, yPos, 150, 20), isEnabled, "Habilitado");
            if (newEnabled != isEnabled)
            {
                animator.enabled = newEnabled;
            }
            yPos += 25;

            // Botón para reiniciar
            if (GUI.Button(new Rect(25, yPos, 120, 20), "Reiniciar"))
            {
                animator.Rebind();
            }
            yPos += 25;

            // Slider para ajustar el tiempo
            float newTime = GUI.HorizontalSlider(new Rect(25, yPos, 200, 20), animator.GetCurrentAnimatorStateInfo(0).normalizedTime, 0f, 1f);
            if (newTime != animator.GetCurrentAnimatorStateInfo(0).normalizedTime)
            {
                animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, 0, newTime);
                animator.Update(0); // Forzar actualización
            }
            yPos += 30;
        }

        /// <summary>
        /// Actualiza el objeto seleccionado y su Animator correspondiente.
        /// </summary>
        private void UpdateSelectedObject(GameObject obj)
        {
            if (obj == null) return;

            // Actualizar el objeto seleccionado en la jerarquía
            objectHierarchy.SelectObject(obj);

            // Verificar si el objeto tiene un componente Animator
            MSLoader.Logger.LogWarning($"Obteniendo Animator de {obj.name}");
            Animator animator = obj.GetComponent<Animator>();
            if (animator == null) MSLoader.Logger.LogError("No hay animator?");
            animatorController.SetSelectedAnimator(animator);

            // Limpiar la animación seleccionada si no hay Animator
            if (animator == null)
            {
                MSLoader.Logger.LogWarning("No se encontró Animator en el objeto seleccionado.");
            }
            else
            {
                MSLoader.Logger.LogInfo($"Animator asignado: {obj.name}");
            }
        }

        public void OnDestroy()
        {
            objectHierarchy.OnObjectSelected -= UpdateSelectedObject;
        }


        private void DrawBundleLoader()
        {
            float screenWidth = Screen.width;
            float panelWidth = 320;
            float margin = 200;
            float xPos = screenWidth - panelWidth - margin;
            float yPos = 10;
            float elementWidth = panelWidth - 20;

            GUI.Box(new Rect(xPos, yPos, panelWidth, 250), "Bundle Manager");
            yPos += 30;

            GUI.Label(new Rect(xPos + 10, yPos, 100, 20), "Nombre del Bundle:");
            bundleName = GUI.TextField(new Rect(xPos + 10, yPos + 20, elementWidth, 25), bundleName);
            yPos += 50;

            float buttonWidth = (elementWidth - 10) / 2;
            if (GUI.Button(new Rect(xPos + 10, yPos, buttonWidth, 30), "Cargar Escenas"))
            {
                testHook.StartCoroutine(assetManager.LoadSceneBundle(bundleName)).GetAwaiter().GetResult();
            }

            if (GUI.Button(new Rect(xPos + 20 + buttonWidth, yPos, buttonWidth, 30), "Cargar Assets"))
            {
                testHook.StartCoroutine(assetManager.LoadAssetBundle(bundleName)).GetAwaiter().GetResult();
            }

            if (GUI.Button(new Rect(xPos + 20 + buttonWidth, yPos, buttonWidth, 30), "Descargar Bundle"))
            {
                testHook.StartCoroutine(assetManager.UnloadAssetBundle(bundleName)).GetAwaiter().GetResult();
            }

            yPos += 40;

            GUI.Box(new Rect(xPos + 10, yPos, elementWidth, 160), "Contenido");

            var scenes = assetManager.GetAvailableScenes;
            scrollPos = GUI.BeginScrollView(
                new Rect(xPos + 15, yPos + 20, elementWidth - 10, 140),
                scrollPos,
                new Rect(0, 0, elementWidth - 25, scenes.Count * 25)
            );

            float contentY = 0;
            foreach (string scene in assetManager.GetAvailableScenes)
            {
                if (GUI.Button(new Rect(0, contentY, elementWidth - 25, 20), $"Escena: {scene}"))
                {
                    // Cargar escena desde bundle
                    testHook.StartCoroutine(
                       sceneLoader.LoadSceneFromBundle(
                            assetManager.GetavailableSceneBundle,
                           bundleName,
                           scene,
                           LoadSceneMode.Single
                       )
                   ).GetAwaiter().GetResult();
                }
                contentY += 22;
            }

            GUI.EndScrollView();

            yPos += 170;
            GUI.Box(new Rect(xPos + 10, yPos, elementWidth, 160), "Assets Disponibles");
            var assets = assetManager.GetAvailableAssets;
            scrollPosAssets = GUI.BeginScrollView(
                new Rect(xPos + 15, yPos + 20, elementWidth - 10, 140),
                scrollPosAssets,
                new Rect(0, 0, elementWidth - 25, assets.Count * 25)
            );

            contentY = 0;
            foreach (var asset in assetManager.GetAvailableAssets)
            {
                if (GUI.Button(new Rect(0, contentY, elementWidth - 25, 20), $"Asset: {asset.Key} [{asset.Value.GetIl2CppType().Name}]"))
                {

                    // Verifica si ya existe una luz direccional
                    if (!GameObject.Find("Bundle_Light"))
                    {
                        // Crea una iluminación para poder ver claramente los objetos instanciados
                        var light = new GameObject("Bundle_Light").AddComponent<Light>();
                        light.type = LightType.Directional;
                        light.intensity = 1.5f;
                    }

                    assetManager.InstantiateAssetFromBundle(bundleName, asset.Key).GetAwaiter().GetResult();
                }
                contentY += 22;
            }

            GUI.EndScrollView();
        }
    }
}
