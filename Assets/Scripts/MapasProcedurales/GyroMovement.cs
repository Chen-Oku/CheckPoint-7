using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class GyroMovement : MonoBehaviour
{
    [Tooltip("Movement speed multiplier applied to input (accelerometer/gyro/touch)")]
    public float speed = 5f;

    private Rigidbody rb;

    // touch drag state
    private Vector2 touchStart;
    private bool touchDragging = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotation; // evita rotación al chocar

        // Enable gyroscope if available (preferred over raw acceleration)
        if (SystemInfo.supportsGyroscope)
        {
            Input.gyro.enabled = true;
        }
    }

    void FixedUpdate()
    {
        Vector3 inputMove = Vector3.zero;

        // 1) Gyroscope gravity (tilt) if supported
        if (SystemInfo.supportsGyroscope)
        {
            // Input.gyro.gravity gives direction of gravity in device space
            Vector3 g = Input.gyro.gravity;
            // Map device axes to world axes (portrait assumed)
            inputMove = new Vector3(g.x, 0f, g.y);
        }
        else
        {
            // 2) Accelerometer fallback (works on most devices)
            Vector3 a = Input.acceleration;
            inputMove = new Vector3(a.x, 0f, a.y);
        }

        // 3) Touch drag fallback — if player touches and drags, use that input
        if (Input.touchCount > 0)
        {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began)
            {
                touchStart = t.position;
                touchDragging = true;
            }
            else if (t.phase == TouchPhase.Moved && touchDragging)
            {
                Vector2 delta = t.position - touchStart;
                // convert pixel delta to -1..1 range (simple normalization)
                float max = Mathf.Max(Screen.width, Screen.height);
                Vector2 norm = Vector2.ClampMagnitude(delta / max, 1f);
                inputMove = new Vector3(norm.x, 0f, norm.y);
            }
            else if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                touchDragging = false;
            }
        }

        // 4) Keyboard fallback for editor testing
        if (Application.isEditor)
        {
            float hx = Input.GetAxis("Horizontal");
            float vz = Input.GetAxis("Vertical");
            if (Mathf.Abs(hx) > 0.01f || Mathf.Abs(vz) > 0.01f)
            {
                inputMove = new Vector3(hx, 0f, vz);
            }
        }

        // Apply movement. Use ForceMode.Acceleration so mass doesn't overly affect responsiveness.
        rb.AddForce(inputMove * speed, ForceMode.Acceleration);
    }
}
