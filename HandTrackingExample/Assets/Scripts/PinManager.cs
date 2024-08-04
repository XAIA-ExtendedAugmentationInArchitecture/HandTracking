using UnityEngine;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;

public class PinManager : MonoBehaviour
{
    [HideInInspector] public Vector3 initialPinPosition = new Vector3(0.12f, 0.0f, 0.045f); // Initial position for the first pin
    
    public DrawingController drawingController; 
    public GameObject PinPrefab;

    [HideInInspector] public GameObject newPin;
    [HideInInspector] public int pinIndex=0;

    public void InstantiatePin(Vector3 position, Quaternion rotation)
    {
        // Instantiate the current GameObject at the given position
        newPin = Instantiate(PinPrefab, position, rotation);
        newPin.name = "pin"+ pinIndex.ToString();
        pinIndex++;
        
        // Add a listener to the ObjectManipulator script's OnManipulationStarted event
        ObjectManipulator manipulator = newPin.GetComponent<ObjectManipulator>();
        if (manipulator != null)
        {
            manipulator.OnClicked.AddListener(() => OnPinManipulationStarted(newPin));
        }
    }

    private void OnPinManipulationStarted(GameObject pin)
    {
    
        // Get the position of the pin when manipulation starts
        Vector3 pos = drawingController.meshGenerator.elementsParent.transform.position;
        Quaternion rot = drawingController.meshGenerator.elementsParent.transform.rotation;

        // Instantiate a new pin at the specific position
        InstantiatePin(pos + (rot * initialPinPosition), rot);

        pin.transform.parent = drawingController.currentDrawingParent.FindObject("pins").transform;

        // Remove the listener to prevent further instantiation
        ObjectManipulator manipulator = pin.GetComponent<ObjectManipulator>();
        if (manipulator != null)
        {
            manipulator.OnClicked.RemoveAllListeners();
        }

    }
}
