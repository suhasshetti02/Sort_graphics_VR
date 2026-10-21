using UnityEngine;

namespace CollisionLab
{
    /// <summary>
    /// Visualizes the PhysX collision manifold contact point and contact normal vector in 3D world space.
    /// Provides clear geometric insight into the collision plane and impulse direction for CGVR viva demonstration.
    /// </summary>
    public class ContactManifoldVisualizer : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private Color normalColor = new Color(1.0f, 0.25f, 0.45f, 1.0f); // Bright Coral / Magenta
        [SerializeField] private Color pointColor = new Color(0.0f, 1.0f, 0.75f, 1.0f);  // Bright Aquamarine
        [SerializeField] private float normalLength = 1.4f;
        [SerializeField] private float lineWidth = 0.05f;
        [SerializeField] private float pointMarkerScale = 0.18f;

        private LineRenderer _normalRenderer;
        private GameObject _pointMarker;
        private Material _lineMaterial;
        private Material _markerMaterial;

        public bool IsActive => (_normalRenderer != null && _normalRenderer.enabled) || (_pointMarker != null && _pointMarker.activeSelf);

        private void Awake()
        {
            SetupVisualizers();
        }

        private void SetupVisualizers()
        {
            // Setup Normal LineRenderer
            _normalRenderer = GetComponent<LineRenderer>();
            if (_normalRenderer == null)
            {
                _normalRenderer = gameObject.AddComponent<LineRenderer>();
            }

            var shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Unlit/Color");

            if (shader != null)
            {
                _lineMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                _normalRenderer.material = _lineMaterial;
            }

            _normalRenderer.useWorldSpace = true;
            _normalRenderer.positionCount = 5;
            _normalRenderer.startWidth = lineWidth;
            _normalRenderer.endWidth = lineWidth;
            _normalRenderer.startColor = normalColor;
            _normalRenderer.endColor = normalColor;
            _normalRenderer.numCapVertices = 4;
            _normalRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _normalRenderer.receiveShadows = false;
            _normalRenderer.enabled = false;

            // Setup Contact Point Marker Sphere
            if (_pointMarker == null)
            {
                _pointMarker = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                _pointMarker.name = "ContactPointMarker";
                _pointMarker.transform.SetParent(transform, false);
                _pointMarker.transform.localScale = Vector3.one * pointMarkerScale;

                var col = _pointMarker.GetComponent<Collider>();
                if (col != null) Destroy(col); // No physics on visual marker

                var renderer = _pointMarker.GetComponent<Renderer>();
                if (renderer != null && shader != null)
                {
                    _markerMaterial = new Material(shader) { hideFlags = HideFlags.DontSave };
                    _markerMaterial.color = pointColor;
                    renderer.material = _markerMaterial;
                }

                _pointMarker.SetActive(false);
            }
        }

        /// <summary>
        /// Displays the contact point sphere and normal vector arrow at the given collision manifold location.
        /// </summary>
        public void ShowContact(Vector3 contactPoint, Vector3 contactNormal)
        {
            if (_normalRenderer == null) SetupVisualizers();

            var normalizedNormal = contactNormal.normalized;
            if (normalizedNormal.sqrMagnitude < 0.001f)
            {
                normalizedNormal = Vector3.up;
            }

            // Position point marker
            if (_pointMarker != null)
            {
                _pointMarker.transform.position = contactPoint;
                _pointMarker.SetActive(true);
            }

            // Draw normal arrow
            if (_normalRenderer != null)
            {
                _normalRenderer.enabled = true;
                var tip = contactPoint + normalizedNormal * normalLength;

                var up = Vector3.up;
                if (Mathf.Abs(Vector3.Dot(normalizedNormal, up)) > 0.9f)
                {
                    up = Vector3.forward;
                }
                var right = Vector3.Cross(normalizedNormal, up).normalized;

                var wingBase = tip - normalizedNormal * 0.25f;
                var wing1 = wingBase + right * 0.12f;
                var wing2 = wingBase - right * 0.12f;

                _normalRenderer.SetPosition(0, contactPoint);
                _normalRenderer.SetPosition(1, tip);
                _normalRenderer.SetPosition(2, wing1);
                _normalRenderer.SetPosition(3, tip);
                _normalRenderer.SetPosition(4, wing2);
            }
        }

        /// <summary>
        /// Clears the contact point marker and normal raycast vector.
        /// </summary>
        public void Clear()
        {
            if (_normalRenderer != null)
            {
                _normalRenderer.enabled = false;
            }

            if (_pointMarker != null)
            {
                _pointMarker.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (_lineMaterial != null) Destroy(_lineMaterial);
            if (_markerMaterial != null) Destroy(_markerMaterial);
        }
    }
}
