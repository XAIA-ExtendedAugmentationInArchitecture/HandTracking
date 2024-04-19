
using Microsoft.MixedReality.OpenXR;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using UnityEngine;
using MixedReality.Toolkit.Subsystems;
using UnityEngine.InputSystem;
using UnityEngine.XR;
using UnityEngine.XR.Hands;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine.Rendering;
using System.Linq;
using Newtonsoft.Json;


public class DirectInputAirTapDisplayer : MonoBehaviour
{

    [SerializeField]
    private GameObject leftHand;
    [SerializeField]
    private GameObject rightHand;

    [SerializeField]
    private InputActionReference leftHandReference;
    [SerializeField]
    private InputActionReference rightHandReference;
    private Boolean trackingState;
    [SerializeField]
    private InputActionReference rightHandActive;

    public string filepath = "Assets/Data/joint_poses.json";
    
    public List<Vector3> jointPosesList;

    public List<float[]> jointPosesArrList;

    public Dictionary<string, List<float[]>> JointPoseDict;
    
    private int counter=0;
    public struct JointPoseData
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    JointPoseData poseData;

    // List to store hand joint pose data

    private void Start()
    {
        JointPoseDict = new Dictionary<string, List<float[]>>();
        jointPosesList =  new List<Vector3>();
        jointPosesArrList = new List<float[]>();
        leftHand.SetActive(false);
        rightHand.SetActive(false);
        leftHandReference.action.performed += ProcessLeftHand;
        rightHandReference.action.performed += ProcessRightHand;
        rightHandReference.action.started += StartTracking;
        rightHandReference.action.canceled += StopTracking;
        rightHandActive.action.performed += GetRightHandJoint;
    }


    private void ProcessRightHand(InputAction.CallbackContext ctx)
    {
        ProcessHand(ctx, rightHand);
        //Debug.Log(ctx);
    }

    private void ProcessLeftHand(InputAction.CallbackContext ctx)
    {
        ProcessHand(ctx, leftHand);
        //Debug.Log(ctx);
    }

    private void ProcessHand(InputAction.CallbackContext ctx, GameObject g)
    {
        g.SetActive(ctx.ReadValue<float>() > 0.95f);     
    }

    private void OnDestroy()
    {
        leftHandReference.action.performed -= ProcessLeftHand;
        rightHandReference.action.performed -= ProcessRightHand;   
    }

    void StartTracking(InputAction.CallbackContext ctx)
    {
    trackingState = true;
    Debug.Log("Tracking started!");
    }

    void StopTracking(InputAction.CallbackContext ctx)
    {
    trackingState = false;
    Debug.Log("Tracking stopped!");

    counter += 1;

    //Debug.Log("bool is false" + trackingState);
    //     for(int i=0; i < jointPoses.Count; i++)
    // {   
    //     Debug.Log(string.Format("{0} {1}", i, jointPoses[i].position));
    // }

    // string listAsString = string.Join(", ", jointPoses.ToArray());

    // string dummy1 = "hello";
    // string dummy2 = "1";
    // JointPoseDict.Add(dummy1, dummy2);

    //Debug.Log(jointPosesArrList.GetType());
    JointPoseDict.Add(counter.ToString(), jointPosesArrList);
    Debug.Log("jointPosesArrList length " + jointPosesArrList.Count);
    string json = JsonConvert.SerializeObject(JointPoseDict);
    //Debug.Log(json.ToString());
    WriteToFile(json);
    }

    void WriteToFile(string json)
    {
        if (File.Exists(filepath))
        {
            File.Delete(filepath);
        }

        using(StreamWriter streamwriter = File.CreateText(filepath))
        {
            streamwriter.Write(json);
        }

        Debug.Log("We exported the file to " + filepath);
    }
    void GetRightHandJoint(InputAction.CallbackContext ctx)
    {
        Debug.Log("I am in void GetRightHandJoint!");
        //Debug.Log("InputAction = " + ctx);
        if (trackingState == false)
        {
            Debug.Log("Tracking ON!");
        }
        else
        {   
            Debug.Log("Tracking OFF!");          
            Debug.Log("Sequence number " + counter);

            var handsSubsystem_mrtk = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsSubsystem>();
            handsSubsystem_mrtk.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.RightHand, out HandJointPose pose);
            Debug.Log("Tracked hand joint pose = " + pose);
      

            if (pose != null)
            {
                poseData.position = pose.Pose.position;
                poseData.rotation = pose.Pose.rotation;
                var vecArr = new float[3]
                {
                    pose.Pose.position.x,
                    pose.Pose.position.y,
                    pose.Pose.position.z
                };
                jointPosesList.Add(pose.Pose.position);
                jointPosesArrList.Add(vecArr);
                

            }
        }
    }  
}