using UnityEngine;

public class ConstrainedMovement : MonoBehaviour
{
    public Transform otherSphere;  // Reference to the other sphere
    private float d0;              // Initial distance between spheres
    private float minDistance;     // Minimum distance
    private float maxDistance;     // Maximum distance

    void Start()
    {
        // Calculate initial distance and constraints
        d0 = Vector3.Distance(transform.position, otherSphere.position);
        maxDistance = d0;
        minDistance = (2 * d0) / Mathf.PI;
    }

    void Update()
    {
        // Constrain movement
        Vector3 direction = (transform.position - otherSphere.position).normalized;
        float distance = Vector3.Distance(transform.position, otherSphere.position);

        // Clamp distance within min and max
        if (distance > maxDistance)
        {
            transform.position = otherSphere.position + direction * maxDistance;
        }
        else if (distance < minDistance)
        {
            transform.position = otherSphere.position + direction * minDistance;
        }

        // Ensure movement is only along the line between the spheres
        Vector3 relativePosition = transform.position - otherSphere.position;
        transform.position = otherSphere.position + Vector3.Project(relativePosition, direction);
    }
}
