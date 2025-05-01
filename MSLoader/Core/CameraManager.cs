using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MSALoader.Components;
using UnityEngine;

namespace MSALoader.Core
{
    public class CameraManager
    {
        // Lista de todas las cámaras detectadas
        public List<Camera> allCameras = new List<Camera>();

        // Cámara dedicada para depuración
        public Camera dedicatedCamera;
        private bool useDedicatedCamera = false;

        // Índice de la cámara seleccionada
        public int selectedCameraIndex = 0;

        // Estado de la cámara libre
        private static bool isFreeCameraActive = false;
        public Camera freeCamera;
        public Camera FreeCamera => CameraDebugController.freeCamera;

        //private CameraDebugController cameraDebugController;

        /// <summary>
        /// Actualiza la lista de cámaras disponibles.
        /// </summary>
        public async Task UpdateCamerasList()
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
                    await Task.Yield();
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
        public async Task ToggleFreeCamera(bool active)
        {
            try
            {
                MSLoader.Logger.LogWarning($"ToggleFreeCamera called with active={active}, current isFreeCameraActive={isFreeCameraActive}");

                if (active == isFreeCameraActive) return;

                // Alternar la cámara libre
                isFreeCameraActive = active;
                await CameraDebugController.ToggleFreeCamera(Camera.main, active);

                MSLoader.Logger.LogInfo(isFreeCameraActive ? "Cámara libre activada" : "Cámara libre desactivada");
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error alternando cámara libre: {ex}");
            }
            await Task.Yield();
        }

        /// <summary>
        /// Cambia la posición de la cámara dedicada a la siguiente cámara disponible.
        /// </summary>
        public async Task SwitchToNextCamera()
        {
            try
            {
                if (allCameras == null || allCameras.Count == 0)
                {
                    MSLoader.Logger.LogWarning("No hay cámaras disponibles para alternar.");
                    return;
                }

                MSLoader.Logger.LogWarning("Buscando siguiente camara...");

                // Incrementar el índice de la cámara seleccionada
                selectedCameraIndex = (selectedCameraIndex + 1) % allCameras.Count;

                MSLoader.Logger.LogWarning($"[CameraManager] Camara Selecionada: {selectedCameraIndex}");


                Camera nextCamera = allCameras[selectedCameraIndex];

                MSLoader.Logger.LogWarning($"[CameraManager] Nombre de Camara: {nextCamera.name}");


                if (nextCamera != null && FreeCamera != null)
                {
                    MSLoader.Logger.LogWarning("ajustando posicion...");
                    FreeCamera.transform.position = nextCamera.transform.position;
                    FreeCamera.transform.rotation = nextCamera.transform.rotation;

                    MSLoader.Logger.LogInfo($"Cámara dedicada posicionada en: {nextCamera.name}");
                    await CameraDebugController.UpdateDedicatedCameraPosition();
                }
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error cambiando a la siguiente cámara: {ex}");
            }
        }

        /// <summary>
        /// Actualiza los controles de la cámara libre.
        /// </summary>
        public async Task UpdateFreeCamera()
        {
            if (isFreeCameraActive)
            {
                await CameraDebugController.UpdateFreeCamera();
            }
        }

        /// <summary>
        /// Dibuja la interfaz gráfica para la cámara libre.
        /// </summary>
        public async Task DrawGUI()
        {
            if (isFreeCameraActive)
            {
                await CameraDebugController.DrawGUI();
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
        public async Task SwitchCamera()
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
            await Task.Yield();
        }

        /// <summary>
        /// Selecciona una cámara específica.
        /// </summary>
        public async Task SelectCamera(Camera camera)
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
            await Task.Yield();
        }



        /// <summary>
        /// Crea una cámara dedicada para depuración.
        /// </summary>
        public async Task CreateDedicatedCamera()
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
                await UpdateCamerasList();
            }
        }

        public async Task DestroyFreeCamera()
        {
            if (freeCamera != null)
            {
                // Notificar a MSBootstrap que debe destruir la cámara
                freeCamera.enabled = false;
                freeCamera = null;
            }
            await Task.Yield();
        }


    }
}
