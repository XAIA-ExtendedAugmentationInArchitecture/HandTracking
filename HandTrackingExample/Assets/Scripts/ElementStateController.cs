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
    private Renderer objectRenderer;

    // Specific RGB values for the toggled and untoggled states
    private Color drawingColor = new Color(176f / 255f, 29f / 255f, 35f / 255f); 
    private Color manipulateColor = new Color(47f / 255f, 106f / 255f, 56f / 255f); 

    private bool toggled = true;

    void Start()
    {
        drawController = GameObject.Find("DrawingController").GetComponent<DrawingController>();
        pressableButton = GetComponent<PressableButton>();
        if (pressableButton != null)
        {
            pressableButton.OnClicked.AddListener(() => ToggleState());
        }
        objectRenderer = GetComponent<Renderer>();
        objectRenderer.material.color = drawingColor;
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

        if (objectRenderer != null)
        {
            objectRenderer.material.color = drawingColor;
        }
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

        if (objectRenderer != null)
        {
            objectRenderer.material.color = manipulateColor;
        }

        Debug.Log("Mode:Manipulating");
    }
}
