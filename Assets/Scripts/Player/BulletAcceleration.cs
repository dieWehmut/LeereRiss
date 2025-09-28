using UnityEngine;

// Adds a continuous acceleration to the bullet's rigidbody in local forward direction (or specified)
public class BulletAcceleration : MonoBehaviour
{
    public Vector3 acceleration = Vector3.zero; // world-space acceleration applied each second
    public bool useLocalForward = true; // if true, acceleration is applied along transform.forward

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        if (rb == null) return;
        Vector3 accel = acceleration;
        if (useLocalForward)
            accel = transform.forward * acceleration.z; // if designer set acceleration.z to magnitude

        // Apply as acceleration (force = mass * accel)
        rb.AddForce(accel * rb.mass, ForceMode.Force);
    }
}
