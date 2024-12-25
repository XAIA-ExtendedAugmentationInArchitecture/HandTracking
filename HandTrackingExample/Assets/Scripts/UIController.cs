using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MixedReality.Toolkit;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.SpatialManipulation;

public class UIController : MonoBehaviour
{
    public MarkerLocalizer markerLocalizer;
    public DrawingController drawController;
    public MqttController mqttController;
    public MeshGeneratorFromJson meshGenerator;
    public BendGeometry benGeo;
    public PressableButton Localize;
    public PressableButton NewDrawing;
    //public PressableButton Undo;
    public PressableButton SaveDrawing;
    public PressableButton VisualizeDrawing;
    public PressableButton NextDrawing;
    public PressableButton SendToRhino;
    public PressableButton SendPriority;
    public PressableButton PeriodicButton;
    public GameObject Periodic;
    public TMP_Text PeriodicInfo;

    public PressableButton SelectToDelete;
    public PressableButton NextToDelete;
    public PressableButton Delete;
    public PressableButton CopyLine;

    public PressableButton VisualizeMesh;

    public PressableButton ScaleButton;
    public GameObject ScaleParent;
    public PressableButton RealScaleButton;
    public PressableButton NextScale;
    public PressableButton PrevScale;
    public TMP_Text ScaleInfo;

    public PressableButton Locks;
    public GameObject TransparencyParent;
    public PressableButton TrasparencyUp;
    public PressableButton TrasparencyDown;
    public TMP_Text TransparencyInfo;

    public PressableButton DrawingMode;

    public PressableButton RedColor;
    public PressableButton GreenColor;
    public PressableButton BlueColor;

    public Material LineBlue;
    public Material LineGreen;
    public Material LineRed;

    public GameObject mqttReceivedDialog;

    public GameObject mqttPriorityDialog;
    public TextMeshProUGUI mqttPriorityDialogBody;

    public GameObject mqttSendDialog;
    public TextMeshProUGUI mqttSendDialogHeader;
    public TextMeshProUGUI mqttSendDialogBody;
    public TextMeshProUGUI mqttReceivedDialogBody;
    public GameObject mqttConnectionDialog;

    public GameObject PickTeam;

    public GameObject StartDialog;
    public GameObject HandMenu;

    public TextMeshPro DrawingText;
    public TextMeshPro ModeText;
    public TextMeshPro TrackingText; 
    public GameObject TableMenu;

    public PressableButton InventoryButton;
    public GameObject Submenu;
    public PressableButton nextPart;
    public PressableButton previousPart;
    public PressableButton DirectionToggle;
    public Slider bendSlider;
    public PressableButton nextGeo;
    public PressableButton previousGeo;
    public PressableButton ManipulatorToggle;

    public PressableButton PriorityMapper;
    private int ind = 0;

    private float[] scales = { 0.5f, 0.2f, 0.1f, 0.05f, 0.02f, 0.01f, 0.004f, 0.002f, 0.001f };
    private int currentScaleIndex = 0;

    private Dictionary<string, List<(List<(string pointName, bool direction, List<List<float>> intervals)>, float parameter)>> priority;

    
    // Start is called before the first frame update
    void Start()
    {   
        Submenu.SetActive(false);
        InventoryButton.OnClicked.AddListener(() =>  ToggleVisibility(Submenu));
        InventoryButton.OnClicked.AddListener(() => drawController.ActivateStandBy());
        nextGeo.OnClicked.AddListener(ShowNextGeo);
        previousGeo.OnClicked.AddListener(ShowPreviousGeo);
        nextPart.OnClicked.AddListener(OnNextPart);
        previousPart.OnClicked.AddListener(OnPreviousPart);
        ManipulatorToggle.OnClicked.AddListener(() =>  ToggleObjectManipulator(ManipulatorToggle.IsToggled));

        TableMenu.SetActive(false);

        Localize.OnClicked.AddListener(() => markerLocalizer.EnableLocalization());

        NewDrawing.OnClicked.AddListener(() => drawController.StartNewDrawing());
        //Undo.OnClicked.AddListener(() =>drawController.UndoLine());

        NextDrawing.OnClicked.AddListener(() => drawController.VisualizeDrawing());
        SaveDrawing.OnClicked.AddListener(() => drawController.SaveEnabledDrawing());

        GameObject next = NextDrawing.gameObject.transform.parent.gameObject;
        VisualizeDrawing.OnClicked.AddListener(() => ToggleVisibility(next));
        SendToRhino.OnClicked.AddListener(() =>
        {
            mqttSendDialogBody.text = "Do you want to send the drawing no." + drawController.EnabledDrawingIndex.ToString() + " back to Rhino?";
            SetupSendToRhinoDialog();
        });

        VisualizeDrawing.OnClicked.AddListener(() => ToggleVisibility(next));

        SendPriority.OnClicked.AddListener(() =>
        {
            mqttPriorityDialogBody.text = "Do you want to send the mapping to Rhino?";
            SetupSendPriorityDialog();
        });  

        Periodic.SetActive(false);
        PeriodicButton.OnClicked.AddListener(() => TogglePeriodic());
        PeriodicInfo.text = "Open";

        SelectToDelete.OnClicked.AddListener(() => drawController.EnableDeleteLineState());
        SelectToDelete.OnClicked.AddListener(() => drawController.ActivateStandBy());
        SelectToDelete.OnClicked.AddListener(() => ToggleVisibility(NextToDelete.gameObject.transform.parent.gameObject));
        SelectToDelete.OnClicked.AddListener(() => ToggleVisibility(Delete.gameObject.transform.parent.gameObject));
        SelectToDelete.OnClicked.AddListener(() => ToggleVisibility(CopyLine.gameObject.transform.parent.gameObject));
        NextToDelete.OnClicked.AddListener(() => drawController.SelectLine());
        Delete.OnClicked.AddListener(() => drawController.DeleteLine());
        CopyLine.OnClicked.AddListener(() => drawController.CopySelectedLine());

        Locks.OnClicked.AddListener(() => meshGenerator.locksParent.SetActive(!meshGenerator.locksParent.activeSelf));

        ScaleParent.SetActive(false);
        ScaleButton.OnClicked.AddListener(() => ScaleParent.SetActive(!ScaleParent.activeSelf));
        ScaleButton.OnClicked.AddListener(() => drawController.ToggleScale());
        ScaleButton.OnClicked.AddListener(() => drawController.ActivateStandBy());
        RealScaleButton.OnClicked.AddListener(() => drawController.currentDrawingParent.transform.localScale = Vector3.one);
        RealScaleButton.OnClicked.AddListener(() =>  meshGenerator.inventoryParent.transform.localScale = Vector3.one);
        
        NextScale.OnClicked.AddListener(OnNextScalePressed);
        PrevScale.OnClicked.AddListener(OnPrevScalePressed);


        TransparencyParent.SetActive(false);

        VisualizeMesh.OnClicked.AddListener(() => TransparencyParent.SetActive(!TransparencyParent.activeSelf));
        TrasparencyUp.OnClicked.AddListener(() => meshGenerator.AdjustTransparency(true, TransparencyInfo));
        TrasparencyDown.OnClicked.AddListener(() => meshGenerator.AdjustTransparency(false, TransparencyInfo));

        DrawingMode.OnClicked.AddListener(() => drawController.ToggleDrawingMode());

        GreenColor.OnClicked.AddListener(() => drawController.lineMaterial = LineGreen);
        RedColor.OnClicked.AddListener(() => drawController.lineMaterial = LineRed);
        BlueColor.OnClicked.AddListener(() => drawController.lineMaterial = LineBlue);

        mqttReceivedDialog.SetActive(false);
        mqttReceivedDialog.GetComponent<Dialog>().SetPositive("Yes", args => meshGenerator.Generate(mqttController.msgData, meshGenerator.elementsParent));
        mqttReceivedDialog.GetComponent<Dialog>().SetNegative("No", args => Debug.Log("Rejected"));

        mqttConnectionDialog.SetActive(false);
        mqttConnectionDialog.GetComponent<Dialog>().SetNeutral("OK", args => mqttController.Connect());

        mqttSendDialog.SetActive(false);
        mqttSendDialog.GetComponent<Dialog>().SetPositive("Yes", args => drawController.SendToRhino());
        mqttSendDialog.GetComponent<Dialog>().SetNegative("No", args => Debug.Log("Rejected"));

        mqttPriorityDialog.SetActive(false);
        mqttPriorityDialog.GetComponent<Dialog>().SetPositive("Yes", args => drawController.SendPriorityData(priority));
        mqttPriorityDialog.GetComponent<Dialog>().SetNegative("No", args => Debug.Log("Rejected"));

        PickTeam.SetActive(true);
        PickTeam.GetComponent<Dialog>().SetPositive("Team A", args => DefineMqttTopics("A"));
        PickTeam.GetComponent<Dialog>().SetNegative("Team B", args => DefineMqttTopics("B"));

        StartDialog.SetActive(false);
        HandMenu.SetActive(false);
        StartDialog.GetComponent<Dialog>().SetNeutral("OK", args => Debug.Log("OK"));

    }

    void Update()
    {
        if (ScaleParent.activeSelf)
        {
            float scaleValue = drawController.currentDrawingParent.transform.localScale.x;
            meshGenerator.inventoryParent.transform.localScale = Vector3.one * scaleValue;
            if (scaleValue > 0.1f)
            {
                ScaleInfo.text = "1:" + (Mathf.Round((1 / scaleValue) * 10f) / 10f).ToString(); 
            }
            else
            {
              ScaleInfo.text = "1:" + Mathf.RoundToInt(1/ scaleValue).ToString();  
            }
            // ScaleInfo.text = (scaleValue*100).ToString() + "%";
            // ScaleInfo.text = "1:" + Mathf.RoundToInt(1/ scaleValue).ToString();
        }
    }

    void TogglePeriodic()
    {
        drawController.newPeriodic = !drawController.newPeriodic;
        if (drawController.newPeriodic)
        {
            PeriodicInfo.text = "Periodic";
        }
        else
        {
            PeriodicInfo.text = "Open";
        }
    }

    void SetupSendToRhinoDialog()
    {
        var dialog = mqttSendDialog.GetComponent<Dialog>();
        dialog.Reset();

        dialog.SetPositive("Yes", args => drawController.SendToRhino());
        dialog.SetNegative("No", args => Debug.Log("Rejected"));

        mqttSendDialog.SetActive(true);
    }

    void SetupSendPriorityDialog()
    {
        var dialog = mqttPriorityDialog.GetComponent<Dialog>();
        dialog.Reset();
        dialog.SetPositive("Yes", args => drawController.SendPriorityData(priority));
        dialog.SetNegative("No", args => Debug.Log("Rejected"));

        mqttPriorityDialog.SetActive(true);
    }

    void DefineMqttTopics(string team)
    {
        if (team=="A")
        {
            mqttController.topicsSubscribe.Add("/kitgkr_teamA_geometries/");
            mqttController.topicsSubscribe.Add("/kitgkr_teamA_geometry/");
            mqttController.topicsSubscribe.Add("/kitgkr_teamA_lines/");
            mqttController.topicsPublish.Add("/kitgkr_teamA_drawings/");
            mqttController.topicsPublish.Add("/kitgkr_teamA_priority/");
            drawController.team = "teamA";
            mqttSendDialogHeader.text = "Team A: Send a Drawing to Rhino";
        }
        else if (team=="B")
        {
            mqttController.topicsSubscribe.Add("/kitgkr_teamB_geometries/");
            mqttController.topicsSubscribe.Add("/kitgkr_teamB_geometry/");
            mqttController.topicsSubscribe.Add("/kitgkr_teamB_lines/");
            mqttController.topicsPublish.Add("/kitgkr_teamB_drawings/");
            mqttController.topicsPublish.Add("/kitgkr_teamB_priority/");
            drawController.team = "teamB";
            mqttSendDialogHeader.text = "Team B: Send a Drawing to Rhino";
        }
        mqttController.subscribeTopics();
        StartDialog.SetActive(true);
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
        meshGenerator.locksParent.SetActive(!meshGenerator.locksParent.activeSelf);
        foreach (Transform child in meshGenerator.elementsParent.transform)
        {
            MeshRenderer mRenderer= child.gameObject.GetComponent<MeshRenderer>();
            mRenderer.enabled = !mRenderer.enabled;
        }    
    }

    public void MessageReceived(string topic, string object_name)
    {
        var dialog = mqttReceivedDialog.GetComponent<Dialog>();
        dialog.Reset();
        dialog.GetComponent<Dialog>().SetNegative("No", args => Debug.Log("Rejected"));
        mqttReceivedDialog.GetComponent<Dialog>().SetBody("Geometry: " + object_name + "\n Do you want to add this geometry in your digital space?");
        mqttReceivedDialogBody.text = "Geometry: " + object_name + "\n Do you want to add this geometry in your digital space?";
        mqttReceivedDialog.SetActive(true);
        if (topic == "/kitgkr_teamA_geometry/" || topic == "/kitgkr_teamB_geometry/")
        {
            dialog.SetPositive("Yes", args => meshGenerator.Generate(mqttController.msgData, meshGenerator.elementsParent));
        }
        else if (topic == "/kitgkr_teamA_lines/" || topic == "/kitgkr_teamB_lines/")
        {
            dialog.SetPositive("Yes", args => drawController.GenerateNewDrawings(mqttController.msgDataLines));
        }
        else if (topic == "/kitgkr_teamA_geometries/" || topic == "/kitgkr_teamB_geometries/")
        {
            dialog.SetPositive("Yes", args => meshGenerator.GenerateMultiple(mqttController.msgDataMeshes, meshGenerator.elementsParent));
        }
        
    }
    public void ActivateConnectionDialog(string connectionState)
    {
        if (connectionState == "lost")
        {
            Debug.Log("CONNECTION LOST!");
            mqttConnectionDialog.GetComponent<Dialog>().SetHeader("MQTT CONNECTION IS LOST");
        }
        else if (connectionState == "disconnected")
        {
            Debug.Log("Disconnected!");
            mqttConnectionDialog.GetComponent<Dialog>().SetHeader("MQTT DISCONNECTED");
        }
        else if (connectionState == "failed")
        {
            Debug.Log("Disconnected!");
            mqttConnectionDialog.GetComponent<Dialog>().SetHeader("MQTT CONNECTION FAILED");
        }
        mqttConnectionDialog.GetComponent<Dialog>().SetBody("Do you want to try to reconnect?");
        mqttConnectionDialog.SetActive(true);
    }

    void OnNextPart()
    {
        benGeo.ChangeIndex(true, bendSlider, DirectionToggle); // Forward navigation
    }

    void OnPreviousPart()
    {
        benGeo.ChangeIndex(false, bendSlider, DirectionToggle); // Backward navigation
    }

    void ShowNextGeo()
    {
        if (meshGenerator.inventoryParent.transform.childCount == 0) return;

        // Increment index and wrap around if necessary
        ind = (ind + 1) % meshGenerator.inventoryParent.transform.childCount;
        SetActiveGeo(ind);

    }

    void ShowPreviousGeo()
    {
        if (meshGenerator.inventoryParent.transform.childCount == 0) return;

        // Decrement index and wrap around if necessary
        ind = (ind - 1 + meshGenerator.inventoryParent.transform.childCount) % meshGenerator.inventoryParent.transform.childCount;
        SetActiveGeo(ind);
    }

    void SetActiveGeo(int index)
    {
        if ( benGeo!= null)
        {
            benGeo.RecolorPlankMeshes();
        }
        // Deactivate all children first
        for (int i = 0; i < meshGenerator.inventoryParent.transform.childCount; i++)
        {
            GameObject child = meshGenerator.inventoryParent.transform.GetChild(i).gameObject;
            bool isActive = (i == index);
            child.SetActive(isActive);

            // Update the BendGeometry reference if this is the active child
            if (isActive)
            {
                // Get the BendGeometry component
                benGeo = child.GetComponent<BendGeometry>();

                // Ensure benGeo is not null before adding listeners
                if (benGeo != null)
                {
                    // Remove existing listeners to avoid duplication
                    DirectionToggle.OnClicked.RemoveAllListeners();
                    bendSlider.OnValueUpdated.RemoveAllListeners();

                    // Add listeners
                    DirectionToggle.OnClicked.AddListener(() =>
                    {
                        bendSlider.Value=0.5f;
                    });

                    Debug.Log("ABC000 Here");
                    bendSlider.OnValueUpdated.AddListener(eventData =>
                    {
                        benGeo.OnBendSliderChanged(eventData.NewValue); // Extract the NewValue and pass it
                    });

                }
                else
                {
                    
                    Debug.LogWarning("BendGeometry component not found on the active child.");
                }
            }
            else
            {
                bool isOn = child.GetComponent<BendGeometry>().moved;
                child.SetActive(isOn);
            }
        }
    }

    void ToggleObjectManipulator(bool enable)
    {
        // Iterate through all children of inventoryParent
        foreach (Transform child in meshGenerator.inventoryParent.transform)
        {
            // Try to get the ObjectManipulator component
            var objectManipulator = child.GetComponent<ObjectManipulator>();
            var renderer = child.GetComponent<MeshRenderer>();
            if (objectManipulator != null)
            {
                objectManipulator.enabled = enable;
            }
            if (renderer != null)
            {
                renderer.enabled = !enable;
            }
        }
    }

    public void MapElements()
    {
        PriorityMapper priorityMapper= new PriorityMapper();
        List<LineRenderer> lineRenderers= new List<LineRenderer>();
        List<List<List<float>>> parts = new List<List<List<float>>> {};

        foreach (Transform child in drawController.currentDrawingParent.transform)
        {
            LineRenderer ln = child.gameObject.GetComponent<LineRenderer>();
            if (ln != null)
            {
                lineRenderers.Add(ln);
            }
        }

        List<Vector3> points = new List<Vector3>();
        List<(Vector3,Vector3)> endPoints = new List<(Vector3,Vector3)>();

        List<string> pointNames = new List<string>();
        
        foreach(Transform child in meshGenerator.inventoryParent.transform)
        {
            Vector3 pt = child.GetComponent<BendGeometry>().CalculateMeanPoint();
            (Vector3,Vector3) endpts = child.GetComponent<BendGeometry>().CalculateEndPoints();

            string ptName = child.gameObject.name;
            if (pt != null)
            {
                points.Add(pt);
                endPoints.Add(endpts);
                if (ptName != null)
                {
                    pointNames.Add(ptName);
                }
                parts.Add(child.GetComponent<BendGeometry>().parts);
            }
        }


        Dictionary<LineRenderer, List<string>> priorityOrder = priorityMapper.MapPointsToLineRenderers(lineRenderers, points, pointNames, 0.5f);
        priority = priorityMapper.MergePriorityData(priorityOrder,points, parts, endPoints, pointNames, lineRenderers);

        SetupSendPriorityDialog();
        //drawController.SavePriorityData(priority);
    }

    void OnNextScalePressed()
    {
        currentScaleIndex = (currentScaleIndex + 1) % scales.Length;
        UpdateScale();
    }

    void OnPrevScalePressed()
    {
        currentScaleIndex = (currentScaleIndex - 1 + scales.Length) % scales.Length;
        UpdateScale();
    }

    void UpdateScale()
    {
        float scale = scales[currentScaleIndex];
        drawController.currentDrawingParent.transform.localScale = Vector3.one * scale;
    }

}
