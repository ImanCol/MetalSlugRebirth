using System;
using System.Collections.Generic;
using MSALoader.Components;
using UnityEngine;

namespace MSALoader.Core
{
    public class CameraManager
    {
        // Lista de todas las cámaras detectadas
        private List<Camera> allCameras = new List<Camera>();

        // Cámara dedicada para depuración
        private Camera dedicatedCamera;
        private bool useDedicatedCamera = false;

        // Índice de la cámara seleccionada
        private int selectedCameraIndex = 0;

        // Estado de la cámara libre
        private bool isFreeCameraActive = false;
        public Camera freeCamera;
        public Camera FreeCamera => freeCamera;

        private CameraDebugController cameraDebugController;

        /// <summary>
        /// Actualiza la lista de cámaras disponibles.
        /// </summary>
        public void UpdateCamerasList()
        {
            try
            {
                allCameras.Clear();

                // Buscar todas las cámaras incluyendo inactivas
                var cameras = Resources.FindObjectsOfTypeAll<Camera>();
                foreach (var cam in cameras)
                {
                    if (cam != null && !allCameras.Contains(cam))
                    {
                        allCameras.Add(cam);
                    }
                }

                // Ordenar por nombre y profundidad
                allCameras = allCameras.OrderBy(c => c.depth).ThenBy(c => c.name).ToList();
                MSLoader.Logger.LogInfo($"Cámaras actualizadas: {allCameras.Count} encontradas");
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error actualizando cámaras: {ex}");
            }
        }

        /// <summary>
        /// Alterna la cámara libre.
        /// </summary>
        public void ToggleFreeCamera(bool active)
        {
            try
            {
                MSLoader.Logger.LogWarning($"ToggleFreeCamera called with active={active}, current isFreeCameraActive={isFreeCameraActive}");

                if (active == isFreeCameraActive) return;

                if (active)
                {
                    MSLoader.Logger.LogWarning("Attempting to create free camera");
                    // Crear cámara libre si no existe
                    if (freeCamera == null)
                    {
                        MSLoader.Logger.LogWarning("Creating new camera GameObject");
                        GameObject cameraGO = new GameObject("Free_Camera");

                        MSLoader.Logger.LogWarning("Adding Camera component");
                        freeCamera = cameraGO.AddComponent<Camera>();

                        MSLoader.Logger.LogWarning("Setting camera properties");
                        freeCamera.depth = 100; // Máxima prioridad
                        freeCamera.tag = "MainCamera";

                        MSLoader.Logger.LogWarning("Setting camera transform");
                        if (Camera.main != null)
                        {
                            freeCamera.transform.position = Camera.main.transform.position;
                            freeCamera.transform.rotation = Camera.main.transform.rotation;
                        }

                        MSLoader.Logger.LogWarning("Adding CameraDebugController component");
                        cameraDebugController = cameraGO.AddComponent<CameraDebugController>();
                    }

                    MSLoader.Logger.LogWarning("Switching camera states");
                    if (Camera.main != null)
                    {
                        Camera.main.enabled = false;
                    }
                    freeCamera.enabled = true;
                }
                else
                {
                    MSLoader.Logger.LogWarning("Attempting to restore main camera");
                    // Restaurar la cámara principal
                    if (freeCamera != null)
                    {
                        freeCamera.enabled = false;
                        if (Camera.main != null)
                        {
                            Camera.main.enabled = true;
                        }

                        MSLoader.Logger.LogWarning("Destroying free camera");
                        UnityEngine.Object.Destroy(freeCamera.gameObject);
                        freeCamera = null;
                        cameraDebugController = null;
                    }
                }

                isFreeCameraActive = active;
                MSLoader.Logger.LogInfo(isFreeCameraActive ? "Cámara libre activada" : "Cámara libre desactivada");
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error alternando cámara libre: {ex}");
            }
        }


        /// <summary>
        /// Verifica si la cámara libre está activa.
        /// </summary>
        public bool IsFreeCameraActive => isFreeCameraActive;

        /// <summary>
        /// Obtiene la lista de todas las cámaras.
        /// </summary>
        public List<Camera> AllCameras => allCameras;

        /// <summary>
        /// Obtiene la cámara seleccionada actualmente.
        /// </summary>
        public Camera SelectedCamera => allCameras.Count > 0 ? allCameras[selectedCameraIndex] : null;

        /// <summary>
        /// Cambia a la siguiente cámara en la lista.
        /// </summary>
        public void SwitchCamera()
        {
            if (allCameras.Count == 0) return;

            // Desactivar cámara actual
            if (selectedCameraIndex < allCameras.Count)
            {
                allCameras[selectedCameraIndex].enabled = false;
            }

            // Cambiar a la siguiente cámara
            selectedCameraIndex = (selectedCameraIndex + 1) % allCameras.Count;
            allCameras[selectedCameraIndex].enabled = true;
            allCameras[selectedCameraIndex].depth = 100;

            MSLoader.Logger.LogInfo($"Cámara activada: {allCameras[selectedCameraIndex].name}");
        }

        /// <summary>
        /// Selecciona una cámara específica.
        /// </summary>
        public void SelectCamera(Camera camera)
        {
            if (camera == null || !allCameras.Contains(camera)) return;

            // Desactivar cámara actual
            if (selectedCameraIndex < allCameras.Count)
            {
                allCameras[selectedCameraIndex].enabled = false;
            }

            // Activar la cámara seleccionada
            selectedCameraIndex = allCameras.IndexOf(camera);
            allCameras[selectedCameraIndex].enabled = true;
            allCameras[selectedCameraIndex].depth = 100;

            MSLoader.Logger.LogInfo($"Cámara seleccionada: {camera.name}");
        }



        /// <summary>
        /// Crea una cámara dedicada para depuración.
        /// </summary>
        public void CreateDedicatedCamera()
        {
            if (dedicatedCamera == null)
            {
                GameObject cameraGO = new GameObject("Dedicated_Camera");
                dedicatedCamera = cameraGO.AddComponent<Camera>();
                dedicatedCamera.depth = 100; // Máxima prioridad
                dedicatedCamera.tag = "MainCamera";

                // Posicionar cerca del jugador
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    dedicatedCamera.transform.position = player.transform.position + new Vector3(0, 2, -5);
                }
                else
                {
                    dedicatedCamera.transform.position = new Vector3(0, 2, -5);
                }

                dedicatedCamera.transform.rotation = Quaternion.Euler(20, 0, 0);

                // Actualizar lista de cámaras
                UpdateCamerasList();
            }
        }

        public void DestroyFreeCamera()
        {
            if (freeCamera != null)
            {
                // Notificar a MSBootstrap que debe destruir la cámara
                freeCamera.enabled = false;
                freeCamera = null;
            }
        }


    }
}
