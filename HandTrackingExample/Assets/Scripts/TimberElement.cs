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


}


        // HashSet<Vector3> uniquePoints = new HashSet<Vector3>();

        // foreach (var segment in segments)
        // {
        //     for (int i = 0; i < segment.Length - 1; i++)
        //     {
        //         // Check if the points are already in the set to avoid duplicates
        //         if (uniquePoints.Add(segment[i]) && uniquePoints.Add(segment[i + 1]))
        //         {
        //             totalLength += Vector3.Distance(segment[i], segment[i + 1]);
        //         }
        //     }
        // }
