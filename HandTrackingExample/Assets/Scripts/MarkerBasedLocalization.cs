using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Microsoft.MixedReality.OpenXR;

public class markerBasedLocalization: MonoBehaviour
{
    public GameObject Geometry;
    public GameObject XR_Rig;
    public bool TrackingOn = false;
    private Quaternion offsetRotation = Quaternion.Euler(90, 0, 0);

    void Start()
    {
        if (Geometry != null)
        {
            Geometry.SetActive(false);
        }
    }
    

    //Update is called once per frame
    void Update()
    {
       if  (TrackingOn)
       {
            if (XR_Rig != null)
            {
                GameObject Trackables = XR_Rig.transform.Find("Trackables").gameObject;
                if (Trackables != null)
                {
                    // Iterate through all children of Trackables
                    foreach (Transform child in Trackables.transform)
                    {
                        // Check if the child has a component named ARMarker with a specific value
                        string arMarkerText = child.GetComponent<ARMarker>().GetDecodedString();
                        if (arMarkerText == "workspace origin")
                        {
                            // If found, set the position and rotation of Geometry and activate it
                            Geometry.transform.position = child.position;
                            Geometry.transform.rotation = child.rotation * offsetRotation;
                            Geometry.SetActive(true);
                            return; // Exit the loop once the desired marker is found
                        }
                    }                    
                }
            }
       }
    }
}
