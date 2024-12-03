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

public class CurveManipulator : MonoBehaviour 
    
{
    [HideInInspector] public LineRenderer lineRenderer;  // Reference to the original LineRenderer component
    public List<float> plankLengths; // List of segment lengths
    public Material lineMaterial;      // Material for the new LineRenderers
    public float lineWidth = 0.25f;     // Width of the new line segments
    public Plane projectionPlane ;

    private CurveControlPoint[] controlPoints = new CurveControlPoint[0];
    [HideInInspector] public Vector3[] controlPositions = new Vector3[0];
    private Vector3[] oldControlPositions = new Vector3[0];
    [HideInInspector] public bool saved = false;
    [HideInInspector] public bool hasControlPoints = false;
    [HideInInspector] public float smoothSegSize = 0.2f;
    private float ctrlPtSize = 0.015f;
    private bool isPeriodic = false;

    private void Start()
    {

    }

    public void Update () 
	{
        GetControlPoints();
        AdjustCurve();

	}

    public GameObject InstantiateCurve(int index, GameObject parentObject, Material lineMaterial, float lineWidth, bool isPeriodic, string name="", string tag="")
    {
        this.lineMaterial = lineMaterial;
        this.lineWidth = lineWidth;
        this.gameObject.name = "line" + index.ToString();
        
        if (name!="")
        {
            this.gameObject.name = name;
        }

        this.gameObject.transform.parent = parentObject.transform;
        this.gameObject.transform.localRotation = Quaternion.identity;
        this.gameObject.transform.localScale = Vector3.one;
        if (tag!="")
        {
            this.gameObject.tag = "simplified";
        }
        LineRenderer newLine = this.gameObject.AddComponent<LineRenderer>();

        newLine.material = lineMaterial;
        newLine.startWidth = lineWidth;
        newLine.endWidth = lineWidth;
        newLine.alignment = LineAlignment.View;
        // newLine.alignment = LineAlignment.TransformZ;
        newLine.positionCount = 0;
        newLine.loop = isPeriodic; 

        lineRenderer = newLine;

        return this.gameObject;
    }

    public GameObject SimplifyCurve(GameObject initialCurve, bool IsPeriodic=false, bool simplify=true)
    {   
        isPeriodic = IsPeriodic;
        GameObject parentObject = initialCurve.transform.parent.gameObject;
        string initName = initialCurve.name;

        LineRenderer initialLineRenderer = initialCurve.GetComponent<LineRenderer>();
        Vector3[] initialPositions = new Vector3[initialLineRenderer.positionCount];
        initialLineRenderer.GetPositions(initialPositions);

        Material lineMaterial = initialLineRenderer.material;
        float lineWidth = initialLineRenderer.startWidth;

        GameObject simplifiedCurve = InstantiateCurve(0, parentObject, lineMaterial, lineWidth, IsPeriodic, initName, "simplified");
        LineRenderer simplifiedCurveRenderer = simplifiedCurve.GetComponent<LineRenderer>();
                        
        simplifiedCurveRenderer.positionCount = initialPositions.Length;
        simplifiedCurveRenderer.SetPositions(initialPositions);

        // Calculate the total length of the line
        float totalLength = CalculateTotalLength(initialPositions);
        if (simplify)
        {
            float simplifyingFactor = 0.02f;

        if (totalLength <= 0.5f)
        {
            simplifyingFactor = 0.01f;
            smoothSegSize = 0.005f;
        }
        
        simplifiedCurveRenderer.Simplify(simplifyingFactor);
        }

        Debug.Log("Hoiii2 " + isPeriodic );
        simplifiedCurveRenderer.loop = isPeriodic;
        return simplifiedCurve;
        
    }



    public (List<Vector3[]> segmentedPoints, float[] updatedSegmentLengths, List<string> usedNames) SegmentCurve(
        List<Vector3> points, float[] segmentLengths, string[] segmentNames)   
    {
        List<Vector3[]> segmentedPoints = new List<Vector3[]>();
        List<string> usedNames = new List<string>();
        float[] updatedSegmentLengths = (float[])segmentLengths.Clone();
        

        float remainingCurveLength = CalculateTotalLength(points.ToArray());
        int currentPointIndex = 0;

        for (int i = 0; i < segmentLengths.Length; i++)
        {
            if (segmentLengths[i] <= 0 )
                {continue;}
            
            if (remainingCurveLength <= 0)
                {break;}

            List<Vector3> currentSegment = new List<Vector3>();
            float remainingSegmentLength = segmentLengths[i];
            
            while (remainingSegmentLength > 0 && currentPointIndex < points.Count - 1)
            {
                float segmentPartLength = Vector3.Distance(points[currentPointIndex], points[currentPointIndex + 1]);

                if (segmentPartLength <= remainingSegmentLength)
                {
                    currentSegment.Add(points[currentPointIndex]);
                    remainingSegmentLength -= segmentPartLength;
                    remainingCurveLength -= segmentPartLength;
                    currentPointIndex++;
                }
                else
                {
                    Vector3 direction = (points[currentPointIndex + 1] - points[currentPointIndex]).normalized;
                    Vector3 splitPoint = points[currentPointIndex] + direction * remainingSegmentLength;

                    currentSegment.Add(points[currentPointIndex]);
                    currentSegment.Add(splitPoint);

                    remainingCurveLength -= remainingSegmentLength; // Corrected
                    points[currentPointIndex] = splitPoint;
                    remainingSegmentLength = 0;
                }
            }

            // Handle any remaining segment length at the end
            if (remainingSegmentLength > 0 && currentPointIndex == points.Count - 1)
            {
                currentSegment.Add(points[currentPointIndex]);
                remainingCurveLength -= remainingSegmentLength;
                remainingSegmentLength = 0;
            }

            // Save the result of the segment if any points were added
            if (currentSegment.Count > 0)
            {
                segmentedPoints.Add(currentSegment.ToArray());
                usedNames.Add(segmentNames[i]);
                updatedSegmentLengths[i] = remainingSegmentLength;
            }
        }

        return (segmentedPoints, updatedSegmentLengths, usedNames);
    }


    public LineRenderer FlattenCurve(LineRenderer lnRenderer)
    {
        Vector3[] initialPositions = new Vector3[lnRenderer.positionCount];
        lnRenderer.GetPositions(initialPositions);
        if (initialPositions.Length > 0)
        {
            Vector3[] flattenPositions = ProjectCurveOnPlane(initialPositions);
            lnRenderer.SetPositions(flattenPositions);
        }
    
        return lnRenderer;
    }

    public Vector3[] ProjectCurveOnPlane(Vector3[] points)
    {
        // 1. Get the first point, last point, and calculate the mean of all other points
        Vector3 firstPoint = points[0];
        Vector3 lastPoint = points[points.Length - 1];
        Vector3 meanPoint = CalculateMean(points);

        // 2. Define the plane using the first, last, and mean points
        projectionPlane = new Plane(firstPoint, lastPoint, meanPoint);

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

    float CalculateTotalLength(Vector3[] points)
    {
        float length = 0.0f;
        for (int i = 0; i < points.Length - 1; i++)
        {
            length += Vector3.Distance(points[i], points[i + 1]);
        }
        return length;
    }

    public void CreateControlPoints( GameObject gobject, Material material, bool periodic)
    {
    
        LineRenderer line = gobject.GetComponent<LineRenderer>();
        Vector3[] points = new Vector3[line.positionCount];
        line.GetPositions(points);

        if (periodic)
        {
            List<Vector3> pointList = new List<Vector3>(points);
            pointList.RemoveAt(pointList.Count - 1);
            points = pointList.ToArray();
        }
        // For each position, create a new GameObject and attach a CurveControlPoint component
        foreach (Vector3 position in points)
        {
            GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointObject.transform.parent = gobject.transform;
            pointObject.transform.position = position + gobject.transform.position;
            pointObject.transform.localScale = new Vector3(ctrlPtSize, ctrlPtSize , ctrlPtSize);
            pointObject.name = "ControlPoint";

            pointObject.GetComponent<Renderer>().material = material;

            ObjectManipulator targetObjectManipulator = pointObject.AddComponent<ObjectManipulator>();
            targetObjectManipulator.AllowedManipulations = TransformFlags.Move ;
            targetObjectManipulator.AllowedInteractionTypes = InteractionFlags.Near | InteractionFlags.Generic;
            
            pointObject.AddComponent<CurveControlPoint>();
            pointObject.GetComponent<Renderer>().enabled = false;
            targetObjectManipulator.enabled = false; 
        }

        hasControlPoints = true;
    }


    void GetControlPoints()
	{
		//find curved points in children
		controlPoints = this.gameObject.GetComponentsInChildren<CurveControlPoint>();

		//add positions
		controlPositions = new Vector3[controlPoints.Length];
		for( int i = 0; i < controlPoints.Length; i++ )
		{
			controlPositions[i] = controlPoints[i].transform.position;
		}
	}


    void AdjustCurve()
	{
		//create old positions if they dont match
		if( oldControlPositions.Length != controlPositions.Length )
		{
			oldControlPositions = new Vector3[controlPositions.Length];
		}

		//check if line points have moved
		bool moved = false;
		for( int i = 0; i < controlPositions.Length; i++ )
		{
			
			//compare
			if( controlPositions[i] != oldControlPositions[i] )
			{
				moved = true;
				oldControlPositions[i] = controlPositions[i];
			}
		}

		//update if moved
		if( moved == true )
		{
			Vector3[] smoothedPoints = GetInterpolatedPoints( controlPositions, 0.1f, isPeriodic);

			//set line settings
			lineRenderer.positionCount = smoothedPoints.Length;
			lineRenderer.SetPositions( smoothedPoints );
			lineRenderer.startWidth = lineWidth;
            lineRenderer.endWidth = lineWidth;

			saved = false;
		}
	}


  
    public static Vector3[] GetInterpolatedPoints(Vector3[] controlPointsList, float resolution, bool isPeriodic)
    {
        Debug.Log("Hoolla1 " + isPeriodic);
        List<Vector3> interpolatedPoints = new List<Vector3>();
        int numPoints = controlPointsList.Length;

        if (numPoints < 2)
            return controlPointsList; // Need at least two points

        int segments = isPeriodic ? numPoints : numPoints - 1;

        for (int i = 0; i < segments; i++)
        {
            Vector3 p0, p1, p2, p3;
            if (isPeriodic)
            {
                p0 = controlPointsList[(i - 1 + numPoints) % numPoints];
                p1 = controlPointsList[i];
                p2 = controlPointsList[(i + 1) % numPoints];
                p3 = controlPointsList[(i + 2) % numPoints];
            }
            else
            {
                p0 = controlPointsList[i == 0 ? i : i - 1];
                p1 = controlPointsList[i];
                p2 = controlPointsList[i + 1 < numPoints ? i + 1 : i];
                p3 = controlPointsList[i + 2 < numPoints ? i + 2 : i + 1 < numPoints ? i + 1 : i];
            }

            // Ensure positive, non-zero resolution
            resolution = Mathf.Max(resolution, 0.01f);
            int loops = Mathf.FloorToInt(1f / resolution);

            for (int j = 0; j <= loops; j++)
            {
                float t = j * resolution;
                if (t > 1f)
                    t = 1f;
                Vector3 newPos = GetCatmullRomPosition(t, p0, p1, p2, p3);
                if (interpolatedPoints.Count == 0 || interpolatedPoints[interpolatedPoints.Count - 1] != newPos)
                {
                    Debug.Log("Hoolla2 ");
                    interpolatedPoints.Add(newPos);
                }
            }
        }

        return interpolatedPoints.ToArray();
    }



    // Clamp the list positions to allow looping
    private static int ClampListPos(int pos, int max)
    {
        if (pos < 0)
        {
            pos = max - 1;
        }
        if (pos > max)
		{
			pos = 1;
		}
		else if (pos > max - 1)
		{
			pos = 0;
		}

        return pos;
    }

    // Returns a position between 4 Vector3 with Catmull-Rom spline algorithm
    // http://www.iquilezles.org/www/articles/minispline/minispline.htm
    private static Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        // The coefficients of the cubic polynomial (except the 0.5f * which is factored out)
        Vector3 a = 2f * p1;
        Vector3 b = p2 - p0;
        Vector3 c = 2f * p0 - 5f * p1 + 4f * p2 - p3;
        Vector3 d = -p0 + 3f * p1 - 3f * p2 + p3;

        // The cubic polynomial: a + b * t + c * t^2 + d * t^3
        Vector3 pos = 0.5f * (a + (b * t) + (c * t * t) + (d * t * t * t));

        return pos;
    }



}
