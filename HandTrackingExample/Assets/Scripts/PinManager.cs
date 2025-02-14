using UnityEngine;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using Unity.VisualScripting;

public class PinManager : MonoBehaviour
{
    //[HideInInspector] public Vector3 initialPinPosition = new Vector3(0.12f, 0.0f, 0.045f); // Initial position for the first pin
    
    public DrawingController drawingController; 
    public GameObject PinPrefab;

    [HideInInspector] public GameObject newPin;

    [HideInInspector] public GameObject pinParent;
    [HideInInspector] public int pinIndex=0;

    public void InstantiatePin(Vector3 position, Quaternion rotation, GameObject parent =null)
    {
        if (parent!=null)
        {
            pinParent = parent;
        }
        // Instantiate the current GameObject at the given position
        newPin = Instantiate(PinPrefab);
        newPin.transform.parent = parent.transform;
        newPin.transform.localPosition = position;
        newPin.transform.localRotation = rotation;
        newPin.name = "pin"+ pinIndex.ToString();
        pinIndex++;
        
        // Add a listener to the ObjectManipulator script's OnManipulationStarted event
        ObjectManipulator manipulator = newPin.GetComponent<ObjectManipulator>();
        if (manipulator != null)
        {
            manipulator.OnClicked.AddListener(() => OnPinManipulationStarted(newPin));
        }
    }

    public void OnPinManipulationStarted(GameObject pin)
    {
    
        // Get the position of the pin when manipulation starts
        Vector3 pos = Vector3.zero;
        Quaternion rot = Quaternion.identity;

        // Instantiate a new pin at the specific position
        InstantiatePin(pos, rot, pin.transform.parent.gameObject);

        pin.transform.parent = drawingController.currentDrawingParent.FindObject("pins").transform;

        // Remove the listener to prevent further instantiation
        ObjectManipulator manipulator = pin.GetComponent<ObjectManipulator>();
        if (manipulator != null)
        {
            manipulator.OnClicked.RemoveAllListeners();
        }

    }
}
