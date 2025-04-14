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
    public UIPressableButton Localize;
    public PressableButton NewDrawing;
    //public PressableButton Undo;
    public PressableButton SaveDrawing;
    public PressableButton NextDrawing;
    public PressableButton SendToRhino;

    public PressableButton FreeHandScaleButton;
    public UIPressableButton ScaleParent;
    public PressableButton NextScale;
    public PressableButton PrevScale;
    public TMP_Text ScaleInfo;
    public TMP_Text NextScaleInfo;
    public TMP_Text PrevScaleInfo;

    public GameObject mqttReceivedDialog;

    public GameObject mqttSendDialog;
    public TextMeshProUGUI mqttSendDialogHeader;
    public TextMeshProUGUI mqttSendDialogBody;
    public TextMeshProUGUI mqttReceivedDialogBody;
    public GameObject mqttConnectionDialog;

    public GameObject PickTeam;

    public GameObject StartDialog;
    public PressableButton InventoryButton;


    private float[] scales = {1.0f, 0.5f, 0.2f, 0.1f, 0.05f, 0.02f, 0.01f, 0.004f, 0.002f, 0.001f };
    private int currentScaleIndex = 0;

    
    // Start is called before the first frame update
    void Start()
    {   

        Localize.OnToggled.AddListener(() => markerLocalizer.EnableLocalization());
        Localize.OnUntoggled.AddListener(() => markerLocalizer.DisableLocalization());
        InventoryButton.OnToggled.AddListener(() => 
        {
            meshGenerator.inventoryParent.SetActive(true);
            meshGenerator.elementsParent.SetActive(true);
            meshGenerator.locksParent.SetActive(true);
        });
        InventoryButton.OnUntoggled.AddListener(() => 
        {
            meshGenerator.inventoryParent.SetActive(false);
            meshGenerator.elementsParent.SetActive(true);
            meshGenerator.locksParent.SetActive(false);
        });

        NewDrawing.OnClicked.AddListener(() => drawController.StartNewDrawing());

        NextDrawing.OnClicked.AddListener(() => drawController.VisualizeDrawing());
        SaveDrawing.OnClicked.AddListener(() => drawController.SaveEnabledDrawing());

        GameObject next = NextDrawing.gameObject.transform.parent.gameObject;
        SendToRhino.OnClicked.AddListener(() =>
        {
            mqttSendDialogBody.text = "Do you want to send the drawing no." + drawController.EnabledDrawingIndex.ToString() + " back to CAD?";
            SetupSendToRhinoDialog();
        });

        
        FreeHandScaleButton.OnClicked.AddListener(() => drawController.ActivateFreehandScale(FreeHandScaleButton.IsToggled));
        NextScale.OnClicked.AddListener(OnNextScalePressed);
        PrevScale.OnClicked.AddListener(OnPrevScalePressed);

        mqttReceivedDialog.SetActive(false);
        mqttReceivedDialog.GetComponent<Dialog>().SetPositive("Yes", args => meshGenerator.Generate(mqttController.msgData, meshGenerator.elementsParent));
        mqttReceivedDialog.GetComponent<Dialog>().SetNegative("No", args => Debug.Log("Rejected"));

        mqttConnectionDialog.SetActive(false);
        mqttConnectionDialog.GetComponent<Dialog>().SetNeutral("OK", args => mqttController.Connect());

        mqttSendDialog.SetActive(false);
        mqttSendDialog.GetComponent<Dialog>().SetPositive("Yes", args => drawController.SendToRhino());
        mqttSendDialog.GetComponent<Dialog>().SetNegative("No", args => Debug.Log("Rejected"));


        PickTeam.SetActive(true);
        PickTeam.GetComponent<Dialog>().SetPositive("Team A", args => DefineMqttTopics("A"));
        PickTeam.GetComponent<Dialog>().SetNegative("Team B", args => DefineMqttTopics("B"));

        StartDialog.SetActive(false);

        StartDialog.GetComponent<Dialog>().SetNeutral("OK", args => Debug.Log("OK"));

    }

    void Update()
    {
        if (ScaleParent.IsToggled)
        {
            if (drawController.currentDrawingParent == null)
            {
                return;
            }

            float scaleValue = drawController.currentDrawingParent.transform.localScale.x;
            meshGenerator.inventoryParent.transform.localScale = Vector3.one * scaleValue;
            if (scaleValue !=1.0f)
            {
                meshGenerator.locksParent.transform.localScale = Vector3.one * 0.75f;
            }
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

        }
    }

    public void SetPeriodic(bool active)
    {
        drawController.newPeriodic = active;

    }

    void SetupSendToRhinoDialog()
    {
        var dialog = mqttSendDialog.GetComponent<Dialog>();
        dialog.Reset();

        dialog.SetPositive("Yes", args => drawController.SendToRhino());
        dialog.SetNegative("No", args => Debug.Log("Rejected"));

        mqttSendDialog.SetActive(true);
    }



    void DefineMqttTopics(string team)
    {
        if (team=="A")
        {
            mqttController.topicsSubscribe.Add("/reclaimingcraft_teamA_geometries/");
            mqttController.topicsSubscribe.Add("/reclaimingcraft_teamA_geometry/");
            mqttController.topicsSubscribe.Add("/reclaimingcraft_teamA_lines/");
            mqttController.topicsPublish.Add("/reclaimingcraft_teamA_drawings/");
            mqttController.topicsPublish.Add("/reclaimingcraft_teamA_priority/");
            drawController.team = "teamA";
            mqttSendDialogHeader.text = "Team A: Send a Drawing to Rhino";
        }
        else if (team=="B")
        {
            mqttController.topicsSubscribe.Add("/reclaimingcraft_teamB_geometries/");
            mqttController.topicsSubscribe.Add("/reclaimingcraft_teamB_geometry/");
            mqttController.topicsSubscribe.Add("/reclaimingcraft_teamB_lines/");
            mqttController.topicsPublish.Add("/reclaimingcraft_teamB_drawings/");
            mqttController.topicsPublish.Add("/reclaimingcraft_teamB_priority/");
            drawController.team = "teamB";
            mqttSendDialogHeader.text = "Team B: Send a Drawing to Rhino";
        }
        mqttController.subscribeTopics();
        StartDialog.SetActive(true);
    }

    public void ToggleVisibility(GameObject gameObject)
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
        if (topic == "/reclaimingcraft_teamA_geometry/" || topic == "/reclaimingcraft_teamB_geometry/")
        {
            dialog.SetPositive("Yes", args => meshGenerator.Generate(mqttController.msgData, meshGenerator.elementsParent));
        }
        else if (topic == "/reclaimingcraft_teamA_lines/" || topic == "/reclaimingcraft_teamB_lines/")
        {

            dialog.SetPositive("Yes", args => drawController.GenerateNewDrawings(mqttController.msgDataLines));
        }
        else if (topic == "/reclaimingcraft_teamA_geometries/" || topic == "/reclaimingcraft_teamB_geometries/")
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
        Debug.Log("Current scale index: " + currentScaleIndex);
        float scale = scales[currentScaleIndex];
        NextScaleInfo.text = "1:" + Mathf.RoundToInt(1/ scales[(currentScaleIndex + 1) % scales.Length]).ToString();
        PrevScaleInfo.text = "1:" + Mathf.RoundToInt(1/ scales[(currentScaleIndex - 1 + scales.Length) % scales.Length]).ToString();
        drawController.currentDrawingParent.transform.localScale = Vector3.one * scale;
    }


}


