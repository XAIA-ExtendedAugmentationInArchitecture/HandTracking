using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Microsoft;
using MixedReality.Toolkit.Subsystems;
using UnityEngine.XR;
using MixedReality.Toolkit;
using MixedReality.Toolkit.Input;
using UnityEngine.XR.Hands;

public class datamanagment : MonoBehaviour
{   
        public GameObject sphereMarker;

        GameObject indexTipObject;
        HandJointPose pose;
        HandsSubsystem handsSubsystemsHL;
        XRHandSubsystem m_handsSubsystems; 


    // Start is called before the first frame update
void Start()
    {
        var handSubsystems = new List<XRHandSubsystem>();
        SubsystemManager.GetSubsystems(handSubsystems);
        for (var i = 0; i < handSubsystems.Count; ++i)
        {
            var handSubsystem = handSubsystems[i];
            if (handSubsystem.running)
            {
                m_handsSubsystems = handSubsystem;
                break;
            }
        }
        if (m_handsSubsystems != null)
            m_handsSubsystems.updatedHands += OnUpdatedHands;
    }

    // Update is called once per frame
    void Update()
    {
        indexTipObject.GetComponent<Renderer>().enabled = false;
        print (m_handsSubsystems);

        // if (handsSubsystemsHL.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.RightHand, out pose))
        // {
        //     indexTipObject.GetComponent<Renderer>().enabled = true;
        //     indexTipObject.transform.position = pose.Position;
        //     indexTipObject.transform.rotation = pose.Rotation;
        //     Debug.Log(pose);
        // }
        
    }

    void OnUpdatedHands(XRHandSubsystem subsystem,
        XRHandSubsystem.UpdateSuccessFlags updateSuccessFlags,
        XRHandSubsystem.UpdateType updateType)
    {
        switch (updateType)
        {
            case XRHandSubsystem.UpdateType.Dynamic:
                // Update game logic that uses hand data
                break;
            case XRHandSubsystem.UpdateType.BeforeRender:
                // Update visual objects that use hand data
                break;
        }
    }
}
