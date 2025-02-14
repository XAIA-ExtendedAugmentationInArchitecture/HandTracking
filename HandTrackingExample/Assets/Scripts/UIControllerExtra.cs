using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.SpatialManipulation;

public class UIControllerExtra : MonoBehaviour
{
    public List<UIPressableButton> UItoggles = new List<UIPressableButton>();

    public DrawingController drawingController;

    public GameObject pinParent;
    public GameObject DrawingSettings;
    public GameObject DrawingModeSettings;
    public GameObject ExtraSettings;


    void Start()
    {
        if (UItoggles == null || UItoggles.Count == 0)
        {
            Debug.LogError("No toggles found in UIControllerExtra.");
        }
        else
        {
            foreach (var toggle in UItoggles)
            {
                toggle.OnToggled.AddListener(() => onToggled(toggle));
                toggle.OnUntoggled.AddListener(() => onUntoggled(toggle));
                
                Debug.Log("Toggle found: " + toggle.Action);
            }
        }
    }

    void onToggled(UIPressableButton toggle)
    {
        switch (toggle.Action)
        {
            case UIPressableButton.ActionType.Drawing_Freehand:
                ExtraSettings.SetActive(true);
                drawingController.ModeFreehand();
                pinParent.SetActive(false);
                break;
            case UIPressableButton.ActionType.Drawing_OnObject:
                ExtraSettings.SetActive(true);
                drawingController.ModeDrawOnObject();
                pinParent.SetActive(false);
                break;
            case UIPressableButton.ActionType.ControlPoints:
                ExtraSettings.SetActive(false);
                drawingController.ModeControlPoints();
                pinParent.SetActive(false);
                break;
            case UIPressableButton.ActionType.AR_Pins:
                ExtraSettings.SetActive(false);
                drawingController.ModePinPoints();
                pinParent.SetActive(true);
                break;
            default:
                break;
        }
    }

    void onUntoggled(UIPressableButton toggle)
    {
        switch (toggle.Action)
        {
            case UIPressableButton.ActionType.Drawing_Freehand:
                ExtraSettings.SetActive(false);
                drawingController.ModeEditing();
                break;
            case UIPressableButton.ActionType.Drawing_OnObject:
                ExtraSettings.SetActive(false);
                drawingController.ModeEditing();
                break;
            case UIPressableButton.ActionType.ControlPoints:
                drawingController.ModeEditing();
                break;
            case UIPressableButton.ActionType.AR_Pins:
                drawingController.ModeEditing();
                pinParent.SetActive(false);
                break;
            default:
                break;
        }
    }



}
