using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MixedReality.Toolkit.UX;

public class UIController : MonoBehaviour
{
    public MarkerLocalizer markerLocalizer;
    public DrawingController drawController;
    public PressableButton Localize;
    public PressableButton NewDrawing;
    public PressableButton SaveDrawing;
    public PressableButton VisualizeDrawing;
    public PressableButton NextDrawing;
    public PressableButton SendToRhino;
    public PressableButton VisualizeMesh;
    public PressableButton DrawingMode;
    public TextMeshPro DrawingText;
    public TextMeshPro ModeText;
    public TextMeshPro TrackingText;
    public GameObject TableMenu;
    
    // Start is called before the first frame update
    void Start()
    {
        TableMenu.SetActive(false);
        Localize.OnClicked.AddListener(() => markerLocalizer.ToggleLocalization());
        NewDrawing.OnClicked.AddListener(() => drawController.StartNewDrawing());
        NextDrawing.OnClicked.AddListener(() => drawController.VisualizeDrawing());
        SaveDrawing.OnClicked.AddListener(() => drawController.SaveEnabledDrawing());

        GameObject next = NextDrawing.gameObject.transform.parent.gameObject;
        VisualizeDrawing.OnClicked.AddListener(() => ToggleVisibility(next));
        SendToRhino.OnClicked.AddListener(() => drawController.SendToRhino());

        
        VisualizeMesh.OnClicked.AddListener(() => ToggleRendererVisibility());
        DrawingMode.OnClicked.AddListener(() => drawController.ToggleDrawingMode());
    }

    void ToggleVisibility(GameObject gameObject)
    {
        if (gameObject != null)
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
        else
        {
            Debug.Log("The requested gameobject is null");
        }
    }

    void ToggleRendererVisibility()
    {
        MeshRenderer mRenderer= drawController.meshCollider.gameObject.GetComponent<MeshRenderer>();
        mRenderer.enabled = !mRenderer.enabled;
        
    }

}
