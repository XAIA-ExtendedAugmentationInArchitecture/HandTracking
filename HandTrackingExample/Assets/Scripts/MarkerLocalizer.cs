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
    [HideInInspector] public bool TrackingOn = false;
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
                            
                            Vector3 pos =  child.position;
                            Quaternion rot = child.rotation * offsetRotation;
                            // If found, set the position and rotation of Geometry and activate it
                            Geometry.transform.position = pos;
                            Geometry.transform.rotation = rot;
                            Geometry.SetActive(true);

                            uIController.TableMenu.transform.position = pos;
                            uIController.TableMenu.transform.rotation = rot;
                            uIController.TableMenu.SetActive(true);

                            // if (drawController.pinManager.newPin)
                            // {
                            //     drawController.pinManager.newPin.transform.position = pos + (rot * drawController.pinManager.initialPinPosition);
                            //     drawController.pinManager.newPin.transform.rotation = rot;
                            // }
                            

                            meshGenerator.locksParent.SetActive(true);

                            meshGenerator.inventoryParent.transform.position = pos;
                            meshGenerator.inventoryParent.transform.rotation = rot;


                            // Define movement amounts along local axes
                            float movementX = 0.4f;
                            float movementY = 0.1f;/* your desired movement along local y-axis */;
                            float movementZ = 0.0f/* your desired movement along local z-axis */;

                            // Calculate movement vector based on local axes
                            Vector3 movement = (movementX * meshGenerator.inventoryParent.transform.right) +
                                            (movementY * meshGenerator.inventoryParent.transform.up) +
                                            (movementZ * meshGenerator.inventoryParent.transform.forward);

                            // Apply movement to the object's position
                            meshGenerator.inventoryParent.transform.position += movement;

                            // Define movement amounts along local axes
                            movementX = 0.0f;
                            movementY = 0.25f;/* your desired movement along local y-axis */;
                            movementZ = 0.25f/* your desired movement along local z-axis */;

                            meshGenerator.detailsParent.transform.position = pos;
                            meshGenerator.detailsParent.transform.rotation = rot;

                            // Calculate movement vector based on local axes
                            movement = (movementX * meshGenerator.detailsParent.transform.right) +
                                            (movementY * meshGenerator.detailsParent.transform.up) +
                                            (movementZ * meshGenerator.detailsParent.transform.forward);
                            
                            meshGenerator.detailsParent.transform.position += movement;


                            return; // Exit the loop once the desired marker is found
                        }
                    }                    
                }
            }
       }
    }
}
