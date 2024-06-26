using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.OpenXR;
using TMPro;

public class MarkerLocalizer: MonoBehaviour
{
    public UIController uIController;
    public DrawingController drawController;
    public MeshGeneratorFromJson meshGenerator;
    public GameObject XR_Rig;
    public bool TrackingOn = false;
    private Quaternion offsetRotation = Quaternion.Euler(90, 0, 0);
    private GameObject Trackables;
    private GameObject Geometry;
    private Coroutine trackingCoroutine;

    void Start()
    {
        Trackables = XR_Rig.transform.Find("Trackables").gameObject;
        Trackables.SetActive(false);
        uIController.TrackingText.text = "Tracking: OFF";

        Geometry = meshGenerator.elementsParent;
    }
    
    public void EnableLocalization()
    {
        TrackingOn = true;   
        Trackables.SetActive(true);
        uIController.TrackingText.text = "Tracking: ON";
        
        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
        }
        trackingCoroutine = StartCoroutine(TrackingTimer());
    }

    private IEnumerator TrackingTimer()
    {
        yield return new WaitForSeconds(5);
        if (TrackingOn)
        {
            DisableLocalization();
        }
        drawController.CreateDrawingParent();
    }

    public void DisableLocalization()
    {
        TrackingOn = false;
        Trackables.SetActive(false);
        uIController.TrackingText.text = "Tracking: OFF";

        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
            trackingCoroutine = null;
        }
    }
    

    //Update is called once per frame
    void Update()
    {
       if  (TrackingOn)
       {
            if (XR_Rig != null)
            {
                if (Trackables != null)
                {
                    // Iterate through all children of Trackables
                    foreach (Transform child in Trackables.transform)
                    {
                        // Check if the child has a component named ARMarker with a specific value
                        string arMarkerText = child.GetComponent<ARMarker>().GetDecodedString();
                        if (arMarkerText == "workspace origin")
                        {
                            if (Geometry==null)
                            {
                                Geometry = meshGenerator.elementsParent;
                            }
                            // If found, set the position and rotation of Geometry and activate it
                            Geometry.transform.position = child.position;
                            Geometry.transform.rotation = child.rotation * offsetRotation;
                            Geometry.SetActive(true);

                            uIController.TableMenu.transform.position = child.position;
                            uIController.TableMenu.transform.rotation = child.rotation * offsetRotation;
                            uIController.TableMenu.SetActive(true);

                            meshGenerator.locksParent.SetActive(true);
                            return; // Exit the loop once the desired marker is found
                        }
                    }                    
                }
            }
       }
    }
}
