using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SlideMovement : MonoBehaviour
{
    public float slideSpeed = 5f;
    private Rigidbody rb;
    private bool isMoving = false;
    private Vector3 direction;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation;
    }

    void Update()
    {
        if (!isMoving)
        {
            Vector3 tilt = Input.acceleration;
            direction = new Vector3(tilt.x, 0, tilt.y);

            if (direction.magnitude > 0.2f) // evita movimiento accidental
            {
                direction.Normalize();
                isMoving = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (isMoving)
            rb.linearVelocity = direction * slideSpeed;
    }

    void OnCollisionEnter(Collision collision)
    {
        rb.linearVelocity = Vector3.zero;
        isMoving = false;
    }
}
