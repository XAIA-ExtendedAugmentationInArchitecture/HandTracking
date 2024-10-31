using System.Collections.Generic;
using UnityEngine;

public class LineConnector : MonoBehaviour
{
    public LineRenderer[] lines; // Array of Line Renderers to connect
    public int divisions = 10; // Number of divisions for each line

    private List<LineRenderer> connectionLines = new List<LineRenderer>();

    void Start()
    {
        CreateConnectionLines();
    }

    void Update()
    {
        UpdateConnectionLines();
    }

    void CreateConnectionLines()
    {
        // Clear previous connection lines
        foreach (var line in connectionLines)
        {
            Destroy(line.gameObject);
        }
        connectionLines.Clear();

        // Create new connection lines based on the number of divisions
        for (int i = 0; i < divisions; i++)
        {
            LineRenderer newLine = new GameObject($"ConnectionLine_{i}").AddComponent<LineRenderer>();
            newLine.positionCount = lines.Length; // Number of lines to connect
            newLine.startWidth = 0.05f; // Set width for the line
            newLine.endWidth = 0.05f; // Set width for the line
            connectionLines.Add(newLine);
        }
    }

    void UpdateConnectionLines()
    {
        // Calculate positions for each connection line based on divisions
        for (int i = 0; i < connectionLines.Count; i++)
        {
            Vector3[] points = new Vector3[lines.Length];

            for (int j = 0; j < lines.Length; j++)
            {
                float t = (float)i / (divisions - 1); // Normalize division index to [0, 1]
                points[j] = GetPointAtT(lines[j], t); // Get the interpolated point at t
            }

            connectionLines[i].SetPositions(points);
        }
    }

    Vector3 GetPointAtT(LineRenderer line, float t)
    {
        // Calculate the position at t for the LineRenderer using linear interpolation
        int pointCount = line.positionCount;

        // Return the only point if there's one
        if (pointCount < 2)
            return line.GetPosition(0); 

        // Calculate the segment index and ensure it's within bounds
        float segmentLength = 1f / (pointCount - 1);
        int segmentIndex = Mathf.FloorToInt(t / segmentLength);

        // Ensure segmentIndex does not exceed the number of points
        segmentIndex = Mathf.Clamp(segmentIndex, 0, pointCount - 2); // Clamp to [0, pointCount - 2]

        // Calculate the local t within that segment
        float localT = (t - (segmentIndex * segmentLength)) / segmentLength;

        // Get the positions of the segment's endpoints
        Vector3 startPos = line.GetPosition(segmentIndex);
        Vector3 endPos = line.GetPosition(segmentIndex + 1);

        // Interpolate between the two endpoints
        return Vector3.Lerp(startPos, endPos, localT);
    }
}
