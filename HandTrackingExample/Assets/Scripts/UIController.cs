using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MixedReality.Toolkit.UX;

public class UIController : MonoBehaviour
{
    public MarkerLocalizer markerLocalizer;
    public DrawingController drawController;
    public MqttController mqttController;
    public MeshGeneratorFromJson meshGenerator;

    public PressableButton Localize;
    public PressableButton NewDrawing;
    public PressableButton Undo;
    public PressableButton SaveDrawing;
    public PressableButton VisualizeDrawing;
    public PressableButton NextDrawing;
    public PressableButton SendToRhino;

    public PressableButton VisualizeMesh;
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

    
    // Start is called before the first frame update
    void Start()
    {
        TableMenu.SetActive(false);

        Localize.OnClicked.AddListener(() => markerLocalizer.EnableLocalization());

        NewDrawing.OnClicked.AddListener(() => drawController.StartNewDrawing());
        Undo.OnClicked.AddListener(() =>drawController.UndoLine());

        NextDrawing.OnClicked.AddListener(() => drawController.VisualizeDrawing());
        SaveDrawing.OnClicked.AddListener(() => drawController.SaveEnabledDrawing());

        GameObject next = NextDrawing.gameObject.transform.parent.gameObject;
        VisualizeDrawing.OnClicked.AddListener(() => ToggleVisibility(next));
        SendToRhino.OnClicked.AddListener(() =>
        {
            mqttSendDialogBody.text = "Do you want to send the drawing no." + drawController.EnabledDrawingIndex.ToString() + " back to Rhino?";
            SetupSendToRhinoDialog();
        });  

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


        PickTeam.SetActive(true);
        PickTeam.GetComponent<Dialog>().SetPositive("Team A", args => DefineMqttTopics("A"));
        PickTeam.GetComponent<Dialog>().SetNegative("Team B", args => DefineMqttTopics("B"));

        StartDialog.SetActive(false);
        HandMenu.SetActive(false);
        StartDialog.GetComponent<Dialog>().SetNeutral("OK", args => Debug.Log("OK"));

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
            mqttController.topicsSubscribe.Add("/kitgkr_teamA_geometries/");
            mqttController.topicsPublish.Add("/kitgkr_teamA_drawings/");
            drawController.team = "teamA";
            mqttSendDialogHeader.text = "Team A: Send a Drawing to Rhino";
        }
        else if (team=="B")
        {
            mqttController.topicsSubscribe.Add("/kitgkr_teamB_geometries/");
            mqttController.topicsPublish.Add("/kitgkr_teamB_drawings/");
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

        dialog.SetPositive("Yes", args => meshGenerator.Generate(mqttController.msgData, meshGenerator.elementsParent));
        dialog.GetComponent<Dialog>().SetNegative("No", args => Debug.Log("Rejected"));

        mqttReceivedDialog.GetComponent<Dialog>().SetBody("Geometry: " + object_name + "\n Do you want to add this geometry in your digital space?");
        mqttReceivedDialogBody.text = "Geometry: " + object_name + "\n Do you want to add this geometry in your digital space?";
        mqttReceivedDialog.SetActive(true);
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

}
