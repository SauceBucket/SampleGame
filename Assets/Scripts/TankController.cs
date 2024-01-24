using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float groundSnapDelay = 0.1f;
    [SerializeField] private float groundOffset = 1.0f;
    [SerializeField] private float snapDistance = 0.2f;

    [SerializeField] private bool isGrounded;
    
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

    void HandleGroundSnap()
    {
        // Perform the raycast
        var hit = Physics2D.Raycast(transform.position, Vector2.down);

        if (hit.collider != null && hit.collider.CompareTag("Ground"))
        {
            var difference = (transform.position.y - groundOffset) - hit.point.y;
            Debug.Log(difference);
            if(Mathf.Abs(difference) < snapDistance){
                Vector2 movement = new Vector2(horizontal, -difference) * moveSpeed * Time.deltaTime;
                rb.MovePosition(rb.position + movement);
            }
            else if(difference > 0){
                Vector2 movement = new Vector2(horizontal, -1f) * moveSpeed * Time.deltaTime;
                rb.MovePosition(rb.position + movement);
            }
            else{
                Vector2 movement = new Vector2(horizontal, 1f) * moveSpeed * Time.deltaTime;
                rb.MovePosition(rb.position + movement);
            }
        }
    }


}