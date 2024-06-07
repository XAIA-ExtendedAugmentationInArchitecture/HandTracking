using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    // Reference to the camera
    private Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        // Find the main camera in the scene
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        // Make the GameObject face the camera
        if (mainCamera != null)
        {
            // Calculate the direction from the GameObject to the camera
            Vector3 direction = mainCamera.transform.position - transform.position;

            // Update the GameObject's rotation to look at the camera
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}
