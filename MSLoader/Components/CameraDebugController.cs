using System.Threading.Tasks;
using UnityEngine;

namespace MSALoader.Components
{
    public static class CameraDebugController
    {
        // Configuración de velocidad
        private static float freeCameraSpeed = 5f;
        private static float freeCameraRotationSpeed = 100f;

        // Estado inicial de la cámara
        private static Vector3 originalCameraPosition;
        private static Quaternion originalCameraRotation;

        // Sistema de movimiento suavizado
        private static Vector3 currentVelocity;
        private static Vector3 targetRotation;
        private static Vector3 currentRotationVelocity;

        // Sistema de zoom
        private static float zoomVelocity;
        private const float zoomSmoothTime = 0.1f;

        // Deadzones para evitar vibraciones residuales
        private static float rotationDeadZone = 0.01f;
        private static float movementDeadZone = 0.001f;

        // Constantes de aceleración y frenado
        private const float acceleration = 8f;
        private const float deceleration = 12f;

        // Referencia a la cámara libre
        public static Camera freeCamera;

        /// <summary>
        /// Activa o desactiva la cámara libre.
        /// </summary>
        public static async Task ToggleFreeCamera(Camera mainCamera, bool active)
        {
            if (active)
            {
                if (freeCamera == null)
                {
                    await CreateFreeCamera(mainCamera);
                }

                freeCamera.enabled = true;
                freeCamera.depth = 100; // Máxima prioridad

                // Actualizar posición si el booleano está activo
                //if (positionDedicatedCamera)
                {
                    await UpdateDedicatedCameraPosition();
                }

            }
            else
            {
                if (freeCamera != null)
                {
                    freeCamera.enabled = false;
                }
            }
        }

        /// <summary>
        /// Crea la cámara libre si no existe.
        /// </summary>
        private static async Task CreateFreeCamera(Camera mainCamera)
        {
            GameObject cameraGO = new GameObject("Free_Camera");
            freeCamera = cameraGO.AddComponent<Camera>();
            freeCamera.depth = 100; // Máxima prioridad
            freeCamera.tag = "MainCamera";

            // Posicionar cerca de la cámara principal
            if (mainCamera != null)
            {
                freeCamera.transform.position = mainCamera.transform.position;
                freeCamera.transform.rotation = mainCamera.transform.rotation;
            }
            else
            {
                freeCamera.transform.position = new Vector3(0, 2, -5);
                freeCamera.transform.rotation = Quaternion.Euler(20, 0, 0);
            }

            // Guardar posición y rotación inicial
            originalCameraPosition = freeCamera.transform.position;
            originalCameraRotation = freeCamera.transform.rotation;
            await Task.Yield();
        }


        /// <summary>
        /// Actualiza la posición de la cámara dedicada con la última cámara activa.
        /// </summary>
        public static async Task UpdateDedicatedCameraPosition()
        {
            try
            {
                if (testHook.cameraManager.dedicatedCamera == null || testHook.cameraManager.allCameras.Count == 0) return;

                Camera lastActiveCamera = testHook.cameraManager.allCameras[testHook.cameraManager.selectedCameraIndex];

                if (lastActiveCamera != null)
                {
                    testHook.cameraManager.dedicatedCamera.transform.position = lastActiveCamera.transform.position;
                    testHook.cameraManager.dedicatedCamera.transform.rotation = lastActiveCamera.transform.rotation;

                    MSLoader.Logger.LogInfo("Posición de la cámara dedicada actualizada con la última cámara activa.");
                }
            }
            catch (Exception ex)
            {
                MSLoader.Logger.LogError($"Error actualizando posición de la cámara dedicada: {ex}");
            }
            await Task.Yield();
        }


        /// <summary>
        /// Actualiza los controles de la cámara libre.
        /// </summary>
        public static void UpdateFreeCamera()
        {
            if (freeCamera == null || !freeCamera.enabled) return;
            HandleFreeCameraMovement().GetAwaiter().GetResult();
        }

        /// <summary>
        /// Maneja el movimiento y rotación de la cámara libre.
        /// </summary>
        private static async Task HandleFreeCameraMovement()
        {
            try
            {
                // 1. Rotación con el mouse
                if (Input.GetMouseButton(1)) // Botón derecho del mouse
                {
                    Vector2 mouseDelta = new Vector2(
                        Input.GetAxis("Mouse X"),
                        Input.GetAxis("Mouse Y")
                    );

                    // Calcular rotación objetivo
                    Vector3 targetRotationDelta = new Vector3(
                        -mouseDelta.y * freeCameraRotationSpeed * Time.deltaTime,
                        mouseDelta.x * freeCameraRotationSpeed * Time.deltaTime,
                        0
                    );

                    targetRotation += targetRotationDelta;

                    // Limitar rotación vertical
                    targetRotation.x = Mathf.Clamp(targetRotation.x, -89f, 89f);
                }

                // Suavizar rotación usando Quaternions
                Quaternion targetQuaternion = Quaternion.Euler(targetRotation);
                freeCamera.transform.rotation = Quaternion.Slerp(
                    freeCamera.transform.rotation,
                    targetQuaternion,
                    Time.deltaTime * 10f
                );

                // 2. Movimiento con teclas WASD/QE
                Vector3 rawDirection = new Vector3(
                    Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0,
                    Input.GetKey(KeyCode.E) ? 1 : Input.GetKey(KeyCode.Q) ? -1 : 0,
                    Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0
                );

                // Aplicar deadzone de movimiento
                if (rawDirection.magnitude < movementDeadZone) rawDirection = Vector3.zero;

                // Calcular dirección relativa a la rotación de la cámara
                Vector3 worldDirection = freeCamera.transform.TransformDirection(rawDirection);

                // Suavizar movimiento con aceleración no lineal
                float speedMultiplier = Input.GetKey(KeyCode.LeftAlt) ? 3f : 1f;
                Vector3 targetVelocity = worldDirection * freeCameraSpeed * speedMultiplier;

                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    Time.deltaTime * (targetVelocity != Vector3.zero ? acceleration : deceleration)
                );

                // Aplicar movimiento suavizado
                freeCamera.transform.position += currentVelocity * Time.deltaTime;

                // 3. Zoom con la rueda del mouse
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    float zoomDelta = scroll * 10f;
                    freeCamera.fieldOfView = Mathf.Clamp(
                        Mathf.SmoothDamp(
                            freeCamera.fieldOfView,
                            freeCamera.fieldOfView - zoomDelta,
                            ref zoomVelocity,
                            zoomSmoothTime
                        ),
                        20f,
                        120f
                    );
                }

                // 4. Eliminar vibración residual
                if (rawDirection == Vector3.zero && currentVelocity.magnitude < 0.001f)
                {
                    currentVelocity = Vector3.zero;
                }

                if (!Input.GetMouseButton(1) && currentRotationVelocity.magnitude < rotationDeadZone)
                {
                    currentRotationVelocity = Vector3.zero;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error en movimiento cámara: {ex}");
            }
        }

        /// <summary>
        /// Dibuja la interfaz gráfica con los controles de la cámara.
        /// </summary>
        public static async Task DrawGUI()
        {
            GUI.Box(new Rect(10, 100, 200, 120), "Controles de Cámara");
            GUI.Label(new Rect(20, 130, 180, 20), "WASD/QE - Movimiento");
            GUI.Label(new Rect(20, 150, 180, 20), "Mouse - Rotación");
            GUI.Label(new Rect(20, 170, 180, 20), "Rueda Mouse - Zoom");
            GUI.Label(new Rect(20, 190, 180, 20), "ALT - Aumentar Velocidad");
            await Task.Yield();
        }
    }
}