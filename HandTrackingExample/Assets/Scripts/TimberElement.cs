using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using System.Linq;



public class TimberElement : MonoBehaviour
{
    public float width = 0;
    public float height = 0;
    public float length = 0;
    public List<int> types = new List<int>(); // clear areas type=0,  defects type=1, connection areas type=2
    public List<Vector3[]> segments = new List<Vector3[]>(); // List of straight lines (each represented by start and end points)
    public List<float> segLengths = new List<float>();

    public List<float> segWithConnectionsLengths = new List<float>();
    public List<int> typesWithConnections = new List<int>(); // clear areas type=0,  defects type=1, connection areas type=2
    [HideInInspector] public bool flipped = false;
    [HideInInspector] public int rotated = 0;
    [HideInInspector] public bool moved = false;
    [HideInInspector] public float moveDist = 0.0f; // from -0.1f to 0.1f
    [HideInInspector] public LineRenderer lineRenderer;
    private List<GameObject> defects = new List<GameObject>();
    private PlankBender plankBender = new PlankBender();


    public void MarkDefects()
    {
        Color dcolor = new Color(Color.red.r, Color.red.g, Color.red.b, 0.2f);

        for (int i = 0; i < segments.Count; i++)
        {
            if (types[i] == 1)
            {
                GameObject defect = new GameObject($"Defect{i}");
                defect.transform.parent = this.gameObject.transform;

                defects.Add(plankBender.GeneratePlankMesh(
                    Vector3.up, 1.0f, width, height, segments[i], defect, dcolor));
            }  
        }
    }


    public (Vector3 firstPoint, Vector3 lastPoint) CalculateEndPoints()
    {
        // Initialize default values
        Vector3 firstPoint = Vector3.zero;
        Vector3 lastPoint = Vector3.zero;

        if (segments == null || segments.Count == 0)
        {
            Debug.LogWarning("The segments list is null or empty!");
            return (firstPoint, lastPoint);
        }

        // Flatten the list and remove duplicates
        List<Vector3> uniquePoints = segments
            .SelectMany(subarray => subarray) // Flatten the array of arrays
            .Distinct()                      // Remove duplicates
            .ToList();

        if (uniquePoints.Count == 0)
        {
            Debug.LogWarning("No unique points to calculate endpoints!");
            return (firstPoint, lastPoint);
        }

        // Assign the first and last points
        firstPoint = uniquePoints.First();
        lastPoint = uniquePoints.Last();

        // Transform the points to world space using the GameObject's transform
        firstPoint = transform.TransformPoint(firstPoint);
        lastPoint = transform.TransformPoint(lastPoint);

        Debug.Log($"First Point: {firstPoint}, Last Point: {lastPoint}");

        return (firstPoint, lastPoint);
    }

    
    public float CalculateTotalLength()
    {
        float totalLength = 0.0f;
        
        if (segments != null && segments.Count > 0)
        {
            var firstPoint = segments.First().First();
            var lastPoint = segments.Last().Last();
            totalLength = Vector3.Distance(firstPoint, lastPoint);
        }

        return totalLength;
    }

    public void CalculateSegmentLengths()
    {
        segLengths.Clear();
        foreach (var segment in segments)
        {
            float length = 0.0f;
            for (int i = 0; i < segment.Length - 1; i++)
            {
                length += Vector3.Distance(segment[i], segment[i + 1]);
            }
            segLengths.Add(length);
        }
        Debug.Log(this.gameObject.name + " Segment Lengths: " + string.Join(", ", segLengths));
    }

    /// <summary>
    /// Main function that enforces a Type=2 connection of length overlapDistance
    /// at both the start and the end of the segments.
    /// </summary>
    public void AddConnectionsAtEdges( List<int> types, List<float> segLengths, float overlapDistance)
    {
        // First, add a connection at the start
        AddConnectionAtEdge(types, segLengths, overlapDistance, true, out List<int> afterStartTypes, out List<float> afterStartLengths);

        // Next, add a connection at the end of the new lists
        AddConnectionAtEdge(afterStartTypes, afterStartLengths, overlapDistance, false, out typesWithConnections, out segWithConnectionsLengths);
    }

    /// <summary>
    /// Inserts a Connectiion  at either the start or the end (based on `isStart`).
    /// 
    /// If isStart = true:  We inject [2, overlapDistance] at the beginning of the segments.
    /// If isStart = false: We inject [2, overlapDistance] at the end of the segments.
    /// 
    /// The function consumes as many segments (from the relevant edge) as needed  to reach overlapDistance.
    /// If the last segment is only partially consumed, it is split into "leftover" + "consumed portion".
    /// </summary>
    private static void AddConnectionAtEdge( List<int> types, List<float> segLengths, float overlapDistance, bool isStart,  // true -> Insert at start; false -> Insert at end
        out List<int> newTypes, out List<float> newSegLengths)
    {
        // Make local copies so we don't mutate the input lists directly
        // (optional – depends on your usage)
        List<int> workingTypes = new List<int>(types);
        List<float> workingLengths = new List<float>(segLengths);

        // If we are adding at the end, reverse both lists first
        // so we can reuse the "accumulate from front" logic.
        bool reversed = false;
        if (!isStart)
        {
            reversed = true;
            workingTypes.Reverse();
            workingLengths.Reverse();
        }

        // Now we do the same accumulation logic as if we're adding at the start:
        // 1. Accumulate segments from the front until we reach overlapDistance.
        float total = 0f;
        int idx = 0;
        while (idx < workingLengths.Count && total < overlapDistance)
        {
            total += workingLengths[idx];
            idx++;
        }

        // Edge case: if total < overlapDistance, we've consumed all segments but still didn't reach the desired overlapDistance.
        // You can decide whether to clamp or throw an error. For now, clamp:
        if (total < overlapDistance)
        {
            overlapDistance = total;
        }

        float leftover = total - overlapDistance;
        if (leftover < 0) leftover = 0;

        // Prepare the output lists
        List<int> resultTypes = new List<int>();
        List<float> resultLengths = new List<float>();

        // 2. Insert the new [Type=2, overlapDistance] segment at the front of the result (since we've reversed if it's the end).
        resultTypes.Add(2);
        resultLengths.Add(overlapDistance);

        // 3. If leftover > 0, that means the last segment we used was only partially consumed. Keep the remainder as the same original type.
        if (leftover > 0)
        {
            int partialType = workingTypes[idx - 1];
            resultTypes.Add(partialType);
            resultLengths.Add(leftover);
        }

        // 4. Copy the remaining segments (those not consumed) into the result
        for (int j = idx; j < workingTypes.Count; j++)
        {
            resultTypes.Add(workingTypes[j]);
            resultLengths.Add(workingLengths[j]);
        }

        // If we had reversed initially for the "end" case, we must now reverse the final result to restore the original order.
        if (reversed)
        {
            resultTypes.Reverse();
            resultLengths.Reverse();
        }

        // Output
        newTypes = resultTypes;
        newSegLengths = resultLengths;
    }

}

