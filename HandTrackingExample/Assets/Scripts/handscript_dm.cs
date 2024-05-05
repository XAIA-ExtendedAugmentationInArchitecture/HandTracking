using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using System.Linq;
using static UnityEditor.Experimental.AssetDatabaseExperimental.AssetDatabaseCounters;
using UnityEngine.XR.OpenXR.Input;



public class handscript_dm : MonoBehaviour
{    // Start is called before the first frame update
     // Gets the first valid implementation of T that is started and running.
     // Reference to the hands aggregator
    HandsAggregatorSubsystem aggregator;
    public List<Vector3> jointPosesList;
    public List<float[]> jointPosesArrList;
    private int counter = 0;
    Instantiator instantiator;
    public GameObject InstantiatorGameObject;
    // Dictionary to store hand joint pose data
    public Dictionary<string, List<float[]>> JointPoseDict;

    void Start()
    {
        instantiator = InstantiatorGameObject.GetComponent<Instantiator>();
        JointPoseDict = new Dictionary<string, List<float[]>>();
        jointPosesList = new List<Vector3>();
        jointPosesArrList = new List<float[]>();

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
            bool handIsValidPinch = aggregator.TryGetPinchProgress(XRNode.RightHand, out bool isReadyToPinch, out bool isPinching, out float pinchAmount);
            Debug.Log("The pinch is" + isPinching);

            if (isPinching)
            {
                PlaceSpheres(jointPose);
                Debug.Log("yeeahh");
            }
        }
    }

    IEnumerator EnableWhenSubsystemAvailable()
    {
        // Wait until the HandsAggregatorSubsystem is available
        yield return new WaitUntil(() => aggregator != null);

        // Once available, you can access its properties
        string isPhysicalData = aggregator.subsystemDescriptor.id;
    }

    void PlaceSpheres(HandJointPose pose)
    {
        if (pose != null)
        {
            var vecArr = new float[3]
            {
                    pose.Pose.position.x,
                    pose.Pose.position.y,
                    pose.Pose.position.z
            };
            jointPosesList.Add(pose.Pose.position);
            jointPosesArrList.Add(vecArr);

            instantiator.InstantiateSpheresRT(pose.Pose, counter.ToString());

        }
    }
}
