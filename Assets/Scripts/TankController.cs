using UnityEngine;

public class TankController : MonoBehaviour
{
    // Debug
    [SerializeField] private bool DoGizmosDebugDrawing = false;

    // Movement
    [SerializeField] private float MoveSpeed = 2f;
    [SerializeField] private float WallDetectionDistance = 0.15f;

    // Rotation
    [SerializeField] private float MaxAngle = 45.0f;
    [SerializeField] private float OverhangLeanAngle = 20f;
    [SerializeField] private float MinDistanceForOverhang = 0.5f;

    // Ground snapping vars
    [SerializeField] private float GroundOffset = 1.0f;
    [SerializeField] private float GroundSnapDistance = 0.1f;

    // Tank measurements
    [SerializeField] private float TankWidth = 1.0f;
    [SerializeField] private float TankHeight = 3f;

    // Position tracking
    private float HorizontalInput = 0f;
    private float TargetAngle = 0f;
    private float SlopeAngle = 0f;
    private float HillCoefficient = 0f;

    // Raycasts
    private RaycastHit2D CenterRaycastHit;
    private RaycastHit2D LeftRaycastHit;
    private RaycastHit2D RightRaycastHit;

    // Wall blocking checks
    private bool LeftWallBlocking = false;
    private bool RightWallBlocking = false;

    void Update()
    {
        //HorizontalInput = -1f;
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
        Gizmos.DrawSphere(CenterRaycastHit.point, 0.01f);
        Gizmos.DrawSphere(LeftRaycastHit.point, 0.01f);
        Gizmos.DrawSphere(RightRaycastHit.point, 0.01f);

        Gizmos.DrawSphere(Physics2D.Raycast(transform.position, Vector2.left).point, 0.01f);

        var TankTop = transform.position;
        TankTop.y += TankHeight;
        Gizmos.DrawSphere(TankTop, 0.02f);
    }

    private void HandleGroundSnap()
    {
        // Perform the raycasts
        CenterRaycastHit = Physics2D.Raycast(new Vector2(transform.position.x, transform.position.y + TankHeight), Vector2.down);

        LeftRaycastHit = Physics2D.Raycast(new Vector2(transform.position.x - TankWidth, transform.position.y + TankHeight), Vector2.down);
        RightRaycastHit = Physics2D.Raycast(new Vector2(transform.position.x + TankWidth, transform.position.y + TankHeight), Vector2.down);

        // Determine if there are any shear walls blocking movement
        var leftWallHit2D = Physics2D.Raycast(transform.position, Vector2.left);
        var rightWallHit2D = Physics2D.Raycast(transform.position, Vector2.right);
        if(leftWallHit2D.collider != null)
        {
            LeftWallBlocking = Mathf.Abs(leftWallHit2D.distance) < WallDetectionDistance;
        }
        else LeftWallBlocking = false;
        if (rightWallHit2D.collider != null)
        {
            RightWallBlocking = Mathf.Abs(rightWallHit2D.distance) < WallDetectionDistance;
        }
        else RightWallBlocking = false;

        // Guard against failed raycasts
        if (CenterRaycastHit.collider == null || LeftRaycastHit.collider == null || RightRaycastHit.collider == null)
        {
            transform.position = new Vector3(0f, 0f, 0f);
            transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            return;
        }

        // Figure out if there is any overhangs, and from that determine angle & 
        var overhang = GetOverhangState();
        switch (overhang.State)
        {
            case OverhangState.RightOverhang:
            case OverhangState.LeftOverhang:
            case OverhangState.NoOverhang:
            default:
                {
                    SlopeAngle = overhang.CorrectAngle;
                    TargetAngle = overhang.CorrectAngle;

                    // Limit the maximum target slope between MaxAngle and negative Max angle to
                    // prevent the tank wildly turning at the edge of a cliff
                    if (TargetAngle > MaxAngle) TargetAngle = MaxAngle;
                    else if (TargetAngle < -MaxAngle) TargetAngle = -MaxAngle;

                    break;
                }
            case OverhangState.TotalOverhang:
                {
                    // The tank is falling, so don't rotate at all
                    break;
                }
        }

        // Calculate the horizontal movement angle coefficient (Steep hill = slow tank)
        float absSlopeAngle = Mathf.Abs(SlopeAngle);
        if (absSlopeAngle >= MaxAngle)
        {
            HillCoefficient = 0f;
        }
        else
        {
            HillCoefficient = 1 - (absSlopeAngle / MaxAngle);
        }

        // Find the length of distance between player's position and ground
        var difference = (CenterRaycastHit.distance - TankHeight) - GroundOffset;

        // If the distance is close, or if the player is below ground, just teleport snap the player tank
        if (Mathf.Abs(difference) < GroundSnapDistance || difference < 0)
        {
            TranslatePlayer(-transform.position.y + (CenterRaycastHit.point.y + GroundOffset));
        }

        // If the player is in total overhang (falling), then tick down their height, and
        // stop them from moving side to side
        else if (overhang.State == OverhangState.TotalOverhang)
        {
            HorizontalInput = 0f;
            TranslatePlayer(-1 * MoveSpeed * Time.deltaTime);
        }

        // The player must be in an overhange with only one point of contact,
        // so keep the current height and let TranslatePlayer() handle the rest
        else
        {
            TranslatePlayer(0f);
        }
    }

    private void TranslatePlayer(float verticalPosition)
    {
        float horizontalMovement = HorizontalInput * MoveSpeed * Time.deltaTime;

        if(LeftWallBlocking && horizontalMovement < 0f) horizontalMovement = 0f;
        if (RightWallBlocking && horizontalMovement > 0f) horizontalMovement = 0f;

        // Calculate position
        var tempPosition = transform.position;
        tempPosition.y += verticalPosition;

        if (horizontalMovement > 0)
        {
            if (TargetAngle > 0)
            {
                // Tank is moving left, and up a hill - Apply hill coefficient
                tempPosition.x += horizontalMovement * HillCoefficient;
            }
            else
            {
                // Tank is moving left, and down a hill - Do not apply hill coefficient
                tempPosition.x += horizontalMovement;
            }
        }
        else
        {
            if (TargetAngle > 0)
            {
                // Tank is moving right, and down a hill - Do not apply hill coefficient
                tempPosition.x += horizontalMovement;
            }
            else
            {
                // Tank is moving right, and up a hill - Apply hill coefficient
                tempPosition.x += horizontalMovement * HillCoefficient;
            }
        }

        // Set it
        transform.position = tempPosition;
        transform.rotation = Quaternion.Euler(0f, 0f, TargetAngle);
    }

    private bool IsGrounded() => 
        (LeftRaycastHit.distance - TankHeight) < GroundOffset + 0.01f ||
        (RightRaycastHit.distance - TankHeight) < GroundOffset + 0.01f ||
        (CenterRaycastHit.distance - TankHeight) < GroundOffset + 0.01f;

    private Overhang GetOverhangState()
    {
        if (!IsGrounded()) return new Overhang(OverhangState.TotalOverhang, 0f);

        //Get angle based on the left & right raycast hits
        var LeftToCenterAngle = Mathf.Atan2(CenterRaycastHit.point.y - LeftRaycastHit.point.y,
                                            CenterRaycastHit.point.x - LeftRaycastHit.point.x)

        // The above Atan2 outputs radians, so convert the slope angle to degrees:
        * (180 / 3.1485f);


        //Get angle based on the left & right raycast hits
        var RightToCenterAngle = Mathf.Atan2(RightRaycastHit.point.y - CenterRaycastHit.point.y,
                                             RightRaycastHit.point.x - CenterRaycastHit.point.x) * (180 / 3.1485f);

        //Get angle based on the left & right raycast hits
        var LeftToRightAngle = Mathf.Atan2(RightRaycastHit.point.y - LeftRaycastHit.point.y,
                                           RightRaycastHit.point.x - LeftRaycastHit.point.x) * (180 / 3.1485f);


        // If none of the 3 raycasts have any real distance, then no overhang is happening
        // So return such, and give the left to right angle
        if (Mathf.Max(LeftRaycastHit.distance - TankHeight,
                      RightRaycastHit.distance - TankHeight,
                      CenterRaycastHit.distance - TankHeight)
            < MinDistanceForOverhang)
        {
            return new Overhang(OverhangState.NoOverhang, LeftToRightAngle);
        }

        // Simple overhangs - Two points of contact with ground
        if (LeftToCenterAngle > MaxAngle) return new Overhang(OverhangState.LeftOverhang, RightToCenterAngle);
        if (RightToCenterAngle < -MaxAngle) return new Overhang(OverhangState.RightOverhang, LeftToCenterAngle);

        // Extreme overhangs - One point of contact with ground
        if (LeftToCenterAngle < -MaxAngle) return new Overhang(OverhangState.RightOverhang, -OverhangLeanAngle);
        if (RightToCenterAngle > MaxAngle) return new Overhang(OverhangState.LeftOverhang, OverhangLeanAngle);

        return new Overhang(OverhangState.NoOverhang, LeftToRightAngle);
    }
}

enum OverhangState
{
    NoOverhang = 0,
    LeftOverhang = 1,
    RightOverhang = 2,
    TotalOverhang = 3
}

internal class Overhang
{
    public Overhang(OverhangState state, float correctAngle)
    {
        State = state;
        CorrectAngle = correctAngle;
    }

    public override string ToString() => $"{State}, {CorrectAngle}";

    public OverhangState State { get; private set; }
    public float CorrectAngle { get; private set; }
}