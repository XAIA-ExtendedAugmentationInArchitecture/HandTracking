using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using System.Linq;



public class BendGeometry : MonoBehaviour
{
    public float width = 0;
    public float height = 0;
    private int index = 0;
    private int originalIndex = 0;
    private float bendValue = 0.0f; // Default bend value
    public List<Vector3[]> initialGeo = new List<Vector3[]>(); // List of straight lines (each represented by start and end points)
    public List<int> types = new List<int>(); // clear areas type=0,  defects type=1, connection areas type=2
    public List<Color> colors = new List<Color>(); // 0 for the selected one, 1 for the clear areas type=0, 2 for the defects type=1 and so on
    private List<bool> side = new List<bool>();
    [HideInInspector] public bool sameDir = true;
    [HideInInspector] public bool moved = false;
    private List<float> bendValues = new List<float>(); // List of values from 0 to 2 indicating the type of each line
    private List<List<Vector3>> bentGeo = new List<List<Vector3>>(); // Nested list for bent pieces (either straight lines or arc-like curves)
    
    private List<LineRenderer> lineRenderers = new List<LineRenderer>(); // List of LineRenderers for visualization
    private List<GameObject> plankObjects = new List<GameObject>();
    private PlankBender plankBender = new PlankBender();
    private MeshCollider meshCollider;
    private Mesh combinedMesh;

    void Start()
    {
        colors.Add(new Color(Color.green.r, Color.green.g, Color.green.b, 0.3f)); // for the selected color
        colors.Add(new Color(Color.white.r, Color.white.g, Color.white.b, 0.3f)); //  for the type = 0
        colors.Add(new Color(Color.red.r, Color.red.g, Color.red.b, 0.3f));  // for the type = 1

    }

    public void ChangeIndex(bool forward, MixedReality.Toolkit.UX.Slider bendSlider, PressableButton sideToggle)
    {
        originalIndex = index;
        bool found = false;

        if (forward)
        {
            // Increment index to the next segment with type = 1
            for (int i = index + 1; i < types.Count; i++)
            {
                if (types[i] == 0)
                {
                    index = i;
                    found = true;
                    bendSlider.Value = bendValues[i];
                    // var interactable = sideToggle.GetComponent<StatefulInteractable>();
                    // if (interactable != null)
                    // {
                    //     interactable.ForceSetToggled(side[i]); // Use SetToggled() method
                    // }
                    // else
                    // {
                    //     Debug.LogWarning("StatefulInteractable component missing on PressableButton.");
                    // }
                    break;
                }
            }

            // If not found, start from the beginning and check up to the original index
            if (!found)
            {
                for (int i = 0; i <= originalIndex; i++)
                {
                    if (types[i] == 0)
                    {
                        index = i;
                        bendSlider.Value = bendValues[i];
                        // var interactable = sideToggle.GetComponent<StatefulInteractable>();
                        // if (interactable != null)
                        // {
                        //     interactable.ForceSetToggled(side[i]); // Use SetToggled() method
                        // }
                        // else
                        // {
                        //     Debug.LogWarning("StatefulInteractable component missing on PressableButton.");
                        // }
                        break;
                    }
                }
            }
        }
        else
        {
            // Decrement index to the previous segment with type = 1
            for (int i = index - 1; i >= 0; i--)
            {
                if (types[i] == 0)
                {
                    index = i;
                    found = true;
                    bendSlider.Value = bendValues[i];
                    // var interactable = sideToggle.GetComponent<StatefulInteractable>();
                    // if (interactable != null)
                    // {
                    //     interactable.ForceSetToggled(side[i]); // Use SetToggled() method
                    // }
                    // else
                    // {
                    //     Debug.LogWarning("StatefulInteractable component missing on PressableButton.");
                    // }
                    break;
                }
            }

            // If not found, start from the end and check down to the original index
            if (!found)
            {
                for (int i = types.Count - 1; i >= originalIndex; i--)
                {
                    if (types[i] == 0)
                    {
                        index = i;
                        bendSlider.Value = bendValues[i];
                        // var interactable = sideToggle.GetComponent<StatefulInteractable>();
                        // if (interactable != null)
                        // {
                        //     interactable.ForceSetToggled(side[i]); // Use SetToggled() method
                        // }
                        // else
                        // {
                        //     Debug.LogWarning("StatefulInteractable component missing on PressableButton.");
                        // }
                        break;
                    }
                }
            }
        }
        Debug.Log ("ABCA " + index + "  " + bendValue + "  " +  bendValues[index]);
        ProcessBending();
    }

    
    public void OnSideToggleChanged(bool isOn)
    {
        side[index] = isOn;
        Debug.Log ("ABCError ");
        ProcessBending();
    }

    public void OnBendSliderChanged(float value)
    {
        if (value >= 0.5f && value <= 1.0f)
        {
            // Remap [0.5, 1] to [0, 0.99]
            bendValue = Mathf.Lerp(0.0f, 0.99f, (value - 0.5f) / 0.5f);
            side[index] = true;
        }
        else if (value >= 0.0f && value < 0.5f)
        {
            // Remap [0, 0.49] to [0.99, 0.00]
            bendValue = Mathf.Lerp(0.99f, 0.00f, value / 0.5f);
            side[index] = false;
        }
        bendValues[index] = value; 
        ProcessBending();
    }



    public void InitializePlankMeshes()
    {
        if (initialGeo.Count != bentGeo.Count)
        {
            Debug.LogError($"Mismatch: initialGeo.Count ({initialGeo.Count}) != bentGeo.Count ({bentGeo.Count})");
            return;
        }

        plankObjects.Clear(); // Clear old objects

        for (int i = 0; i < initialGeo.Count; i++)
        {
            GameObject plankObj = new GameObject($"Plank{i}");
            plankObj.transform.parent = this.gameObject.transform;

            Color plankColor = (types[i] == 0) ? colors[1] : colors[2];

            plankObjects.Add(plankBender.GeneratePlankMesh(
                Vector3.up, 1.0f, width, height, initialGeo[i], plankObj, plankColor));
        }

        UpdatePlankMeshes();
        UpdateMeshCollider();
    }

    public void InitializeValues()
    {
        for (int i = 0; i < initialGeo.Count; i++)
        {
            bendValues.Add(0.5f);
            side.Add(true);
            bentGeo.Add(new List<Vector3>(initialGeo[i]));

        }
    }
    public void InitializeColors()
    {
        colors.Add(new Color(Color.green.r, Color.green.g, Color.green.b, 0.3f)); // for the selected color
        colors.Add(new Color(Color.white.r, Color.white.g, Color.white.b, 0.3f)); //  for the type = 0
        colors.Add(new Color(Color.red.r, Color.red.g, Color.red.b, 0.3f));  // for the type = 1
    }

    public void InitializeLineRenderers()
    {
        // Create LineRenderers for each initial geometry segment
        int i=0;
        foreach (var segment in initialGeo)
        {
            GameObject lineObj = new GameObject("Segment" + i.ToString());
            lineObj.transform.parent = this.gameObject.transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();

            lr.startWidth = 0.05f; // Specify the desired start width
            lr.endWidth = 0.05f;

            lineRenderers.Add(lr);
            i++;
        }

        // UpdateLineRenderers();

    }

    void UpdateLineRenderers()
    {
        // Update LineRenderers to reflect current bentGeo state
        for (int i = 0; i < bentGeo.Count; i++)
        {
            LineRenderer lr = lineRenderers[i];
            lr.positionCount = bentGeo[i].Count;
            lr.SetPositions(bentGeo[i].ToArray());
        }
    }

    public List<Vector3> CombineLineRendererPoints()
    {
        List<Vector3> combinedPoints = new List<Vector3>();

        foreach (var lineRenderer in lineRenderers)
        {
            int pointCount = lineRenderer.positionCount; // Get the number of points in the LineRenderer
            Vector3[] points = new Vector3[pointCount];
            lineRenderer.GetPositions(points); // Get all the positions from the LineRenderer

            foreach (var point in points)
            {
                // Add the point if it is not the same as the last point in the combined list
                if (combinedPoints.Count == 0 || combinedPoints[combinedPoints.Count - 1] != point)
                {
                    combinedPoints.Add(point);
                }
            }
        }

        return combinedPoints;
    }    
    void UpdatePlankMeshes()
    {
        if (bentGeo.Count != plankObjects.Count)
        {
            Debug.LogError($"Mismatch: bentGeo.Count ({bentGeo.Count}) != plankObjects.Count ({plankObjects.Count})");
            return;
        }

        for (int i = 0; i < bentGeo.Count; i++)
        {
            Vector3[] updatedPolyline = bentGeo[i].ToArray();
            GameObject plankObj = plankObjects[i];

            Color plankColor = (i == index) ? colors[0] : (types[i] == 0 ? colors[1] : colors[2]);

            plankBender.GeneratePlankMesh(Vector3.up, 1.0f, width, height, updatedPolyline, plankObj, plankColor);
        }
    }

    public void RecolorPlankMeshes()
    {
        if (bentGeo.Count != plankObjects.Count)
        {
            Debug.LogError($"Mismatch: bentGeo.Count ({bentGeo.Count}) != plankObjects.Count ({plankObjects.Count})");
            return;
        }

        for (int i = 0; i < bentGeo.Count; i++)
        {
            Vector3[] updatedPolyline = bentGeo[i].ToArray();
            GameObject plankObj = plankObjects[i];

            Color plankColor = (types[i] == 0) ? colors[1] : colors[2];

            plankBender.GeneratePlankMesh(Vector3.up, 1.0f, width, height, updatedPolyline, plankObj, plankColor);
        }
    }

    void UpdateMeshCollider()
    {
        // Ensure combinedMesh is initialized
        if (combinedMesh == null)
        {
            combinedMesh = new Mesh();
        }
        else
        {
            combinedMesh.Clear();
        }

        // Get all child MeshFilters, excluding the parent GameObject's own MeshFilter
        MeshFilter[] childMeshFilters = this.gameObject.GetComponentsInChildren<MeshFilter>();
        List<CombineInstance> combineInstances = new List<CombineInstance>();

        foreach (MeshFilter meshFilter in childMeshFilters)
        {
            // Skip the MeshFilter of the parent GameObject
            if (meshFilter.gameObject == this.gameObject)
                continue;

            CombineInstance combine = new CombineInstance
            {
                mesh = meshFilter.sharedMesh,
                transform = this.transform.worldToLocalMatrix * meshFilter.transform.localToWorldMatrix
            };
            combineInstances.Add(combine);
        }

        // Combine meshes into one
        combinedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

        // Ensure the MeshCollider exists
        if (meshCollider == null)
        {
            meshCollider = this.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                meshCollider = this.gameObject.AddComponent<MeshCollider>();
            }
        }

        // Update the MeshCollider
        meshCollider.sharedMesh = null; // Clear previous mesh reference
        meshCollider.sharedMesh = combinedMesh;
    }


    void ProcessBending() /// the original
    {
        if (types[index] != 0)
        {
            Debug.Log("Cannot bend this part");
            return;
        }

        Vector3[] segment = initialGeo[index];
        Vector3 startPt = segment[0];
        Vector3 endPt = segment[1];
        Vector3 tangent = (endPt - startPt).normalized;

        float dmax = Vector3.Distance(startPt, endPt);

        float value = Mathf.Lerp(0, dmax - (2 * dmax / Mathf.PI), bendValue);
        Vector3 endPtTr = endPt - (tangent * value);

        (Vector3[] arc, bool isLine) = ComputeArc(startPt, endPtTr, dmax, side[index]);

        if (isLine)
        {
            arc = segment;
        }

        bentGeo[index] = new List<Vector3>(arc);

        AlignSegments();
        // UpdateLineRenderers(); 
        UpdatePlankMeshes();
        UpdateMeshCollider();
    }

    (Vector3[], bool) ComputeArc(Vector3 A, Vector3 B, float s, bool side)
    {
        // Calculate chord length c
        Vector3 D = B - A;
        float c = D.magnitude;
        if (c >= s)
        {
            // Chord length is greater than or equal to arc length; no solution, return a line
            Debug.Log("Chord length is greater than or equal to arc length. No arc possible.");
            return (new Vector3[] { A, B }, true);  // Returning as a line
        }

        float a = c / s;

        // Function f(x) = sin(x) - a * x
        System.Func<float, float> f = x => Mathf.Sin(x) - a * x;

        // Bisection method parameters
        float xLow = 1e-6f;
        float xHigh = Mathf.PI / 2.0f - 1e-6f;

        // Check if f(xLow) and f(xHigh) have opposite signs
        if (f(xLow) * f(xHigh) > 0)
        {
            Debug.Log("No solution found in the interval.");
            return (null, false);
        }

        // Tolerance and iteration limit
        float tol = 1e-6f;
        int maxIter = 100;
        int iterCount = 0;

        float xMid = 0f;
        while ((xHigh - xLow) > tol && iterCount < maxIter)
        {
            xMid = (xLow + xHigh) / 2.0f;
            float fMid = f(xMid);
            if (fMid == 0)
            {
                break;
            }
            else if (f(xLow) * fMid < 0)
            {
                xHigh = xMid;
            }
            else
            {
                xLow = xMid;
            }
            iterCount++;
        }
        float x = (xLow + xHigh) / 2.0f;

        // Compute radius r
        float r = s / (2 * x);

        // Compute distance from midpoint to center
        float d = Mathf.Sqrt(r * r - (c / 2.0f) * (c / 2.0f));

        // Compute midpoint M
        Vector3 M = (A + B) / 2.0f;

        // Unit vector along chord
        Vector3 u = D.normalized;

        // Normal vector to chord
        Vector3 n = Vector3.Cross(u, Vector3.up).normalized;
        // Vector3 n = Vector3.Cross(u, Vector3.forward).normalized;

        // Determine the center C based on the side parameter
        Vector3 C = M + (side ? n * d : -n * d);
        // Vector3 C = side ? M + n * d : M - n * d;

        // Create arc points (approximated as a few points for simplicity)
        int numPoints = 20;  // Number of points to approximate the arc
        Vector3[] arcPoints = new Vector3[numPoints];
        float angleStep = (2 * x) / (numPoints - 1);

        for (int i = 0; i < numPoints; i++)
        {
            float angle = side ? -x + i * angleStep : x - i * angleStep;
            // float angle = -x + i * angleStep;
            float xCoord = C.x + r * Mathf.Cos(angle);
            float zCoord = C.z + r * Mathf.Sin(angle);
            arcPoints[i] = new Vector3(xCoord, 0, zCoord);
        }

        return (arcPoints, false);  // Returning as an arc
    }


    void AlignSegments() 
    {
        if (bentGeo.Count > 0 && bentGeo[0].Count > 0)
        {
            Vector3 firstPoint = bentGeo[0][0];
            Vector3 translationToOrigin = Vector3.zero - firstPoint;

            for (int j = 0; j < bentGeo[0].Count; j++)
            {
                bentGeo[0][j] += translationToOrigin; // Move each point of the first segment
            }
        }

        // Logic to align segments based on previous ones
        for (int i = 1; i < bentGeo.Count; i++)
        {
            List<Vector3> segment = bentGeo[i];
            List<Vector3> prevSegment = bentGeo[i - 1];

            // Translate the segment to align with the end of the previous segment
            Vector3 translation = prevSegment[prevSegment.Count - 1] - segment[0];
            for (int j = 0; j < segment.Count; j++)
            {
                segment[j] += translation; // Apply translation
            }

            // Align arc direction (adjusted for XZ plane)
            Vector3 tangent = (segment[1] - segment[0]).normalized;
            Vector3 prevTangent = (prevSegment[prevSegment.Count - 1] - prevSegment[prevSegment.Count - 2]).normalized;
            float angle = Vector3.SignedAngle(tangent, prevTangent, Vector3.up); // Rotate around Y-axis for XZ plane

            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up); // Adjust rotation axis for XZ plane
            for (int j = 0; j < segment.Count; j++)
            {
                // Apply rotation relative to the translation point (end of previous segment)
                segment[j] = rotation * (segment[j] - prevSegment[prevSegment.Count - 1]) + prevSegment[prevSegment.Count - 1];
            }

            bentGeo[i] = segment;
        }
    }

    public Vector3 CalculateMeanPoint()
    {
        Vector3 meanPt = Vector3.zero;

        if (bentGeo == null || bentGeo.Count == 0)
        {
            Debug.LogWarning("The bentGeo list is null or empty!");
            return meanPt;
        }

        // Flatten the list and remove duplicates
        List<Vector3> uniquePoints = bentGeo
            .SelectMany(sublist => sublist) // Flatten the list of lists
            .Distinct()                     // Remove duplicates
            .ToList();

        if (uniquePoints.Count == 0)
        {
            Debug.LogWarning("No unique points to calculate mean!");
            return meanPt;
        }

        // Calculate the mean point
        Vector3 sum = Vector3.zero;
        foreach (Vector3 localPoint in uniquePoints)
        {
            // Convert each point from local space to world space
            Vector3 globalPoint = transform.TransformPoint(localPoint);
            sum += globalPoint;
        }

        meanPt = sum / uniquePoints.Count;
        Debug.Log($"Mean Point: {meanPt}");

        return meanPt;
    }

    public (Vector3 firstPoint, Vector3 lastPoint) CalculateEndPoints()
{
    // Initialize default values
    Vector3 firstPoint = Vector3.zero;
    Vector3 lastPoint = Vector3.zero;

    if (bentGeo == null || bentGeo.Count == 0)
    {
        Debug.LogWarning("The bentGeo list is null or empty!");
        return (firstPoint, lastPoint);
    }

    // Flatten the list and remove duplicates
    List<Vector3> uniquePoints = bentGeo
        .SelectMany(sublist => sublist) // Flatten the list of lists
        .Distinct()                     // Remove duplicates
        .ToList();

    if (uniquePoints.Count == 0)
    {
        Debug.LogWarning("No unique points to calculate endpoints!");
        return (firstPoint, lastPoint);
    }

    // Assign the first and last points
    firstPoint = uniquePoints.First();
    lastPoint = uniquePoints.Last();

    Debug.Log($"First Point: {firstPoint}, Last Point: {lastPoint}");

    return (firstPoint, lastPoint);
}


}
