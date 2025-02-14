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
    private MoveAlongCurveConstraint constraint;
    
    private List<float> lengths = new List<float>();
    public List<List<int>> types = new List<List<int>>();

    public List<List<float>> segLengths = new List<List<float>>();

    private List<List<GameObject>> segments = new List<List<GameObject>>();

    public int lastIndex=0;
    public bool isFirst= true;
    public List<int> lastIndices = new List<int>();
    public List<bool> areInterpolated = new List<bool>();
    private bool isInterpolated = false;
    public List<Vector3> interPts = new List<Vector3>();
    private Vector3 interPt = Vector3.zero;
    public List<Vector3[]> lastConnections = new List<Vector3[]>();
    private Vector3[] lastConnection = new Vector3[0];
    private PlankBender plankBender = new PlankBender();
    [HideInInspector] public List<string> names = new List<string>();

    public void CreateEndPoint(Material material, Vector3 position, 
                            bool isStartPt = true, bool show = true)
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

            // Destroy all GameObjects in the last segment and remove the segment
            if (segments.Count > 0)
            {
                foreach (var obj in segments[index])
                {
                    Destroy(obj);
                }
                segments.RemoveAt(index)  ;
            }
            lastIndices.RemoveAt(index+1);
            areInterpolated.RemoveAt(index+1);
            interPts.RemoveAt(index+1);
            lastConnections.RemoveAt(index+1);
            segLengths.RemoveAt(index);
            types.RemoveAt(index);

            // Destroy and remove all end points
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
            if (segments.Count ==0 )
            {
                isFirst = true;
            }
          
            done = false;
            
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

    public void AppendSegments(List<float> newSegLengths, List<int> subtypes)
    {
        if (done)
        {
            Debug.LogWarning("The curve is already closed. Cannot append more elements.");
            return;
        }

        segLengths.Add(newSegLengths);
        types.Add(subtypes);

        AddMeshSegments( newSegLengths, subtypes);
    }

    private void AddMeshSegments( List<float> lengths, List<int> types )
    {
        segments.Add(new List<GameObject>());

        int stIndex =0;
        if ( !isFirst)
        {
            stIndex = 1;
            GenerateNextPiece(0, types, lastConnections.Last());
        }
        else
        {
            lastIndices.Add(0);
            areInterpolated.Add(false);
            interPts.Add(Vector3.zero);
            lastConnections.Add(new Vector3[0]);

        }
        lastIndex = lastIndices.Last();
        isInterpolated = areInterpolated.Last();
        interPt= interPts.Last();
        lastConnection = lastConnections.Last();

        for (int i = stIndex; i < lengths.Count; i++)
        {
            float len = lengths[i];

            (Vector3 point, int index, bool interpolated) = GetIndexAlongCurve(len*scale);
            
            // Get points between lastIndex and current index
            LineRenderer lineRenderer = GetComponent<LineRenderer>();
            int numPoints = index - lastIndex + 1;
            Vector3[] pointsBetween = new Vector3[numPoints];

            if (  isFirst)
            {
                pointsBetween[0] = lineRenderer.GetPosition(0);
                for (int j = 1; j < numPoints; j++)
                {
                    pointsBetween[j] = lineRenderer.GetPosition(lastIndex + j);
                }
                isFirst = false;
            }
            else if(isInterpolated)
            {
                pointsBetween[0] = interPt;
                for (int j = 1; j < numPoints; j++)
                {
                    pointsBetween[j] = lineRenderer.GetPosition(lastIndex + j);
                }
            }
            else
            {
                for (int j = 0; j < numPoints; j++)
                {
                    pointsBetween[j] = lineRenderer.GetPosition(lastIndex + j);
                }
            }

            interPt = point;
            isInterpolated = interpolated;
            
            if (isInterpolated )
            {
                pointsBetween[pointsBetween.Length - 1] = interPt;
            }

            lastIndex =index;

            GenerateNextPiece(i, types, pointsBetween);

            if (types[i] == 2)
            {
                lastConnection = pointsBetween;
            }
            else
            {
                lastConnection = new Vector3[0];
            }

            if(point == Vector3.zero)
            {
                done = true;
                return;
            } 
        }
        lastIndices.Add(lastIndex);
        areInterpolated.Add(isInterpolated);
        interPts.Add(interPt);
        lastConnections.Add(lastConnection);
    }

    private void GenerateNextPiece(int index, List<int> types, Vector3[] pointsBetween)
    {
        GameObject piece = new GameObject($"Piece{names.Last()}_{index}");
        piece.transform.parent = this.gameObject.transform;

        Color pcolor = new Color(Color.white.r, Color.white.g, Color.white.b, 0.5f);
        if (types[index]==1)
        {
            pcolor = new Color(Color.red.r, Color.red.g, Color.red.b, 0.5f);
        }
        else if(types[index]==2)
        {
            pcolor = new Color(Color.green.r, Color.green.g, Color.green.b, 0.5f);
        }

        MeshGeneratorFromJson meshGenerator = GameObject.Find("MeshGenerator").GetComponent<MeshGeneratorFromJson>();
        GameObject inventoryParent = meshGenerator.inventoryParent;
    
        GameObject element = inventoryParent.FindObject(names.Last());

        float width = element.GetComponent<TimberElement>().width;
        float height = element.GetComponent<TimberElement>().height;

        segments.Last().Add(plankBender.GeneratePlankMesh(
                Vector3.up, scale, width, height, pointsBetween, piece, pcolor));
        
        Debug.Log($"Points Between: [{string.Join(", ", pointsBetween.Select(p => p.ToString()))}]");
 
    }

    private void AddEndPoints( List<float> lengths)
    {
        // Calculate the cumulative length
        float cumulativeLength = 0f;

        foreach (float len in lengths)
        {
            cumulativeLength += (len*scale);
        }

        if (lengths.Count > 1)
        {
            cumulativeLength -= overlapDist*scale* (lengths.Count-1);
        }

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

    private (Vector3, int, bool) GetIndexAlongCurve(float targetDistance)
    {
        float accumulatedDistance = 0.0f;

        // Get the LineRenderer component
        LineRenderer lineRenderer = GetComponent<LineRenderer>();

        // Get the number of positions in the LineRenderer
        int numPoints = lineRenderer.positionCount;

        bool interFirst = false;
        if (isInterpolated)
        {
            interFirst = true;
            if (lastIndex > 0)
            {
                lastIndex--;
            }
        }

        // Iterate through the positions
        for (int i = lastIndex; i < numPoints - 1; i++)
        {
            // Get the positions of the current and next points
            Vector3 pointA = lineRenderer.GetPosition(i);
            if (interFirst)
            {
                pointA = interPt;
                interFirst = false;
            }
            Vector3 pointB = lineRenderer.GetPosition(i + 1);

            // Calculate the segment length
            float segmentLength = Vector3.Distance(pointA, pointB);

            // Check if the target distance falls within this segment
            if (accumulatedDistance + segmentLength >= targetDistance)
            {
                // Calculate the fraction along the segment
                float remainingDistance = targetDistance - accumulatedDistance;
                float t = remainingDistance / segmentLength;

                // If point exactly matches a vertex, return that vertex index
                if (t == 0) return (pointA, i, false);
                if (t == 1) return (pointB, i + 1, false);
                // Interpolate between the points
                Debug.Log($"AAA QQQQ4 AccumulatedDistance: {accumulatedDistance/scale} and PtAtoPtBLength is: {segmentLength/scale} remainingDistance: {remainingDistance/scale}");
                return (Vector3.Lerp(pointA, pointB, t), i+1, true);
            }

            // Accumulate the distance
            accumulatedDistance += segmentLength;
        }

        // If the distance is greater than the total curve length, return last point
        return (Vector3.zero, numPoints - 1, false);
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
                Debug.Log($"AAA BBB AccumulatedDistance: {accumulatedDistance/scale} and segmentLength is: {segmentLength/scale} remainingDistance: {remainingDistance/ scale}");
                return Vector3.Lerp(pointA, pointB, t);
            }

            // Accumulate the distance
            accumulatedDistance += segmentLength;
        }

        // If the distance is greater than the total curve length, return zero vector
        return Vector3.zero;
    }

    public void UpdateStartPoint(bool show = true)
    {
        Destroy(stPts[0]);

        Vector3 position = this.GetComponent<LineRenderer>().GetPosition(0);

        GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pointObject.transform.parent = this.gameObject.transform;
        pointObject.transform.position = position + this.gameObject.transform.position;
        pointObject.transform.localScale = new Vector3(ctrlPtSize, ctrlPtSize , ctrlPtSize);
        
        pointObject.GetComponent<Renderer>().material = mat;
        
        pointObject.name = "StartPoint";
        pointObject.GetComponent<Renderer>().material.color = new Color(Color.green.r, Color.green.g, Color.green.b, 0.3f);


        stPts[0]= pointObject;

        if ( !show)
        {
            pointObject.GetComponent<Renderer>().enabled = false;
        } 

        

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

    public void UpdateMapping()
    {
        foreach (var pt in endPts)
        {
            Destroy(pt);
        }

        foreach (var pt in stPts)
        {
            Destroy(pt);
        }

        endPts.Clear();
        stPts.Clear();

        lastIndex=0;
        isFirst= true;
        isInterpolated = false;
        interPt = Vector3.zero;
        lastConnection = new Vector3[0];

        // clear the lastIndices, areInterpolated, interPts and lastConnections lists
        lastIndices.Clear();
        areInterpolated.Clear();
        interPts.Clear();
        lastConnections.Clear();
        
        // Clear all existing segments
        foreach (var segmentList in segments)
        {
            foreach (var segment in segmentList)
            {
            Destroy(segment);
            }
        }
        segments.Clear();

        CreateEndPoint(mat, GetComponent<LineRenderer>().GetPosition(0));
        UpdateEndPoints();
        SnapElements();

        UpdateSegments();


    }

    public void UpdateSegments()
    {
        // Create a copy of the lengths list to iterate over
        List<List<float>> segLengthsCopy = new List<List<float>>(segLengths);

        for (int i = 0; i < segLengthsCopy.Count; i++)
        {
            // Take sublist from index 0 to i+1
            List<float> subList =segLengthsCopy[i];
            AddMeshSegments(subList, types[i]);
            GameObject inventory = GameObject.Find("MeshGenerator").GetComponent<MeshGeneratorFromJson>().inventoryParent;
            GameObject selectedGameObject = inventory.FindObject(names[i]);
            Debug.Log($"Selecteeeed GameObject: {names[i]}");
            if (selectedGameObject != null)
            {
                Debug.Log($"Selecteeeed GameObject222");
                SnapOnMiddlePoint(selectedGameObject, true, i);
            }
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

     public void SnapOnMiddlePoint(GameObject element, bool update=false, int index = 0)
    {
        // Calculate the cumulative length
        float cumulativeLength = 0f;

    
        if (update)
        {
            foreach (float len in lengths.Take(index))
            {
                cumulativeLength += (len*scale);
            }
            cumulativeLength += (lengths[index]*scale) /2;

            if (index >= 0)
            {
                cumulativeLength -= overlapDist*scale* (index);
            }
        }
        else
        {
            foreach (float len in lengths.Take(lengths.Count - 1))
            {
                cumulativeLength += (len*scale);
            }
            cumulativeLength += (lengths.Last()*scale) /2;

            if (lengths.Count > 1)
            {
                cumulativeLength -= overlapDist*scale* (lengths.Count-1);
            }
        }

    
        Vector3 midPoint = GetPositionAlongCurve(cumulativeLength);
        
        GameObject midSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        midSphere.transform.parent = this.gameObject.transform;
        midSphere.transform.position = midPoint + this.gameObject.transform.position;
        midSphere.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        midSphere.GetComponent<Renderer>().material = mat;
        midSphere.GetComponent<Renderer>().material.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.3f);


        // Get the LineRenderer component
        LineRenderer lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer component not found on the GameObject!");
            return;
        }

        Vector3 tangent = Vector3.zero;
        // Calculate the tangent (direction) of the curve at the last point
        if (update)
        {
            Debug.Log($" {index - 2} Tangent update");
            if (index>1)
            {
                tangent = (endPts[index].transform.position - stPts[index - 2].transform.position).normalized;
                Debug.Log($" {index - 2} Tangent update: {tangent} + point {endPts[index].transform.position} + point {stPts[index - 2].transform.position}");
        
            }
            }
        else
        {
            tangent = (endPts.Last().transform.position - stPts[stPts.Count - 2].transform.position).normalized;
            Debug.Log($" {stPts.Count - 2}  {endPts.Count - 1} Tangent: {tangent} + point {endPts.Last().transform.position} + point {stPts[stPts.Count - 2].transform.position}");

        }

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

        // Get the mesh renderer to calculate actual center
        MeshRenderer meshRenderer = element.GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            Debug.LogError("MeshRenderer not found on element!");
            return;
        }

        Vector3 meshCenter = meshRenderer.bounds.center;
        // Create visual marker for mesh center
        GameObject centerSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        centerSphere.transform.parent = element.transform;
        centerSphere.transform.position = meshCenter;
        centerSphere.transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
        centerSphere.GetComponent<Renderer>().material = mat;
        centerSphere.GetComponent<Renderer>().material.color = new Color(Color.yellow.r, Color.yellow.g, Color.yellow.b, 0.3f);


        // Calculate rotation from element's direction to curve's tangent
        Quaternion fromToRotation = Quaternion.FromToRotation(elementDirection, tangent);
        element.transform.rotation = fromToRotation * element.transform.rotation;

        // Position the element so its mesh center aligns with the midPoint
        element.transform.position = midPoint - tangent * timberElement.length*scale / 2 + Vector3.up * 0.5f*scale;

        Debug.Log($"Snapped element '{element.name}' to curve at position {midPoint} with tangent {tangent} and with length {timberElement.length}.");
    

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

        Destroy(stPoint);
        
        stPoint = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        stPoint.transform.parent = this.gameObject.transform;
        stPoint.transform.position = position + this.gameObject.transform.position;
        stPoint.transform.localScale = new Vector3(ctrlPtSize*2, ctrlPtSize*2 , ctrlPtSize*2);
        stPoint.GetComponent<Renderer>().material = material;
        stPoint.name = "StartPoint0";
        stPoint.GetComponent<Renderer>().material.color = new Color(Color.magenta.r, Color.magenta.g, Color.magenta.b, 0.3f);
    

        var manipulator = stPoint.AddComponent<ObjectManipulator>();
        manipulator.AllowedManipulations = TransformFlags.Move; // Enable movement
        manipulator.AllowedInteractionTypes = InteractionFlags.Near | InteractionFlags.Ray; // Allow near or far manipulation
        
        constraint = stPoint.AddComponent<MoveAlongCurveConstraint>();
        constraint.Initialize(GetComponent<LineRenderer>());

        manipulator.selectExited.AddListener(OnManipulationExited);

    }

    private void OnManipulationExited(SelectExitEventArgs args)
    {
        parameter = constraint.parameter;
        ShiftStPosition(constraint.closestIndex);
        UpdateMapping();
    }

     public void ShiftStPosition(int targetIndex)
    {
        // Get the LineRenderer component
        LineRenderer lineRenderer = GetComponent<LineRenderer>();

        // Get the number of positions in the LineRenderer
        int numPoints = lineRenderer.positionCount;

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

