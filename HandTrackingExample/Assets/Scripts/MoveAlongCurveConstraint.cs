using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using UnityEngine;

public class MoveAlongCurveConstraint : TransformConstraint
{
    private LineRenderer lineRenderer;
    public float parameter = 0.0f;
    public int closestIndex = 0;

    public override TransformFlags ConstraintType => TransformFlags.Move;

    public void Initialize(LineRenderer targetLineRenderer)
    {
        lineRenderer = targetLineRenderer;
    }

    public override void ApplyConstraint(ref MixedRealityTransform transform)
    {
        if (lineRenderer == null)
        {
            Debug.LogWarning("LineRenderer is not assigned.");
            return;
        }

        // // Project the manipulated position onto the curve
        // transform.Position = GetClosestPointOnCurve(transform.Position);

        // Find the closest vertex index
        closestIndex = GetClosestVertexIndex(transform.Position);

        // Snap to the closest vertex
        if (closestIndex != -1)
        {
            transform.Position = lineRenderer.GetPosition(closestIndex);
            // Calculate parameter (0 to 1) based on the closest index
            parameter = (float)closestIndex / (lineRenderer.positionCount - 1);
        }
    }

    private Vector3 GetClosestPointOnCurve(Vector3 position)
    {
        float closestDistance = float.MaxValue;
        Vector3 closestPoint = Vector3.zero;

        // Loop through all segments of the LineRenderer
        for (int i = 0; i < lineRenderer.positionCount - 1; i++)
        {
            Vector3 start = lineRenderer.GetPosition(i);
            Vector3 end = lineRenderer.GetPosition(i + 1);
            Vector3 pointOnSegment = GetPointOnSegment(position, start, end);

            float distance = Vector3.Distance(position, pointOnSegment);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = pointOnSegment;
            }
        }

        return closestPoint;
    }

    private Vector3 GetPointOnSegment(Vector3 point, Vector3 segmentStart, Vector3 segmentEnd)
    {
        Vector3 segment = segmentEnd - segmentStart;
        float t = Mathf.Clamp01(Vector3.Dot(point - segmentStart, segment) / segment.sqrMagnitude);
        return segmentStart + t * segment;
    }

    private int GetClosestVertexIndex(Vector3 position)
    {
        int closestIndex = -1;
        float closestDistance = float.MaxValue;

        // Loop through all vertices of the LineRenderer
        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            Vector3 vertexPosition = lineRenderer.GetPosition(i);
            float distance = Vector3.Distance(position, vertexPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        return closestIndex;
    }
}
