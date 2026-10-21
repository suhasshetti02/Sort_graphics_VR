using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform mainCamera;

    #region Camera Movement Parameters

    public float moveSpeed         = 1.0f;
    public float rotateSpeed       = 90.0f;
    public float shiftRate         = 2.0f; // Hold Shift to accelerate
    public float scrollSensitivity = 10.0f; // Mouse scroll wheel zoom speed
    public float minDistance       = 0.5f; // Minimum distance to impassable surfaces (<= 0 permits penetration)

    #endregion

    #region Movement Velocity Components

    private Vector3 _direction = Vector3.zero;
    private Vector3 _speedForward;
    private Vector3 _speedBack;
    private Vector3 _speedLeft;
    private Vector3 _speedRight;
    private Vector3 _speedUp;
    private Vector3 _speedDown;

    #endregion

    void Start()
    {
        if ( mainCamera == null ) mainCamera = gameObject.transform;
    }

    void Update()
    {
        if ( Input.GetKeyDown( KeyCode.F ) )
        {
            GameManager.TriggerFrameCamera();
        }

        // Mouse scroll wheel zoom along viewing axis
        var scroll = Input.GetAxis( "Mouse ScrollWheel" );
        if ( Mathf.Abs( scroll ) > 0.0001f )
        {
            var zoomSpeed = Input.GetKey( KeyCode.LeftShift ) ? scrollSensitivity * shiftRate : scrollSensitivity;
            mainCamera.Translate( mainCamera.forward * ( scroll * zoomSpeed ), Space.World );
        }

        GetDirection();
        // Check proximity to impassable surfaces
        while ( Physics.Raycast( mainCamera.position, _direction, out var hit, minDistance ) )
        {
            // Cancel out velocity component normal to the surface
            var angel     = Vector3.Angle( _direction, hit.normal );
            var magnitude = Vector3.Magnitude( _direction ) * Mathf.Cos( Mathf.Deg2Rad * ( 180 - angel ) );
            _direction += hit.normal * magnitude;
        }

        mainCamera.Translate( _direction * moveSpeed * Time.unscaledDeltaTime, Space.World );
    }

    /// <summary>
    /// Adaptively frames the sorting array within the unobstructed viewport corridor
    /// between the left controls and right code panel, taking camera field of view,
    /// aspect ratio, and UI margins into account for a level, unobstructed view.
    /// </summary>
    public void FrameArray( int count, float maxVal = 20f )
    {
        if ( mainCamera == null ) mainCamera = gameObject.transform;

        var cam = GetComponent<Camera>();
        if ( cam == null && mainCamera != null ) cam = mainCamera.GetComponent<Camera>();

        var fov = cam != null ? cam.fieldOfView : 60f;
        var aspect = cam != null ? cam.aspect : ( 16f / 9f );
        if ( aspect <= 0.1f ) aspect = 16f / 9f;

        // Pillars span x from 0 to (count - 1) * HorizontalGap
        var arrayWidth = ( count - 1 ) * Performance.Config.HorizontalGap + 1.0f;
        var xCenter = ( ( count - 1 ) * Performance.Config.HorizontalGap ) / 2f;

        // Pillar height is maxVal * CubeScale
        var maxPillarHeight = maxVal * Performance.Config.CubeScale;
        var yCenter = Mathf.Max( 2f, maxPillarHeight * 0.45f );

        // Required distances for horizontal and vertical FOV with aperture-aware margins
        var fovRadV = fov * 0.5f * Mathf.Deg2Rad;
        var fovRadH = Mathf.Atan( Mathf.Tan( fovRadV ) * aspect );

        // Span ratio: Array occupies the clear corridor between x_ndc = -0.80 and +0.38 (span = 1.18)
        var clearSpanRatio = 1.18f;
        var distH = arrayWidth / ( clearSpanRatio * Mathf.Tan( fovRadH ) );
        var effectiveHeight = ( maxPillarHeight + 4f ) * 1.35f;
        var distV = ( effectiveHeight * 0.5f ) / Mathf.Tan( fovRadV );
        var distance = Mathf.Max( distH, distV, 15f );

        // Screen-space projection verification:
        // x_ndc = (X_w - X_cam) / (distance * tan(fovRadH)).
        // Shifting array leftward in the viewport (away from the right-hand code panel)
        // requires x_ndc < 0, which dictates X_cam > x_center (positive horizontal offset).
        var horizontalNdcTarget = -0.21f; // Center of clear zone: midpoint of -0.80 and +0.38
        var xOffset = -horizontalNdcTarget * distance * Mathf.Tan( fovRadH );
        var camX = xCenter + xOffset;

        // Viewing pitch angle for balanced perspective
        var pitchAngle = 14f;
        var pitchRad = pitchAngle * Mathf.Deg2Rad;

        // Set rotation with zero yaw and zero roll for a level horizon
        mainCamera.rotation = Quaternion.Euler( pitchAngle, 0f, 0f );

        // Position camera back and elevated along the pitch angle relative to array center
        var camY = yCenter + distance * Mathf.Sin( pitchRad );
        var camZ = -distance * Mathf.Cos( pitchRad );

        mainCamera.position = new Vector3( camX, camY, camZ );
    }

    private void GetDirection()
    {
        #region Sprint Movement

        if ( Input.GetKeyDown( KeyCode.LeftShift ) ) moveSpeed *= shiftRate;
        if ( Input.GetKeyUp( KeyCode.LeftShift ) ) moveSpeed /= shiftRate;

        #endregion

        #region Keyboard Translation

        // Reset direction components
        _speedForward = Vector3.zero;
        _speedBack = Vector3.zero;
        _speedLeft = Vector3.zero;
        _speedRight = Vector3.zero;
        _speedUp = Vector3.zero;
        _speedDown = Vector3.zero;
        // Sample movement inputs
        if ( Input.GetKey( KeyCode.W ) ) _speedForward = mainCamera.forward;
        if ( Input.GetKey( KeyCode.S ) ) _speedBack = -mainCamera.forward;
        if ( Input.GetKey( KeyCode.A ) ) _speedLeft = -mainCamera.right;
        if ( Input.GetKey( KeyCode.D ) ) _speedRight = mainCamera.right;
        if ( Input.GetKey( KeyCode.E ) ) _speedUp = Vector3.up;
        if ( Input.GetKey( KeyCode.Q ) ) _speedDown = Vector3.down;
        _direction = _speedForward + _speedBack + _speedLeft + _speedRight + _speedUp + _speedDown;

        #endregion

        #region Mouse Look Rotation

        if ( Input.GetMouseButton( 1 ) )
        {
            // Rotate camera orientation
            mainCamera.RotateAround( mainCamera.position, Vector3.up, Input.GetAxis( "Mouse X" ) * rotateSpeed * Time.unscaledDeltaTime );
            mainCamera.RotateAround( mainCamera.position, mainCamera.right, -Input.GetAxis( "Mouse Y" ) * rotateSpeed * Time.unscaledDeltaTime );
            // Rotate movement velocity vector
            _direction = V3RotateAround( _direction, Vector3.up, Input.GetAxis( "Mouse X" ) * rotateSpeed * Time.unscaledDeltaTime );
            _direction = V3RotateAround( _direction, mainCamera.right, -Input.GetAxis( "Mouse Y" ) * rotateSpeed * Time.unscaledDeltaTime );
        }

        #endregion
    }

    /// <summary>
    /// Computes the vector obtained by rotating a Vector3 around a center axis by a given angle.
    /// </summary>
    /// <param name="source">Source Vector3 prior to rotation</param>
    /// <param name="axis">Axis of rotation</param>
    /// <param name="angle">Rotation angle in degrees</param>
    /// <returns>Resulting rotated Vector3</returns>
    public Vector3 V3RotateAround( Vector3 source, Vector3 axis, float angle )
    {
        var q = Quaternion.AngleAxis( angle, axis ); // Rotation quaternion
        return q * source; // Return transformed vector
    }
}