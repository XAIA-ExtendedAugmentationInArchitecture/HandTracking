
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
using Unity.VisualScripting;


public class DirectInputAirTapDisplayer : MonoBehaviour
{

    
    // [SerializeField]
    // private GameObject leftHand;
    // [SerializeField]
    // private GameObject rightHand;

    [SerializeField]
    private InputActionReference leftHandReference;
    [SerializeField]
    private InputActionReference rightHandReference;
    private Boolean trackingState;

    private Boolean recordState = false;

    [SerializeField]
    private InputActionReference rightHandActive;

    public string filepath = "Assets/Data/joint_poses.json";
    
    public List<Vector3> jointPosesList;

    public List<float[]> jointPosesArrList;

    // Dictionary to store hand joint pose data
    public Dictionary<string, List<float[]>> JointPoseDict;
    
    private int counter=0;

    Instantiator instantiator;
    public GameObject InstantiatorGameObject;

    private void Start()
    {
        instantiator =  InstantiatorGameObject.GetComponent<Instantiator>();
        JointPoseDict = new Dictionary<string, List<float[]>>();
        jointPosesList =  new List<Vector3>();
        
        // leftHand.SetActive(false);
        // rightHand.SetActive(false);
        leftHandReference.action.started += ProcessLeftHand;
        rightHandReference.action.started += ProcessRightHand;
        // rightHandReference.action.started += StartTracking;
        // rightHandReference.action.canceled += StopRecording;
        rightHandActive.action.performed += GetRightHandJoint;
    }


    private void ProcessRightHand(InputAction.CallbackContext ctx)
    {
        ProcessHand(ctx);
        Debug.Log("Right hand action started = " + ctx.started);
        if (recordState == false)
        {   
            recordState = true;
            Debug.Log("Record state = " + recordState);
            jointPosesArrList = new List<float[]>();
        }
        else if (recordState == true)
        {
            recordState = false;
            StopRecording();
            Debug.Log("Record state = " + recordState);
        }
    }

    private void ProcessLeftHand(InputAction.CallbackContext ctx)
    {

        ProcessHand(ctx);
        Debug.Log("Left hand action started = " + ctx.started);
        if (recordState == false)
        {   
            recordState = true;
            Debug.Log("Record state = " + recordState);
        }
        else if (recordState == true)
        {
            recordState = false;
            StopRecording();
            Debug.Log("Record state = " + recordState);
        }
    }

    private void ProcessHand(InputAction.CallbackContext ctx)
    {
        // recordState = ctx.ReadValue<float>() > 0.95f;
        
        Debug.Log(ctx.ReadValue<float>() > 0.95f ? "Air tap was registered" : "Air tap was not registered");
   
    }

    private void OnDestroy()
    {
        leftHandReference.action.performed -= ProcessLeftHand;
        rightHandReference.action.performed -= ProcessRightHand;   
    }


    void StopRecording()
    {
        Debug.Log("Recording stopped! Poses recorded = " + jointPosesList.Count);

        JointPoseDict.Add(counter.ToString(), jointPosesArrList);
        Debug.Log("Recorded joint poses " + jointPosesArrList.Count);
        string json = JsonConvert.SerializeObject(JointPoseDict);
        WriteToFile(json);

        counter ++;
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

        if (recordState == false)
        {
            Debug.Log("Recording OFF!");
        }
        else
        {   
            Debug.Log("Recording ON!");          
            Debug.Log("Recording sequence number " + counter);

            var handsSubsystem_mrtk = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsSubsystem>();
            Debug.Log("current subsystem = " + handsSubsystem_mrtk);
            handsSubsystem_mrtk.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.RightHand, out HandJointPose pose);
            
            Debug.Log("Tracked hand joint pose = " + pose);
     

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
                
                //instantiator.InstantiateSpheresRT(pose.Pose, counter.ToString());

            }
        }
    }  
}