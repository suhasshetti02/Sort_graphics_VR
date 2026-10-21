using UnityEngine;

namespace CollisionLab
{
    /// <summary>
    /// Renders a dynamic 3D velocity vector arrow for a Rigidbody using a Unity LineRenderer.
    /// Updates dynamically during motion and smoothly collapses to zero when stationary.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PhysicsVectorVisualizer : MonoBehaviour
    {
        [Header("Vector Settings")]
        [SerializeField] private Color vectorColor = Color.cyan;
        [SerializeField] private float lengthScale = 0.35f;
        [SerializeField] private float lineWidth = 0.06f;
        [SerializeField] private float arrowheadLength = 0.3f;
        [SerializeField] private float arrowheadWidth = 0.15f;
        [SerializeField] private float minSpeedThreshold = 0.08f;
        [SerializeField] private Vector3 originOffset = Vector3.zero;

        private Rigidbody _rb;
        private LineRenderer _lineRenderer;
        private Material _lineMaterial;

        public Color VectorColor
        {
            get => vectorColor;
            set
            {
                vectorColor = value;
                UpdateLineAppearance();
            }
        }

        public float LengthScale
        {
            get => lengthScale;
            set => lengthScale = value;
        }

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            SetupLineRenderer();
        }

        private void SetupLineRenderer()
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");
            if (shader != null)
            {
                _lineMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                _lineRenderer.material = _lineMaterial;
            }

            _lineRenderer.useWorldSpace = true;
            _lineRenderer.positionCount = 5;
            _lineRenderer.startWidth = lineWidth;
            _lineRenderer.endWidth = lineWidth;
            _lineRenderer.numCapVertices = 4;
            _lineRenderer.numCornerVertices = 4;
            _lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _lineRenderer.receiveShadows = false;

            UpdateLineAppearance();
        }

        private void UpdateLineAppearance()
        {
            if (_lineRenderer == null) return;

            _lineRenderer.startColor = vectorColor;
            _lineRenderer.endColor = vectorColor;
            if (_lineMaterial != null && _lineMaterial.HasProperty("_Color"))
            {
                _lineMaterial.color = vectorColor;
            }
        }

        private void LateUpdate()
        {
            UpdateVectorVisualization();
        }

        public void UpdateVectorVisualization()
        {
            if (_rb == null || _lineRenderer == null) return;

            var vel = _rb.velocity;
            var speed = vel.magnitude;

            if (speed < minSpeedThreshold)
            {
                _lineRenderer.enabled = false;
                return;
            }

            _lineRenderer.enabled = true;

            var origin = transform.position + originOffset;
            var dir = vel / speed;
            var tip = origin + vel * lengthScale;

            // Compute perpendicular vectors for arrowhead wings
            var up = Vector3.up;
            if (Mathf.Abs(Vector3.Dot(dir, up)) > 0.9f)
            {
                up = Vector3.forward;
            }
            var right = Vector3.Cross(dir, up).normalized;

            var wingBase = tip - dir * arrowheadLength;
            var wing1 = wingBase + right * arrowheadWidth;
            var wing2 = wingBase - right * arrowheadWidth;

            // 5-point arrow geometry: Origin -> Tip -> Wing1 -> Tip -> Wing2
            _lineRenderer.SetPosition(0, origin);
            _lineRenderer.SetPosition(1, tip);
            _lineRenderer.SetPosition(2, wing1);
            _lineRenderer.SetPosition(3, tip);
            _lineRenderer.SetPosition(4, wing2);
        }

        public void ClearVector()
        {
            if (_lineRenderer != null)
            {
                _lineRenderer.enabled = false;
            }
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null)
            {
                Destroy(_lineMaterial);
            }
        }
    }
}
