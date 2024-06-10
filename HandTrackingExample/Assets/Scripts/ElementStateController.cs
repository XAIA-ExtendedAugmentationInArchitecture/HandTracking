using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MixedReality.Toolkit;
using MixedReality.Toolkit.UX;
using TMPro;
using MixedReality.Toolkit.SpatialManipulation;

public class ElementStateController : MonoBehaviour
{
    private DrawingController drawController;
    public GameObject target;
    private ObjectManipulator targetObjectManipulator;
    private StatefulInteractable targetStatefulInteractable;
    private MeshCollider targetCollider;
    private PressableButton pressableButton; 

    private GameObject closed;
    private GameObject open;

    private bool toggled = true;

    void Start()
    {
        drawController = GameObject.Find("DrawingController").GetComponent<DrawingController>();
        pressableButton = GetComponent<PressableButton>();
        if (pressableButton != null)
        {
            pressableButton.OnClicked.AddListener(() => ToggleState());
        }
        closed = gameObject.FindObject("closed");
        open = gameObject.FindObject("open");
    }

    public void ToggleState()
    {
        toggled = !toggled;

        if (toggled)
        {
             DrawingState();
        }
        else
        {
            ManipulateState();
        }
    }


    public void DrawingState()
    {
        if (target!=null)
        {
            targetObjectManipulator = target.GetComponent<ObjectManipulator>();

            if (targetObjectManipulator != null)
            {
                Destroy(targetObjectManipulator);
            }

            targetCollider = target.GetComponent<MeshCollider>();

            if (targetCollider != null)
            {
                Destroy(targetCollider);
            }

            target.AddComponent<MeshCollider>();

            targetStatefulInteractable = target.AddComponent<StatefulInteractable>();
            targetStatefulInteractable.ToggleMode = StatefulInteractable.ToggleType.Toggle;
            targetStatefulInteractable.OnToggled.AddListener(() => drawController.StartDrawing());
            targetStatefulInteractable.OnUntoggled.AddListener(() => drawController.StopDrawing());
 
        }

        closed.SetActive(true);
        open.SetActive(false);

        Debug.Log("Mode: Drawing");
    }

    public void ManipulateState()
    {
        if (target!=null)
        {
            targetStatefulInteractable = target.GetComponent<StatefulInteractable>();

            if (targetStatefulInteractable != null)
            {
                Destroy(targetStatefulInteractable);
            }

            targetCollider = target.GetComponent<MeshCollider>();

            if (targetCollider != null)
            {
                Destroy(targetCollider);
            }

            target.AddComponent<MeshCollider>();

            targetObjectManipulator = target.AddComponent<ObjectManipulator>();
            targetObjectManipulator.AllowedManipulations = TransformFlags.Move | TransformFlags.Rotate;
            targetObjectManipulator.AllowedInteractionTypes = InteractionFlags.Near | InteractionFlags.Ray | InteractionFlags.Gaze | InteractionFlags.Generic; 

        }

        closed.SetActive(false);
        open.SetActive(true);

        Debug.Log("Mode:Manipulating");
    }
}
