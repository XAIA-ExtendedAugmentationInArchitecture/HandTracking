using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using System.Linq;

public class OrderController : MonoBehaviour
{
    private DrawingController drawController;
    private const float proximityThreshold = 0.25f;

    [HideInInspector] public GameObject selectedStPoint;

   public void CheckProximityToCurves(GameObject selectedObj)
    {
        drawController = GameObject.Find("DrawingController").GetComponent<DrawingController>();
        GameObject currentDrawingParent = drawController.currentDrawingParent;

        
        if (currentDrawingParent == null)
        {
            Debug.LogWarning("OrderController: No currentDrawingParent found.");
            return;
        }

        foreach (Transform curveChild in currentDrawingParent.transform)
        {
            if (curveChild.CompareTag("simplified"))
            {
                 var mappingOnCurve = curveChild.GetComponent<MappingOnCurve>();
                if (mappingOnCurve != null)
                {
                    // Get start points from MappingOnCurve
                    Vector3 snapPoint = mappingOnCurve.stPts.Last().transform.position;

                    // Get manipulated object's position
                    (Vector3 firstPoint, Vector3 lastPoint) = selectedObj.GetComponent<TimberElement>().CalculateEndPoints();

                    float scale = drawController.currentDrawingParent.transform.localScale.x;
                    float dist = scale * proximityThreshold;
                    if (Vector3.Distance(firstPoint, snapPoint) <= dist || Vector3.Distance(lastPoint, snapPoint) <= dist)
                    {
                        Debug.Log($"Selected object {selectedObj.name} is within {proximityThreshold}m under the scale of {scale} on {curveChild.name}.");
                        
                        mappingOnCurve.SnapElementOnCurve(selectedObj, snapPoint);
                        float length = selectedObj.GetComponent<TimberElement>().length;
                        mappingOnCurve.AppendTimberElement(selectedObj.name, length, scale);

                        mappingOnCurve.AppendSegments(selectedObj.GetComponent<TimberElement>().segWithConnectionsLengths, selectedObj.GetComponent<TimberElement>().typesWithConnections);
                        mappingOnCurve.SnapOnMiddlePoint(selectedObj);

                        GameObject lockObject = GameObject.Find("MeshGenerator").GetComponent<MeshGeneratorFromJson>().locksParent.FindObject("lock_" + selectedObj.name);
                        if (lockObject == null)
                        {
                            Debug.LogWarning("sssssssssssss + " + selectedObj.name);
                        }
                        lockObject.GetComponent<ElementStateController>().ToggleState();
                        return;
                    }
                    else
                    {
                        Debug.Log($"Selected object {selectedObj.name} is not within {proximityThreshold}m under the scale of {scale} on {curveChild.name}.");
                        
                        selectedObj.GetComponent<TimberElement>().flipped = false;
                        selectedObj.GetComponent<TimberElement>().rotated = 0;
                        selectedObj.GetComponent<TimberElement>().moveDist = 0.0f;

                        mappingOnCurve.RemoveTimberElement(selectedObj.name);
                        mappingOnCurve.UpdateEndPoints();
                    
                    }
                }

            }
        }
    }
} 

    



