using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;



public class handscript_dm : MonoBehaviour
{    // Start is called before the first frame update
     // Gets the first valid implementation of T that is started and running.
     // Reference to the hands aggregator
    HandsAggregatorSubsystem aggregator;

    void Start()
    {
        // Gets the first valid implementation of T that is started and running.
        aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();

        if (aggregator != null)
        {
            // If the aggregator is available, enable functionality
            StartCoroutine(EnableWhenSubsystemAvailable());
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (aggregator != null)
        {
            // Get a single joint (Index tip, on left hand, for example)
            bool jointIsValid = aggregator.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.RightHand, out HandJointPose jointPose);

            // Check whether the user's left hand is facing away (commonly used to check "aim" intent)
            bool handIsValid = aggregator.TryGetPalmFacingAway(XRNode.LeftHand, out bool isLeftPalmFacingAway);
            Debug.Log("The pose of the joint is" + jointIsValid + jointPose);

            // Query pinch characteristics from the left hand.
            bool handIsValidPinch = aggregator.TryGetPinchProgress(XRNode.LeftHand, out bool isReadyToPinch, out bool isPinching, out float pinchAmount);
            Debug.Log("The pinch is" + jointIsValid + jointPose);
        }
    }

    IEnumerator EnableWhenSubsystemAvailable()
    {
        // Wait until the HandsAggregatorSubsystem is available
        yield return new WaitUntil(() => aggregator != null);

        // Once available, you can access its properties
        string isPhysicalData = aggregator.subsystemDescriptor.id;
    }
}
