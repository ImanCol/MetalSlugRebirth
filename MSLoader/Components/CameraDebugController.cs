using UnityEngine;

namespace MSALoader.Components
{
    public class CameraDebugController : MonoBehaviour
    {
        // Constructor obligatorio
        public CameraDebugController(IntPtr ptr) : base(ptr) { }

        // Configuración de velocidad
        public float freeCameraSpeed = 5f;
        public float freeCameraRotationSpeed = 100f;

        // Estado inicial de la cámara
        private Vector3 originalCameraPosition;
        private Quaternion originalCameraRotation;

        // Sistema de movimiento suavizado
        private Vector3 currentVelocity;
        private Vector3 targetRotation;
        private Vector3 currentRotationVelocity;

        // Sistema de zoom
        private float zoomVelocity;
        private const float zoomSmoothTime = 0.1f;

        // Deadzones para evitar vibraciones residuales
        private float rotationDeadZone = 0.01f;
        private float movementDeadZone = 0.001f;

        // Constantes de aceleración y frenado
        private const float acceleration = 8f;
        private const float deceleration = 12f;

        /// <summary>
        /// Inicialización de la cámara.
        /// </summary>
        private void Start()
        {
            try
            {
                // Guardar posición y rotación inicial
                originalCameraPosition = transform.position;
                originalCameraRotation = transform.rotation;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error en Start: {ex}");
            }
        }

        /// <summary>
        /// Actualización continua de la cámara.
        /// </summary>
        private void Update()
        {
            HandleFreeCameraMovement();
        }

        /// <summary>
        /// Maneja el movimiento y rotación de la cámara libre.
        /// </summary>
        private void HandleFreeCameraMovement()
        {
            try
            {
                Camera targetCamera = Camera.main; // Usar la cámara principal

                if (targetCamera == null) return;

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
                targetCamera.transform.rotation = Quaternion.Slerp(
                    targetCamera.transform.rotation,
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
                Vector3 worldDirection = targetCamera.transform.TransformDirection(rawDirection);

                // Suavizar movimiento con aceleración no lineal
                float speedMultiplier = Input.GetKey(KeyCode.LeftAlt) ? 3f : 1f;
                Vector3 targetVelocity = worldDirection * freeCameraSpeed * speedMultiplier;

                currentVelocity = Vector3.Lerp(
                    currentVelocity,
                    targetVelocity,
                    Time.deltaTime * (targetVelocity != Vector3.zero ? acceleration : deceleration)
                );

                // Aplicar movimiento suavizado
                targetCamera.transform.position += currentVelocity * Time.deltaTime;

                // 3. Zoom con la rueda del mouse
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    float zoomDelta = scroll * 10f;
                    targetCamera.fieldOfView = Mathf.Clamp(
                        Mathf.SmoothDamp(
                            targetCamera.fieldOfView,
                            targetCamera.fieldOfView - zoomDelta,
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
        private void OnGUI()
        {
            GUI.Box(new Rect(10, 100, 200, 120), "Controles de Cámara");
            GUI.Label(new Rect(20, 130, 180, 20), "WASD/QE - Movimiento");
            GUI.Label(new Rect(20, 150, 180, 20), "Mouse - Rotación");
            GUI.Label(new Rect(20, 170, 180, 20), "Rueda Mouse - Zoom");
            GUI.Label(new Rect(20, 190, 180, 20), "ALT - Aumentar Velocidad");
        }

        /// <summary>
        /// Método estático para inicializar la cámara desde otros sistemas.
        /// </summary>
        internal static void OnMainCameraStart(Camera instance)
        {
            try
            {
                if (instance == null)
                {
                    Debug.LogError("La cámara proporcionada es nula.");
                    return;
                }

                Debug.Log("Cámara inicializada correctamente.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error en OnMainCameraStart: {ex}");
            }
        }
    }
}
