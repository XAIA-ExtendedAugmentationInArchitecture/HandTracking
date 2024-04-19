using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using UnityEngine.XR.OpenXR.Input;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit.SpatialManipulation;
using MixedReality.Toolkit.Input;


public class DataManagementDM : MonoBehaviour
{   
    public GameObject indexTipObject;

    XRHandSubsystem m_handsSubsystems;

    // Data structure to store hand joint pose information
    struct JointPoseData
    {
        public TrackedHandJoint jointType;
        public Vector3 position;
        public Quaternion rotation;

    }

    // List to store hand joint pose data
    List<JointPoseData> jointPoses = new List<JointPoseData>();

    void Awake()
    {
        //var aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();
        var handsSubsystem_mrtk = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsSubsystem>();
        //Debug.Log("print subsystem");
        Debug.Log("First running subsystem = " + handsSubsystem_mrtk);
        if (handsSubsystem_mrtk != null)
        {
            // Read the capability information off the implementation's descriptor.
            bool isPhysicalData = handsSubsystem_mrtk.subsystemDescriptor.IsPhysicalData;
            Debug.Log("Is physical data = " + isPhysicalData);
            var txt = handsSubsystem_mrtk.ToString();
            Debug.Log("Current subsystem = " + txt);
        }

        var handSubsystems = new List<XRHandSubsystem>();

        SubsystemManager.GetSubsystems(handSubsystems);
        Debug.Log("List HandSubsystem = " + handSubsystems);

        foreach (var handSubsystem in handSubsystems)
        {
            if (handSubsystem.running)
            {
                m_handsSubsystems = handSubsystem;
                break;
            }
        }

        if (handsSubsystem_mrtk != null)
        {
            Debug.Log("handsSubsystem_mrtk started");
            //handsSubsystem_mrtk. += OnUpdatedHandDM;
            handsSubsystem_mrtk.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.RightHand, out HandJointPose pose);
            //Debug.Log("tracked joint pose = " + pose);
        }

        // if (m_handsSubsystems != null)
        // {
        //     Debug.Log("m_handsSubsystems is running");
        //     m_handsSubsystems.updatedHands += OnUpdatedHandDM;
        // }

    }

    void Update()
    {
        if (indexTipObject != null)
            indexTipObject.GetComponent<Renderer>().enabled = false;
    }

    void OnUpdatedHandDM(XRHandSubsystem subsystem,
        XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
        XRHandSubsystem.UpdateType updateType)

    {
        Debug.Log("Update Success Flags = " + updateSuccessFlags); 
        XRHand LH = subsystem.leftHand;
        XRHand RH = subsystem.rightHand;

        for(var i = XRHandJointID.BeginMarker.ToIndex();
        i < XRHandJointID.EndMarker.ToIndex(); 
        i++)
        {
            var trackingData = LH.GetJoint(XRHandJointIDUtility.FromIndex(i));

            if (trackingData.TryGetPose(out UnityEngine.Pose pose))
            {
                Debug.Log("Tracking Data, Pose = " + pose);
            }
        }

        // foreach (var joint in joints )
        // {
        //     // Store joint pose data
        //     JointPoseData poseData;
        //     poseData.jointType = joint.Key;
        //     poseData.position = joint.Value.pose.position;
        //     poseData.rotation = joint.Value.pose.rotation;
        //     jointPoses.Add(poseData);
        //     Debug.Log(jointPoses);

        //     if (joint.Key == TrackedHandJoint.IndexTip && indexTipObject != null)
        //     {
        //         indexTipObject.GetComponent<Renderer>().enabled = true;
        //         indexTipObject.transform.position = joint.Value.pose.position;
        //         indexTipObject.transform.rotation = joint.Value.pose.rotation;

        //         // Print joint pose data
        //         Debug.Log("Joint Type: " + poseData.jointType.ToString() + 
        //             ", Position: " + poseData.position.ToString() + 
        //             ", Rotation: " + poseData.rotation.eulerAngles.ToString());
        //     }
        //     }
        }
    }

