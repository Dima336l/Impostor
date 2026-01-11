using UnityEngine;

namespace Impostor.Camera
{
    /// <summary>
    /// Controls the POV camera view at the table, providing an immersive first-person perspective.
    /// </summary>
    public class TableCameraController : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float verticalRotationLimit = 60f;
        [SerializeField] private float cameraHeight = 1.6f; // Average eye height
        [SerializeField] private bool invertY = false;

        [Header("Smoothing")]
        [SerializeField] private float rotationSmoothing = 10f;
        [SerializeField] private float positionSmoothing = 10f;

        [Header("Table Position")]
        [SerializeField] private Transform tableCenter;
        [SerializeField] private float distanceFromTable = 0.5f;

        private Camera _camera;
        private float _verticalRotation = 0f;
        private float _horizontalRotation = 0f;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            if (_camera == null)
            {
                _camera = gameObject.AddComponent<Camera>();
            }

            // Set up camera defaults
            _camera.fieldOfView = 75f;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 100f;
        }

        private void Start()
        {
            if (tableCenter == null)
            {
                GameObject tableObj = GameObject.FindGameObjectWithTag("Table");
                if (tableObj != null)
                {
                    tableCenter = tableObj.transform;
                }
            }

            InitializeCameraPosition();
        }

        private void Update()
        {
            HandleMouseLook();
            UpdateCameraPosition();
        }

        private void InitializeCameraPosition()
        {
            if (tableCenter != null)
            {
                // Position camera at table edge, looking toward center
                Vector3 directionToCenter = (tableCenter.position - transform.position).normalized;
                if (directionToCenter == Vector3.zero)
                {
                    directionToCenter = Vector3.forward;
                }

                _targetPosition = tableCenter.position - directionToCenter * distanceFromTable;
                _targetPosition.y = tableCenter.position.y + cameraHeight;

                transform.position = _targetPosition;
                transform.LookAt(tableCenter.position);

                _horizontalRotation = transform.eulerAngles.y;
                _verticalRotation = -transform.eulerAngles.x;
            }
            else
            {
                // Default position
                _targetPosition = new Vector3(0, cameraHeight, -distanceFromTable);
                transform.position = _targetPosition;
                _horizontalRotation = 0f;
                _verticalRotation = 0f;
            }
        }

        private void HandleMouseLook()
        {
            if (!Input.GetMouseButton(1)) // Right mouse button held for look
            {
                return;
            }

            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            if (invertY)
            {
                mouseY = -mouseY;
            }

            _horizontalRotation += mouseX;
            _verticalRotation -= mouseY;
            _verticalRotation = Mathf.Clamp(_verticalRotation, -verticalRotationLimit, verticalRotationLimit);
        }

        private void UpdateCameraPosition()
        {
            if (tableCenter != null)
            {
                // Keep camera at table edge, rotating around it
                Vector3 direction = Quaternion.Euler(_verticalRotation, _horizontalRotation, 0) * Vector3.forward;
                _targetPosition = tableCenter.position - direction * distanceFromTable;
                _targetPosition.y = tableCenter.position.y + cameraHeight;
            }

            // Smooth position and rotation
            transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * positionSmoothing);
            _targetRotation = Quaternion.Euler(_verticalRotation, _horizontalRotation, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, _targetRotation, Time.deltaTime * rotationSmoothing);
        }

        public void SetTableCenter(Transform tableTransform)
        {
            tableCenter = tableTransform;
            InitializeCameraPosition();
        }

        public void ResetCamera()
        {
            InitializeCameraPosition();
        }

        public void SetCameraHeight(float height)
        {
            cameraHeight = height;
            if (tableCenter != null)
            {
                _targetPosition.y = tableCenter.position.y + cameraHeight;
            }
        }
    }
}

