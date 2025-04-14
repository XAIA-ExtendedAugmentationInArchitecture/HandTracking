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
                toggle.OnClicked.AddListener(() => onClicked(toggle));
                
                Debug.Log("Toggle found: " + toggle.Action);
            }
        }

    }

    void onClicked(UIPressableButton toggle)
    {
        switch (toggle.Action)
        {
            case UIPressableButton.ActionType.Curve_Open:
                SetPeriodic(false);
                break;
            case UIPressableButton.ActionType.Curve_Closed:
                SetPeriodic(true);
                break;
            case UIPressableButton.ActionType.Color_Blue:
                drawingController.lineMaterial.color = Color.blue;
                break;
            case UIPressableButton.ActionType.Color_Red:
                drawingController.lineMaterial.color = Color.red;
                break;
            case UIPressableButton.ActionType.Color_Green:
                drawingController.lineMaterial.color = Color.green;
                break;
            default:
                break;
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
                foreach (Transform child in pinParent.transform)
                {
                    child.gameObject.SetActive(true);
                }
                break;
            case UIPressableButton.ActionType.Session_Space:
                //untoggle all other buttons
                foreach (var t in UItoggles)
                {
                    if (t != toggle && t.IsToggled)
                    {
                        t.ForceSetToggled(false);
                    }
                }
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

        public void SetPeriodic(bool active)
    {
        drawingController.newPeriodic = active;

    }



}
