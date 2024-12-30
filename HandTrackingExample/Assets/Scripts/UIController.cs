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
    public GameObject SelectGeos;
    
    public PressableButton DetailsButton;
    public GameObject Submenu;
    public PressableButton nextGeo;
    public PressableButton previousGeo;
    public PressableButton VisibTimber;
    public PressableButton nextPair;
    public PressableButton previousPair;
    public PressableButton VisibInventory;
    public PressableButton RotateClockwise;
    public PressableButton FlipButton;
    public Slider MoveSlider;

    public GameObject arrow;

    private bool endPointsOn = false;
    private bool detailsOn = false;
    private List<string[]> totalpairs = new List<string[]>();
    private  GameObject[] pairObjects = new GameObject[2];
    private bool isFirstEl = true;
    private int pairIndex = -1;

    [HideInInspector]  public string detailElement="";
    [HideInInspector]  public GameObject detailElementObject;

    const float OVERLAP_DISTANCE = 0.30f;

    private int ind = 0;

    private float[] scales = { 0.5f, 0.2f, 0.1f, 0.05f, 0.02f, 0.01f, 0.004f, 0.002f, 0.001f };
    private int currentScaleIndex = 0;

    private Dictionary<string, List<(List<(string pointName, bool direction, List<List<float>> intervals)>, float parameter)>> priority;

    
    // Start is called before the first frame update
    void Start()
    {   
        VisibInventory.OnClicked.AddListener(() => {
            if (meshGenerator?.inventoryParent != null && drawController?.linesParent != null) {
                ToggleVisibility(meshGenerator.inventoryParent);
                ToggleVisibility(drawController.linesParent);
            } else {
                Debug.LogWarning("Required parent objects are missing");
            }
        });
        VisibTimber.OnClicked.AddListener(() => ToggleVisibility(meshGenerator.inventoryParent));
        arrow.SetActive(false);
        Submenu.SetActive(false);
        SelectGeos.SetActive(false);
        InventoryButton.OnClicked.AddListener(() => {
            ToggleVisibility(SelectGeos);
            drawController.EnableControlPoints(false);
            endPointsOn = !endPointsOn;
            Debug.Log("Inventory Button Clicked and the end points will be " + endPointsOn);
            drawController.EnableEndPoints(endPointsOn);
            drawController.ActivateStandBy();
        });
        DetailsButton.OnClicked.AddListener(() => {
            ToggleVisibility(Submenu);
            drawController.ActivateStandBy();
            detailsOn = !detailsOn;
            if (detailsOn)
            {
                CalculatePairs();
                pairIndex = -1;
            }

        });
        nextGeo.OnClicked.AddListener(ShowNextGeo);
        previousGeo.OnClicked.AddListener(ShowPreviousGeo);

        RotateClockwise.OnClicked.AddListener(() => {
            var timElement = detailElementObject?.GetComponent<TimberElement>();
            if (timElement != null)
            {
                Debug.LogWarning($"Choco {timElement.rotated} ");
                timElement.rotated = (timElement.rotated + 90) % 360;
                Debug.LogWarning($"Choco2 {timElement.rotated} ");

                TransformDetailElement(detailElementObject, isFirstEl);
            }
            else
            {
                Debug.LogWarning("No TimberElement component found or detail element is null");
            }


        });

        MoveSlider.OnValueUpdated.AddListener((SliderEventData sliderEventData) => {
            var timElement = detailElementObject?.GetComponent<TimberElement>();
            if (timElement != null)
            {
                // Extract the slider value from sliderEventData
                float value = sliderEventData.NewValue;

                // Remap the slider value from 0-1 to -0.1-0.1
                float remappedValue = (value * 0.2f) - 0.1f;
                timElement.moveDist = remappedValue;

                TransformDetailElement(detailElementObject, isFirstEl);
            }
            else
            {
                Debug.LogWarning("No TimberElement component found or detail element is null");
            }
        });


        FlipButton.OnClicked.AddListener(() => {
            var timElement = detailElementObject?.GetComponent<TimberElement>();
            if (timElement != null)
            {
                Debug.Log($"Chico: {timElement.flipped}");
                timElement.flipped = !timElement.flipped;
                Debug.Log($"Chico2: {timElement.flipped}");
                TransformDetailElement(detailElementObject, isFirstEl);
            }
            else
            {
                Debug.LogWarning("No TimberElement component found or detail element is null");
            }
        });

        nextPair.OnClicked.AddListener(() => ShowPair(1));
        previousPair.OnClicked.AddListener(() => ShowPair(-1));

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

        SelectToDelete.OnClicked.AddListener(() => {
            drawController.EnableDeleteLineState();
            drawController.ActivateStandBy();
            ToggleVisibility(NextToDelete.gameObject.transform.parent.gameObject);
            ToggleVisibility(Delete.gameObject.transform.parent.gameObject); 
            ToggleVisibility(CopyLine.gameObject.transform.parent.gameObject);
        });

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

            foreach (Transform child in meshGenerator.locksParent.transform)
            {
                GameObject element = child.GetComponent<ElementStateController>().target;

                // 1) Grab world-space bounds from the Renderer
                Bounds worldBounds = element.GetComponent<Renderer>().bounds;
                Vector3 worldCenter = worldBounds.center;
                Vector3 worldSize   = worldBounds.size;

                // 2) Convert them to element-local coordinates
                Vector3 localCenter = element.transform.InverseTransformPoint(worldCenter);
                Vector3 localSize   = element.transform.InverseTransformVector(worldSize);

                // 3) Adjust local offset to be "above" the item
                Vector3 localOffset = new Vector3(
                    localCenter.x *scaleValue,
                    (localCenter.y + (localSize.y / 2.0f) + 0.075f) * scaleValue,
                    localCenter.z *scaleValue
                );

                // 4) Assign to the lock's Orbital LocalOffset
                child.GetComponent<Orbital>().LocalOffset = localOffset;
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

        // Deactivate all children first
        for (int i = 0; i < meshGenerator.inventoryParent.transform.childCount; i++)
        {
            GameObject child = meshGenerator.inventoryParent.transform.GetChild(i).gameObject;
            GameObject childlock = meshGenerator.locksParent.FindObject("lock_"+ child.name);
            bool isActive = (i == index);
            child.SetActive(isActive);
            childlock.SetActive(isActive);

            if (!isActive)
            {
                //Debug.LogError("The child "+ child.name + " is not active");
                bool isOn = child.GetComponent<TimberElement>().moved;
                child.SetActive(isOn);
                childlock.SetActive(isOn);
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

    void  CalculatePairs()
    {   totalpairs.Clear();

        foreach (Transform child in drawController.currentDrawingParent.transform)
        {
            // Check if the child GameObject has the specific tag you are interested in
            if (child.CompareTag("simplified"))
            {
                List<string[]> pairs = child.GetComponent<MappingOnCurve>().GetPairs();
                if (pairs != null && pairs.Count > 0)
                {
                    totalpairs.AddRange(pairs);
                }
            }
        }

        Debug.Log($"Total pairs: {totalpairs.Count} // [{string.Join(", ", totalpairs)}]");
    }

    public void ShowPair(int direction = 1)
    {
        arrow.SetActive(false);
        meshGenerator.detailsParent.SetActive(true);
        if (totalpairs.Count == 0) return;

        pairIndex = (pairIndex + 1 * direction) % totalpairs.Count;

        string[] pair = totalpairs[pairIndex];

        Debug.Log($"Here the pairs are [{string.Join(", ", pair)}]");

        // First deactivate all children
        foreach (Transform child in meshGenerator.detailsParent.transform)
        {
            child.gameObject.SetActive(false);
        }

        pairObjects[0] = meshGenerator.detailsParent.transform.Find(pair[0]).gameObject;
        pairObjects[1] = meshGenerator.detailsParent.transform.Find(pair[1]).gameObject;

        if (pairObjects[0]==null)
        {
            Debug.Log("Disaster0");
        }
        else if ( pairObjects[1]==null)
        {
            Debug.Log("Disaster1");
        }

        TransformDetailElement(meshGenerator.inventoryParent.transform.Find(pair[0]).gameObject);
        TransformDetailElement(meshGenerator.inventoryParent.transform.Find(pair[1]).gameObject, false);
        
        pairObjects[0].SetActive(true);
        pairObjects[1].SetActive(true);
        
    }

    public void SetDetailElement()
    {
        foreach(Transform child in meshGenerator.inventoryParent.transform)
        {
            if (child.name == detailElement)
            {
                detailElementObject = child.gameObject;

                if (pairObjects[0].name == detailElement)
                {
                    isFirstEl = true;
                }
                else if (pairObjects[1].name == detailElement)
                {
                    isFirstEl = false;
                }
                MoveSlider.Value = (detailElementObject.GetComponent<TimberElement>().moveDist + 0.1f) / 0.2f;
                Debug.Log($"The selected object is: {child.name} and it is first: {isFirstEl}");
                return;
            }
        }
    }

    /// <summary>
    /// Transforms a detail element's position and rotation based on its properties
    /// </summary>
    /// <param name="isFirstElement">Whether this is the first element in a pair</param>
    private void TransformDetailElement(GameObject originalObject, bool isFirstElement = true)
    {

        if (originalObject == null)
        {
            Debug.LogWarning($"TransformDetailElement: Original Object is null");
            
            return;
        }

        GameObject targetObject = meshGenerator.detailsParent.FindObject(originalObject.name);
        // Find corresponding timber element

        if (targetObject == null)
        {
            Debug.LogWarning($"TransformDetailElement: Target object is null");
            return;
        }

        // Get element properties
        var elementProperties = originalObject.GetComponent<TimberElement>();
        if (elementProperties == null)
        {
            Debug.LogError($"TransformDetailElement: Missing TimberElement component on {originalObject.name}");
            return;
        }

        float moveDist = elementProperties.moveDist;
        bool isFlipped = elementProperties.flipped;
        int rotation = elementProperties.rotated;

        //Debug.Log($"Choco The object is: {originalObject.name} and it is the first element: {isFirstElement} and it is flipped: {isFlipped} and rotation in Z is {targetObject.transform.rotation.eulerAngles.z} and movedist is {moveDist} ");

        // Calculate bounds
        var renderer = targetObject.GetComponent<Renderer>();
        if (renderer == null)
        {
            Debug.LogError($"TransformDetailElement: Missing Renderer on {targetObject.name}");
            return;
        }

        Bounds worldBounds = renderer.bounds;
        float localSize = elementProperties.length;

        // Calculate new position and rotation
        Vector3 newPosition = targetObject.transform.position;
        Vector3 newRotation = targetObject.transform.rotation.eulerAngles;

        // Base position offset accounting for element size and overlap
        float baseOffset = OVERLAP_DISTANCE / 2f;
        float sizeOffset = isFlipped ? 0 : -localSize;

        // Calculate x position based on element order and properties
        newPosition.x = isFirstElement ?
            sizeOffset + moveDist + baseOffset :
            (isFlipped ? localSize : 0) + moveDist - baseOffset;

        
        Debug.LogWarning($"Choco {rotation} ");
        newRotation.x = rotation;

        if (! isFirstElement)
        {
            newPosition.z = 0.15f;
        }
        else
        {
            newPosition.z = 0.0f;
        }

        if (isFlipped)
        {
            newRotation.z = 180;
            Debug.LogWarning($"Choco222");
        }
        else
        {
            newRotation.z = 0;
            Debug.LogWarning($"Choco11");
        }


        // Apply transformation
        targetObject.transform.position = newPosition;
        targetObject.transform.rotation = Quaternion.Euler(rotation, 0, newRotation.z );

        Debug.Log($"Choco2 The object is: {originalObject.name} and it is the first element: {isFirstElement} and it is flipped: {isFlipped}  and rotation in z is {targetObject.transform.rotation.eulerAngles.z} and the rotation in x is {targetObject.transform.rotation.eulerAngles.x} and movedist is {moveDist} ");
        //Debug.LogWarning($"Choco2 {newRotation.x} ");
    }
}


//  // Handle rotation first
//         newRotation.x = rotation;

//         if (!isFirstElement)
//         {
//             newPosition.z = 0.15f;
//         }
//         else
//         {
//             newPosition.z = 0.0f;
//         }

//         // Handle flip state independently of rotation
//         if (isFlipped)
//         {
//             // Only apply 180 degree flip if not already rotated
//             newRotation.z = (rotation == 0 || rotation == 180) ? 180 : rotation;
//         }
//         else
//         {
//             newRotation.z = rotation;
//         }

//         // Apply transformation
//         targetObject.transform.position = newPosition;
//         targetObject.transform.rotation = Quaternion.Euler(newRotation);

//         Debug.Log($"Object: {originalObject.name}, First: {isFirstElement}, Flipped: {isFlipped}, Rotation Z: {newRotation.z}, MoveDist: {moveDist}");
//         Debug.LogWarning($"Rotation X: {newRotation.x}");
