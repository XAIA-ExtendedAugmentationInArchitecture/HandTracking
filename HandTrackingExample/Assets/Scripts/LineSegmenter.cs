using UnityEngine;
using System.Collections.Generic;

public class LineSegmenter : MonoBehaviour
{
    public LineRenderer lineRenderer;  // Reference to the original LineRenderer component
    public List<float> segmentLengths; // List of segment lengths
    public Material lineMaterial;      // Material for the new LineRenderers
    public float lineWidth = 0.1f;     // Width of the new line segments

    private void Start()
    {
        // Example initialization if needed
        // segmentLengths = new List<float> { 0.1f, 0.25f, 0.05f };
        // SegmentLine();
    }

    public void SegmentLine()
    {
        int pointCount = lineRenderer.positionCount;
        Vector3[] points = new Vector3[pointCount];
        lineRenderer.GetPositions(points);

        // Calculate the total length of the line
        List<float> segmentDistances = new List<float>();
        float totalLineLength = 0f;

        for (int i = 0; i < points.Length - 1; i++)
        {
            float distance = Vector3.Distance(points[i], points[i + 1]);
            segmentDistances.Add(distance);
            totalLineLength += distance;
        }

        // Now, we need to split the line based on the provided lengths
        float currentSegmentLength = 0f;
        int currentPointIndex = 0;
        float distanceCovered = 0f;

        for (int segmentIndex = 0; segmentIndex < segmentLengths.Count; segmentIndex++)
        {
            float targetLength = segmentLengths[segmentIndex];  // The target segment length
            currentSegmentLength = 0f;
            List<Vector3> segmentPoints = new List<Vector3> { points[currentPointIndex] };

            while (currentSegmentLength < targetLength && currentPointIndex < segmentDistances.Count)
            {
                float segmentDistance = segmentDistances[currentPointIndex];
                float remainingLength = targetLength - currentSegmentLength;

                if (distanceCovered + segmentDistance >= remainingLength)
                {
                    // Create a segment for the remaining length
                    float ratio = remainingLength / segmentDistance;
                    Vector3 newPoint = Vector3.Lerp(points[currentPointIndex], points[currentPointIndex + 1], ratio);
                    segmentPoints.Add(newPoint);

                    Vector3[] projectedPoints = ProjectPointsOnPlane(segmentPoints.ToArray());
                   
                    float factor = (float)segmentIndex / (segmentLengths.Count - 1);  // Calculate factor based on index
                    Color segmentColor = lineMaterial.color.RainbowColor(factor);  // Get the color using the factor
                    CreateLineRenderer(projectedPoints, segmentColor); // Create a new LineRenderer for this segment

                    
                    // Update the segmentLengths
                    segmentLengths[segmentIndex] = 0.0f;  // Fully used up this segment

                    // Move to the next segment
                    currentSegmentLength = targetLength;
                    float leftoverDistance = segmentDistance - remainingLength;
                    
                    // If there's leftover distance, reduce the next segment length
                    if (leftoverDistance > 0 && segmentIndex + 1 < segmentLengths.Count)
                    {
                        segmentLengths[segmentIndex + 1] -= leftoverDistance;
                    }
                    distanceCovered = 0f;
                    currentPointIndex++;

                    break;  // Move to the next target length
                }
                else
                {
                    currentSegmentLength += segmentDistance;
                    distanceCovered += segmentDistance;
                    segmentPoints.Add(points[currentPointIndex + 1]);
                    currentPointIndex++;
                }
            }

            // If the total line length is shorter than the target length, handle the remaining part
            if (currentPointIndex >= segmentDistances.Count)
            {
                float remainingLineLength = totalLineLength - currentSegmentLength;
                if (remainingLineLength > 0)
                {
                    // Create a final segment with the remaining length
                    Vector3[] projectedPoints = ProjectPointsOnPlane(segmentPoints.ToArray());
                    float factor = (float)segmentIndex / (segmentLengths.Count - 1);  // Calculate factor based on index
                    Color segmentColor = lineMaterial.color.RainbowColor(factor);  // Get the color using the factor
                    CreateLineRenderer(projectedPoints, segmentColor);  // Create the final segment for the remaining curve
                    segmentLengths[segmentIndex] -= remainingLineLength;
                }
                break;
            }
        }
    }





    private Vector3[] ProjectPointsOnPlane(Vector3[] points)
    {
        // 1. Get the first point, last point, and calculate the mean of all other points
        Vector3 firstPoint = points[0];
        Vector3 lastPoint = points[points.Length - 1];
        Vector3 meanPoint = CalculateMean(points);

        // 2. Define the plane using the first, last, and mean points
        Plane projectionPlane = new Plane(firstPoint, lastPoint, meanPoint);

        // 3. Project each point onto the plane
        Vector3[] projectedPoints = new Vector3[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            projectedPoints[i] = ProjectPointOnPlane(projectionPlane, points[i]);
        }

        return projectedPoints;
    }

    private Vector3 CalculateMean(Vector3[] points)
    {
        Vector3 sum = Vector3.zero;
        for (int i = 1; i < points.Length - 1; i++) // Exclude first and last point
        {
            sum += points[i];
        }
        return sum / (points.Length - 2); // Mean of the intermediate points
    }

    private Vector3 ProjectPointOnPlane(Plane plane, Vector3 point)
    {
        // Calculate the distance from the point to the plane
        float distance = plane.GetDistanceToPoint(point);

        // Project the point onto the plane by moving it along the normal by the distance
        return point - plane.normal * distance;
    }

    // Function to create a new LineRenderer for each segment
    private void CreateLineRenderer(Vector3[] segmentPoints, Color segmentColor )
    {
        // Create a new GameObject to hold the LineRenderer
        GameObject lineObject = new GameObject("SegmentLineRenderer");
        LineRenderer segmentLineRenderer = lineObject.AddComponent<LineRenderer>();
        Debug.Log ( "Hola3");
        // Set properties for the new LineRenderer
        segmentLineRenderer.material = lineMaterial;
        segmentLineRenderer.material.color = segmentColor;  // Set the color
        segmentLineRenderer.startWidth = lineWidth;
        segmentLineRenderer.endWidth = lineWidth;

        // Set the positions for the LineRenderer
        segmentLineRenderer.positionCount = segmentPoints.Length;
        segmentLineRenderer.SetPositions(segmentPoints);

        // Optional: Customize the rendering layer, sorting, or other properties
    }
}
