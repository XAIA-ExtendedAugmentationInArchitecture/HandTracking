using System;
using System.IO;
using System.Text;
using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.OpenXR.Input;
using TMPro;
using DrawingsData;
using MeshElementData;
using Newtonsoft.Json;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.SpatialManipulation;
using System.Linq;

public class MappingOnCurve : MonoBehaviour 
    
{
    private float ctrlPtSize = 0.04f;

    private float scale = 1.0f;
    private Material mat;
    private float overlapDist = 0.30f; // if you adjust this you need to adjust the same variable in the script UIController OVERLAP_DISTANCE
    private bool done = false;

    [HideInInspector] public List<GameObject> stPts = new List<GameObject>(); //the starting points for all the timber elements that are snapped into this curve
    [HideInInspector] public List<GameObject> endPts = new List<GameObject>(); //the end points for all the timber elements that are snapped into this curve

    [HideInInspector] public GameObject stPoint;
    [HideInInspector] public float parameter = 0.0f;
    
    private List<float> lengths = new List<float>();
    private List<string> names = new List<string>();

    public void CreateEndPoint(Material material, Vector3 position, bool isStartPt = true, bool show = true)
    {
        if (mat == null)
        {
            mat = material;
        }
        
        GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pointObject.transform.parent = this.gameObject.transform;
        pointObject.transform.position = position + this.gameObject.transform.position;
        pointObject.transform.localScale = new Vector3(ctrlPtSize, ctrlPtSize , ctrlPtSize);
        
        pointObject.GetComponent<Renderer>().material = material;
        
        if (isStartPt)
        {
            pointObject.name = "StartPoint";
            pointObject.GetComponent<Renderer>().material.color = new Color(Color.green.r, Color.green.g, Color.green.b, 0.3f);
            stPts.Add(pointObject);
        }
        else
        {
            pointObject.name = "EndPoint";
            pointObject.GetComponent<Renderer>().material.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, 0.3f);
            endPts.Add(pointObject);
        }
           
        //pointObject.AddComponent<CurveControlPoint>();
        if ( !show)
        {
            pointObject.GetComponent<Renderer>().enabled = false;
        }   
    }

    public void RemoveTimberElement (string name)
    {
        int index = names.IndexOf(name);

        if (index != -1) // Check if the name exists in the list
        {
            Debug.Log($"Element with name {name} found at index {index}.");

            names.RemoveAt(index); 
            lengths.RemoveAt(index);

            // Destroy and remove all control points
            foreach (var endPoint in endPts)
            {
                Destroy(endPoint);
            }
            endPts.Clear();

            // Skip the first start point (index 0) and destroy/remove the rest
            for (int i = stPts.Count - 1; i > 0; i--)
            {
                Destroy(stPts[i]);
                stPts.RemoveAt(i);
            }
            
            if (done)
            {
                done = false;
            } 
        }
        else
        {
            Debug.LogWarning($"Element with name {name} not found in the names list.");
        }

        Debug.Log($"Bazinga9: \nNames: [{string.Join(", ", names)}]\nLengths: [{string.Join(", ", lengths)}]\nEnd Points: [{string.Join(", ", endPts.Select(p => p.name))}]\nStart Points: [{string.Join(", ", stPts.Select(p => p.name))}]");

    }

    public void AppendTimberElement (string name, float length, float Scale)
    {
        scale = Scale;
        if (done)
        {
            Debug.LogWarning("The curve is already closed. Cannot append more elements.");
            return;
        }

        names.Add(name);
        lengths.Add(length);
        
        AddEndPoints(lengths);
        Debug.Log($"Whoboo scale: {scale} and the new length is: {length} \nLengths: [{string.Join(", ", lengths)}]"); 
    }

    private void AddEndPoints( List<float> lengths)
    {
        // Calculate the cumulative length

        float cumulativeLength = 0f;

        foreach (float len in lengths)
        {
            cumulativeLength += (len*scale);
            Debug.Log ($"Bazinga7 {len} ");
        }

        if (lengths.Count>1)
        {
            cumulativeLength -= overlapDist*scale* (lengths.Count-1);
        }

        Debug.Log ($"Bazinga7 scale is {scale} and cumulativeLength is {cumulativeLength} ");
        Vector3 endPoint = GetPositionAlongCurve(cumulativeLength);
        if (endPoint != Vector3.zero)
        {
            Vector3 startPoint = GetPositionAlongCurve(cumulativeLength - overlapDist*scale);
            
            CreateEndPoint(mat, startPoint, true);
            CreateEndPoint(mat, endPoint, false);
        }
        else 
        {
            done = true;
            LineRenderer lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer != null && lineRenderer.loop)
            {
                endPoint = GetPositionAlongCurve(overlapDist*scale);
                CreateEndPoint(mat, endPoint, false);
            }
        }
    }

    private Vector3 GetPositionAlongCurve(float targetDistance)
    {
        float accumulatedDistance = 0f;

        // Get the LineRenderer component
        LineRenderer lineRenderer = GetComponent<LineRenderer>();

        // Get the number of positions in the LineRenderer
        int numPoints = lineRenderer.positionCount;

        // Iterate through the positions
        for (int i = 0; i < numPoints - 1; i++)
        {
            // Get the positions of the current and next points
            Vector3 pointA = lineRenderer.GetPosition(i);
            Vector3 pointB = lineRenderer.GetPosition(i + 1);

            // Calculate the segment length
            float segmentLength = Vector3.Distance(pointA, pointB);

            // Check if the target distance falls within this segment
            if (accumulatedDistance + segmentLength >= targetDistance)
            {
                // Calculate the fraction along the segment
                float remainingDistance = targetDistance - accumulatedDistance;
                float t = remainingDistance / segmentLength;

                // Interpolate between the points
                Debug.Log($"Whoboo2 accumulatedDistance: {accumulatedDistance} and segmentLength is: {segmentLength} remainingDistance: {remainingDistance}");
                return Vector3.Lerp(pointA, pointB, t);
            }

            // Accumulate the distance
            accumulatedDistance += segmentLength;
        }

        // If the distance is greater than the total curve length, return zero vector
        return Vector3.zero;
    }

    public void UpdateEndPoints()
    {
        // Create a copy of the lengths list to iterate over
        List<float> lengthsCopy = new List<float>(lengths);

        for (int i = 0; i < lengthsCopy.Count; i++)
        {
            // Take sublist from index 0 to i+1
            List<float> subList = lengthsCopy.Take(i + 1).ToList();
            AddEndPoints(subList);
        }
    }

    public void SnapElementOnCurve(GameObject element, Vector3 snapPoint)
    {
        if (stPts.Count == 0)
        {
            Debug.LogWarning("No start points available to snap the element!");
            return;
        }

        // Get the LineRenderer component
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer component not found on the GameObject!");
            return;
        }

        // Calculate the tangent (direction) of the curve at the last point
        Vector3 tangent = GetTangentAtPoint(snapPoint);
        if (tangent == Vector3.zero)
        {
            Debug.LogError("Failed to calculate tangent at the last start point!");
            return;
        }

        // Get the element's TimberElement script
        TimberElement timberElement = element.GetComponent<TimberElement>();
        if (timberElement == null)
        {
            Debug.LogError("The provided element does not have a TimberElement script attached!");
            return;
        }

        // Calculate the element's local start and end points
        var (elementStart, elementEnd) = timberElement.CalculateEndPoints();
        Vector3 elementDirection = (elementEnd - elementStart).normalized;

        // Position the element's start point to match the last start point
        Vector3 offset = elementStart - element.transform.position;
        element.transform.position = snapPoint - offset;

        // Calculate rotation from element's direction to curve's tangent
        Quaternion fromToRotation = Quaternion.FromToRotation(elementDirection, tangent);
        element.transform.rotation = fromToRotation * element.transform.rotation;

        Debug.Log($"Snapped element '{element.name}' to curve at position {snapPoint} with tangent {tangent} and with length {timberElement.length}.");
    }

    public void SnapElements ()
    {
        MeshGeneratorFromJson meshGenerator = GameObject.Find("MeshGenerator").GetComponent<MeshGeneratorFromJson>();
        GameObject inventoryParent = meshGenerator.inventoryParent;
        
        int i =0;
        foreach(string name in names)
        {
            GameObject element = inventoryParent.FindObject(name);
            if (element != null)
            {
                Vector3 snapPoint = stPts[i].transform.position;
                SnapElementOnCurve(element, snapPoint);
            
            }
            i++;
        }
    }
    
    // Helper method to calculate the tangent at a given point on the LineRenderer
    private Vector3 GetTangentAtPoint(Vector3 position)
    {
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer component not found on the GameObject!");
            return Vector3.zero;
        }

        int numPoints = lineRenderer.positionCount;
        if (numPoints < 2)
        {
            Debug.LogError("LineRenderer must have at least two points to calculate tangents!");
            return Vector3.zero;
        }

        for (int i = 0; i < numPoints - 1; i++)
        {
            Vector3 pointA = lineRenderer.GetPosition(i);
            Vector3 pointB = lineRenderer.GetPosition(i + 1);

            // Check if the position falls within this segment
            if (Vector3.Distance(position, pointA) <= ctrlPtSize || Vector3.Distance(position, pointB) <= ctrlPtSize)
            {
                return (pointB - pointA).normalized; // Return the tangent as the normalized direction
            }
        }

        Debug.LogWarning("Position is not close to any segment of the LineRenderer!");
        return Vector3.zero;
    }

    public List<string[]> GetPairs()
    {
        var pairs = new List<string[]>();

        for (int i = 0; i < names.Count; i++)
        {
            // If not at the last element, pair current with next.
            if (i < names.Count - 1)
            {
                pairs.Add(new string[] { names[i], names[i + 1] });
            }
            // If at the last element and done = true, pair it with the first.
            else if (done)
            {
                pairs.Add(new string[] { names[i], names[0] });
            }
        }

        return pairs;
    }

    public void CreateStPoint(Material material, Vector3 position)
    {
        if (mat == null)
        {
            mat = material;
        }
        
        stPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        stPoint.transform.parent = this.gameObject.transform;
        stPoint.transform.position = position + this.gameObject.transform.position;
        stPoint.transform.localScale = new Vector3(ctrlPtSize*2, ctrlPtSize*2 , ctrlPtSize*2);
        stPoint.GetComponent<Renderer>().material = material;
        stPoint.name = "StartPoint0";
        stPoint.GetComponent<Renderer>().material.color = new Color(Color.magenta.r, Color.magenta.g, Color.magenta.b, 0.3f);
    

        StatefulInteractable interactable = stPoint.AddComponent<StatefulInteractable>();
        interactable.ToggleMode = StatefulInteractable.ToggleType.Button;

        OrderController orderCon = GameObject.Find("DrawingController").GetComponent<OrderController>();
        interactable.OnClicked.AddListener(() => 
        {
            stPoint.GetComponent<Renderer>().material.color = new Color(Color.blue.r, Color.blue.g, Color.blue.b, 0.3f);
            
            if (orderCon.selectedStPoint != null)
            {
                orderCon.selectedStPoint.GetComponent<Renderer>().material.color = new Color(Color.magenta.r, Color.magenta.g, Color.magenta.b, 0.3f);
            }
            orderCon.selectedStPoint = stPoint;
        });
            
    }

    public void UpdateStPosition(float value)
    {
        // Get the LineRenderer component
        LineRenderer lineRenderer = GetComponent<LineRenderer>();

        // Get the number of positions in the LineRenderer
        int numPoints = lineRenderer.positionCount;

        // Calculate the target position based on the slider value
        int targetIndex = Mathf.RoundToInt(value * (numPoints - 1));

        Debug.Log($"Whobooboo: {value} {numPoints} {targetIndex}");
        Vector3 targetPosition = lineRenderer.GetPosition(targetIndex);

        // Update the sphere's position to the target position
        stPoint.transform.position = targetPosition + this.gameObject.transform.position;
    }

     public void ShiftStPosition(float value)
    {
        // Get the LineRenderer component
        LineRenderer lineRenderer = GetComponent<LineRenderer>();

        // Get the number of positions in the LineRenderer
        int numPoints = lineRenderer.positionCount;

        // Calculate the target position based on the slider value
        int targetIndex = Mathf.RoundToInt(value * (numPoints - 1));
        Vector3 targetPosition = lineRenderer.GetPosition(targetIndex);

        Vector3[] positions = new Vector3[numPoints];

        lineRenderer.GetPositions(positions);

        // Shift the positions array to make the targetIndex position the first
        List<Vector3> shiftedPositions = new List<Vector3>();

        // Add positions starting from the targetIndex to the end
        for (int i = targetIndex; i < numPoints; i++)
        {
            shiftedPositions.Add(positions[i]);
        }

        // Add positions from the start to the targetIndex
        for (int i = 0; i < targetIndex; i++)
        {
            shiftedPositions.Add(positions[i]);
        }

        // Update the LineRenderer with the shifted positions
        lineRenderer.positionCount = shiftedPositions.Count;
        lineRenderer.SetPositions(shiftedPositions.ToArray());
    }
}

