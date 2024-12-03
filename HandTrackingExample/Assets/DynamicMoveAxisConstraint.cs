using UnityEngine;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;


public class DynamicMoveAxisConstraint : MoveAxisConstraint
{
    private Vector3 customAxis = Vector3.zero; // The dynamic axis of movement
    private Vector3 startPosition;            // Starting position of the object
    private bool axisCalculated = false;      // Flag to ensure axis is calculated once

    public Transform otherObject;             // Reference to the other sphere

    /// <summary>
    /// Override the manipulation start logic to calculate the axis dynamically.
    /// </summary>
    public void CalculateAxis()
    {
        if (otherObject == null)
        {
            Debug.LogError("Other object reference is not set.");
            return;
        }

        // Calculate the vector from this object to the other
        customAxis = (otherObject.position - transform.position).normalized;
        axisCalculated = true;
    }

    /// <summary>
    /// Override the constraint application to limit movement to the calculated axis.
    /// </summary>
    public override void ApplyConstraint(ref MixedRealityTransform transform)
    {
        if (!axisCalculated)
        {
            CalculateAxis();
        }

        Vector3 position = transform.Position;

        // Project the movement onto the custom axis
        Vector3 projectedPosition = Vector3.Project(position - WorldPoseOnManipulationStart.Position, customAxis);

        // Constrain the position to the axis
        position = WorldPoseOnManipulationStart.Position + projectedPosition;

        transform.Position = position;
    }
}
