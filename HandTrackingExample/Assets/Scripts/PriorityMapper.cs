using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PriorityMapper : MonoBehaviour
{
    public LineRenderer lineRenderer;
    public int newStartIndex;

    // Set a new start point by specifying its index in the original points array
    public void ShiftStartPoint(int newStartIndex)
    {
        if (lineRenderer == null)
        {
            Debug.LogWarning("LineRenderer is not assigned!");
            return;
        }
        if (lineRenderer.loop)
        {
            Debug.LogWarning("Curve is not periodic!");
            return;
        }

        int pointCount = lineRenderer.positionCount;
        if (newStartIndex < 0 || newStartIndex >= pointCount)
        {
            Debug.LogWarning("Invalid newStartIndex!");
            return;
        }

        // Get the current positions
        Vector3[] positions = new Vector3[pointCount];
        lineRenderer.GetPositions(positions);

        // Create a new array for reordered points
        Vector3[] reorderedPositions = new Vector3[pointCount];

        // Fill the new array starting from the newStartIndex
        int index = 0;
        for (int i = newStartIndex; i < pointCount; i++)
        {
            reorderedPositions[index] = positions[i];
            index++;
        }
        for (int i = 0; i < newStartIndex; i++)
        {
            reorderedPositions[index] = positions[i];
            index++;
        }

        // Update the LineRenderer with the new order of points
        lineRenderer.SetPositions(reorderedPositions);
    }

    (Vector3, int, float, float) FindClosestPoint(Vector3 point, LineRenderer curve)
    {
        Vector3 closestPoint = Vector3.zero;
        float minDistance = Mathf.Infinity;
        int closestIndex = -1;

        // Get the number of points in the LineRenderer
        int pointCount = curve.positionCount;
        Vector3[] positions = new Vector3[pointCount];
        curve.GetPositions(positions);

        // Iterate over each point to find the closest
        for (int i = 0; i < pointCount; i++)
        {
            float distance = Vector3.Distance(point, positions[i]);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestPoint = positions[i];
                closestIndex = i;
            }
        }

        // Remap the closest index to the range [0, 1]
        float normalizedParameter = pointCount > 1 ? closestIndex / (float)(pointCount - 1) : 0;

        return (closestPoint, closestIndex, minDistance, normalizedParameter);
    }


    bool FindDirection(List<Vector3> points, LineRenderer curve)
    {
        if (points == null || points.Count < 2)
        {
            Debug.LogWarning("The list of points is null or does not have enough points!");
            return false;
        }

        if (curve == null)
        {
            Debug.LogWarning("LineRenderer is not assigned!");
            return false;
        }

        // Find the closest points to the start and end points of the list
        var (closestStartPoint, startIndex, startDistance, _) = FindClosestPoint(points[0], curve);
        var (closestEndPoint, endIndex, endDistance, _) = FindClosestPoint(points[points.Count - 1], curve);

        // Compare indices to determine the direction
        if (startIndex < endIndex)
        {
            return true; // Direction remains the same
        }
        else
        {
            return false; // Direction is reversed
        }
    }


    Vector3 CalculateMeanPoint(List<Vector3> points)
    {
        if (points == null || points.Count == 0)
        {
            Debug.LogWarning("The list of points is null or empty!");
            return Vector3.zero;
        }

        Vector3 sum = Vector3.zero;
        foreach (Vector3 point in points)
        {
            sum += point;
        }

        Vector3 meanPoint = sum / points.Count;
        return meanPoint;
    }

    // public Dictionary<string, List<Vector3>> MapPointsToLineRenderers(
    // List<LineRenderer> lineRenderers,
    // List<Vector3> points,
    // float threshold)
    // {
    //     // Dictionary to store the mapping of LineRenderer's GameObject name to points
    //     Dictionary<string, List<Vector3>> lineToPointMap = new Dictionary<string, List<Vector3>>();

    //     // Initialize the dictionary with empty lists for each LineRenderer
    //     foreach (LineRenderer line in lineRenderers)
    //     {
    //         string gameObjectName = line.gameObject.name;
    //         lineToPointMap[gameObjectName] = new List<Vector3>();
    //     }

    //     // Track which points have already been assigned
    //     HashSet<Vector3> assignedPoints = new HashSet<Vector3>();

    //     foreach (Vector3 point in points)
    //     {
    //         LineRenderer closestLine = null;
    //         string closestLineName = null;
    //         int closestIndex = -1;
    //         float minDistance = Mathf.Infinity;

    //         // Find the closest LineRenderer for the current point
    //         foreach (LineRenderer line in lineRenderers)
    //         {
    //             var (_, index, distance) = FindClosestPoint(point, line);

    //             if (distance < minDistance && distance <= threshold)
    //             {
    //                 minDistance = distance;
    //                 closestLine = line;
    //                 closestLineName = line.gameObject.name;
    //                 closestIndex = index;
    //             }
    //         }

    //         // If a closest LineRenderer was found within the threshold, assign the point to it
    //         if (closestLine != null && closestLineName != null && !assignedPoints.Contains(point))
    //         {
    //             lineToPointMap[closestLineName].Add(point);
    //             assignedPoints.Add(point); // Mark this point as assigned
    //         }
    //     }

    //     // Ensure the points for each LineRenderer are sorted by their closest index
    //     foreach (var key in lineToPointMap.Keys)
    //     {
    //         LineRenderer line = lineRenderers.Find(l => l.gameObject.name == key);
    //         if (line == null) continue;

    //         List<(Vector3 point, int index)> validPoints = new List<(Vector3, int)>();
    //         foreach (var point in lineToPointMap[key])
    //         {
    //             var (_, index, _) = FindClosestPoint(point, line);
    //             validPoints.Add((point, index));
    //         }

    //         validPoints.Sort((a, b) => a.index.CompareTo(b.index));

    //         // Update the dictionary with the sorted points
    //         lineToPointMap[key] = new List<Vector3>();
    //         foreach (var (point, _) in validPoints)
    //         {
    //             lineToPointMap[key].Add(point);
    //         }
    //     }

    //     return lineToPointMap;
    // }

    public Dictionary<LineRenderer, List<string>> MapPointsToLineRenderers(
    List<LineRenderer> lineRenderers,
    List<Vector3> points,
    List<string> pointNames,
    float threshold)
    {
        // Dictionary to store the mapping of LineRenderer to point names
        Dictionary<LineRenderer, List<string>> lineToPointMap = new Dictionary<LineRenderer, List<string>>();

        // Initialize the dictionary with empty lists for each LineRenderer
        foreach (LineRenderer line in lineRenderers)
        {
            lineToPointMap[line] = new List<string>();
        }

        // Track which points have already been assigned
        HashSet<int> assignedPointIndices = new HashSet<int>();

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 point = points[i];
            string pointName = pointNames[i];

            LineRenderer closestLine = null;
            int closestIndex = -1;
            float minDistance = Mathf.Infinity;

            // Find the closest LineRenderer for the current point
            foreach (LineRenderer line in lineRenderers)
            {
                var (_, index, distance,_) = FindClosestPoint(point, line);

                if (distance < minDistance && distance <= threshold)
                {
                    minDistance = distance;
                    closestLine = line;
                    closestIndex = index;
                }
            }

            // If a closest LineRenderer was found within the threshold, assign the point's name to it
            if (closestLine != null && !assignedPointIndices.Contains(i))
            {
                lineToPointMap[closestLine].Add(pointName);
                assignedPointIndices.Add(i); // Mark this point as assigned
            }
        }

        // Ensure the names for each LineRenderer are sorted by their closest index
        foreach (var key in lineToPointMap.Keys.ToList())
        {
            LineRenderer line = key;

            List<(string name, int index)> validPoints = new List<(string, int)>();
            foreach (var pointName in lineToPointMap[line])
            {
                int pointIndex = pointNames.IndexOf(pointName);
                var (_, index, _, _) = FindClosestPoint(points[pointIndex], line);
                validPoints.Add((pointName, index));
            }

            validPoints.Sort((a, b) => a.index.CompareTo(b.index));

            // Update the dictionary with the sorted names
            lineToPointMap[line] = new List<string>();
            foreach (var (name, _) in validPoints)
            {
                lineToPointMap[line].Add(name);
            }
        }

        return lineToPointMap;
    }

    public Dictionary<string, List<(List<(string pointName, bool direction, List<List<float>> intervals)>, float parameter)>> MergePriorityData(
    Dictionary<LineRenderer, List<string>> priorityOrder,
    List<Vector3> points,
    List<List<List<float>>> parts,
    List<(Vector3 start, Vector3 end)> endPoints,
    List<string> pointNames,
    List<LineRenderer> lineRenderers)
    {
        // Dictionary to store the directions for each LineRenderer
        Dictionary<LineRenderer, bool> lineDirection = new Dictionary<LineRenderer, bool>();

        // Determine direction for each LineRenderer using the first pair of endPoints
        foreach (var line in lineRenderers)
        {
            if (priorityOrder.ContainsKey(line))
            {
                bool direction = FindDirection(new List<Vector3> { endPoints[0].start, endPoints[0].end }, line);
                lineDirection[line] = direction;
            }
        }

        // Dictionary to store the closest distance for each LineRenderer
        Dictionary<LineRenderer, float> lineDistances = new Dictionary<LineRenderer, float>();

        foreach (var line in lineRenderers)
        {
            if (lineDirection.ContainsKey(line))
            {
                // Use start or end based on the direction
                Vector3 referencePoint = lineDirection[line] ? endPoints[0].start : endPoints[0].end;

                // Find the closest distance to the LineRenderer
                var (_, _,_, parameter) = FindClosestPoint(referencePoint, line);
                lineDistances[line] = parameter;
            }
        }

        // Final dictionary to store the merged priority data
        Dictionary<string, List<(List<(string pointName, bool direction, List<List<float>> intervals)>, float parameter)>> priority =
            new Dictionary<string, List<(List<(string, bool, List<List<float>>)> , float)>>();

        // Combine data into the required format
        foreach (var kvp in priorityOrder)
        {
            LineRenderer line = kvp.Key;
            List<string> assignedPointNames = kvp.Value;
            bool direction = lineDirection[line];
            float parameter = lineDistances[line];

            List<(string, bool, List<List<float>>)> innerData = new List<(string, bool, List<List<float>>)>();

            for (int i = 0; i < assignedPointNames.Count; i++)
            {
                string pointName = assignedPointNames[i];
                List<List<float>> intervals = parts[i];

                // Add point data to the inner list
                innerData.Add((pointName, direction, intervals));
            }

            // Combine inner data with the single distance (parameter)
            priority[line.name] = new List<(List<(string, bool, List<List<float>>)>, float)>
            {
                (innerData, parameter)
            };
        }

        return priority;
    }




    // Vector3 CalculateMeanPoint(LineRenderer curve)
    // {
    //     if (curve == null)
    //     {
    //         Debug.LogWarning("LineRenderer is not assigned!");
    //         return Vector3.zero;
    //     }

    //     int pointCount = curve.positionCount;
    //     if (pointCount == 0)
    //     {
    //         Debug.LogWarning("LineRenderer has no points!");
    //         return Vector3.zero;
    //     }

    //     Vector3[] positions = new Vector3[pointCount];
    //     curve.GetPositions(positions);

    //     Vector3 sum = Vector3.zero;
    //     foreach (Vector3 point in positions)
    //     {
    //         sum += point;
    //     }

    //     Vector3 meanPoint = sum / pointCount;
    //     return meanPoint;
    // }


}
