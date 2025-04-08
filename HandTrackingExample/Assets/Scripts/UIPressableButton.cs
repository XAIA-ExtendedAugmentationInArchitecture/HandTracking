using UnityEngine;
using MixedReality.Toolkit.UX;

public class UIPressableButton : PressableButton
{

    public enum ActionType
    {
         None,

        // Basic Menu
        Session_Space, Actions, CADLink, Inventory,
        // Drawing Actions
        Drawing_Freehand, Drawing_OnObject, ControlPoints,

        // AR Interaction
        AR_Pins, Localize,

        // Curves
        Curve_Open, Curve_Closed,

        // Colors
        Color_Red, Color_Blue, Color_Green,

        //CADLinks
        SaveDrawing, SendDrawing,

        //FileSettings
        NewDrawing, PreviousDrawing,

        //Scale
        ScaleParent, FreehandScale, NextScale, PreviousScale

  
    }

    [SerializeField] // This makes it visible in the Inspector
    private ActionType actionType;

    // Property for external access (optional)
    public ActionType Action
    {
        get => actionType;
        set => actionType = value;
    }
}
