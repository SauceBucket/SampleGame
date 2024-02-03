using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float groundOffset = 1.0f;
    [SerializeField] private float snapDistance = 0.1f;
    
    private Rigidbody2D rb;
    private Vector3 groundSnapTarget;    
    private float horizontal = 0f;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");        
    }

    void FixedUpdate()
    {        
        HandleGroundSnap();
    }

    private void HandleGroundSnap()
    {
        // Perform the raycast
        var hit = Physics2D.Raycast(transform.position, Vector2.down);

        //Check for hit
        if (hit.collider != null)
        {

            //Find the length of distance between player's position and ground
            var difference = (transform.position.y - groundOffset) - hit.point.y;

            //If the distance is close, just teleport snap
            if(Mathf.Abs(difference) < snapDistance){
                TranslatePlayer(-transform.position.y + (hit.point.y + groundOffset));
            }

            //If player is below the ground (positive distance), make the player go up (over time)
            else if(difference < 0){
                TranslatePlayer(1 * moveSpeed * Time.deltaTime);
            }

            //The player must be above ground, make them fall (over time)
            else{
                TranslatePlayer(-1 * moveSpeed * Time.deltaTime);
            }
        }
    }

    private void TranslatePlayer(float verticalPosition){
        var tempPosition = transform.position;
        tempPosition.y += verticalPosition;
        tempPosition.x += horizontal * moveSpeed * Time.deltaTime;
        transform.position = tempPosition;
    }

}