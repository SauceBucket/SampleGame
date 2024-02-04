using UnityEngine;

public class TankController : MonoBehaviour
{
    // Debug
    [SerializeField] private bool DoGizmosDebugDrawing = false;

    // Movement
    [SerializeField] private float MoveSpeed = 2f;

    // Ground snapping vars
    [SerializeField] private float GroundOffset = 1.0f;
    [SerializeField] private float GroundSnapDistance = 0.1f;

    // Tank measurements
    [SerializeField] private float TankWidth = 1.0f;
    [SerializeField] private float TankHeight = 3f;

    // Position tracking
    private float HorizontalInput = 0f;
    private float TargetAngle = 0f;

    // Raycasts
    private RaycastHit2D CenterRaycastHit;
    private RaycastHit2D LeftRaycastHit;
    private RaycastHit2D RightRaycastHit;

    void Update()
    {
        HorizontalInput = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        HandleGroundSnap();
    }

    void OnDrawGizmos()
    {
        if (!DoGizmosDebugDrawing) return;

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(CenterRaycastHit.point, 0.1f);
        Gizmos.DrawSphere(LeftRaycastHit.point, 0.1f);
        Gizmos.DrawSphere(RightRaycastHit.point, 0.1f);
    }

    private void HandleGroundSnap()
    {
        // Perform the raycast
        CenterRaycastHit = Physics2D.Raycast(transform.position, Vector2.down);

        LeftRaycastHit = Physics2D.Raycast(new Vector2(transform.position.x - TankWidth, transform.position.y + TankHeight), Vector2.down);
        RightRaycastHit = Physics2D.Raycast(new Vector2(transform.position.x + TankWidth, transform.position.y + TankHeight), Vector2.down);

        // Guard against failed raycasts
        if (CenterRaycastHit.point == null || LeftRaycastHit.point == null || RightRaycastHit.point == null)
        {
            transform.position = new Vector3(0f, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            return;  
        }

        // Find the length of distance between player's position and ground
        var difference = CenterRaycastHit.distance - GroundOffset;

        // If the distance is close, just teleport snap
        if (Mathf.Abs(difference) < GroundSnapDistance)
        {
            TranslatePlayer(-transform.position.y + (CenterRaycastHit.point.y + GroundOffset));
        }

        // If player is below the ground (positive distance), make the player go up (over time)
        else if (difference < 0)
        {
            TranslatePlayer(1 * MoveSpeed * Time.deltaTime);
        }
        
        // The player must be above ground, make them fall (over time)
        else
        {
            TranslatePlayer(-1 * MoveSpeed * Time.deltaTime);
        }

        // Tank rotation - don't rotate the tank if the tank is too far above the ground
        if ((LeftRaycastHit.distance - TankHeight) < GroundOffset || (RightRaycastHit.distance - TankHeight) < GroundOffset)
        {
            //Get angle based on the left & right raycast hits
            TargetAngle = Mathf.Atan2(RightRaycastHit.point.y - LeftRaycastHit.point.y,
                                      RightRaycastHit.point.x - LeftRaycastHit.point.x)

            // The above Atan2 outputs radians, so convert to degrees:
            * (180 / 3.1485f);

            // Adjust the angle slowly
            TargetAngle = Mathf.MoveTowardsAngle(transform.rotation.eulerAngles.z, TargetAngle, MoveSpeed);
        }
    }

    private void TranslatePlayer(float verticalPosition)
    {
        // Calculate position
        var tempPosition = transform.position;
        tempPosition.y += verticalPosition;
        tempPosition.x += HorizontalInput * MoveSpeed * Time.deltaTime;

        // Set it
        transform.position = tempPosition;
        transform.rotation = Quaternion.Euler(0f, 0f, TargetAngle);
    }

}