using System;
using System.IO;
using System.Text;
using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.OpenXR.Input;
using TMPro;
using DrawingsData;
using MeshElementData;
using Newtonsoft.Json;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.SpatialManipulation;
using System.Linq;

public class DrawingController : MonoBehaviour
{
    [HideInInspector] public bool receivedDrawing = false;
    [HideInInspector] public bool drawingOn = false;
    [HideInInspector] public bool pointsOn = false;
    [HideInInspector] public bool pinPointsOn = false;
    private bool farDrawing = false;

    [HideInInspector] public string drawingMode = "farDrawing";
    [HideInInspector] public string lastDrawingMode = "";

    public GameObject BoundsBox;
    [HideInInspector] public GameObject Pin;
    public UIController uIController;
    public MeshGeneratorFromJson meshGenerator;
    public Drawings storedDrawings; 
    public MqttController mqttController;
    public PinManager pinManager;
    public Transform reticleTransform;
    //public GameObject reticleVisual;
    public Material lineMaterial;
    public GameObject linesParent;
    public int EnabledDrawingIndex = -1;
    private LineRenderer lineRenderer;
    private LineRenderer lineRenderer_realtime;
    private LineRenderer lineRenderer_simplified;
    public Material ControlPointMaterial;
    private float lineWidth = 0.002f;
    private int DrawingIndex = -1;
    private int lineIndex = -1;
    private int linePointIndex = 0;
    private GameObject reticleVisual;
    private GameObject lineObject;
    private GameObject lineObjectSimplified;
    private float minDistance = 0.005f;
    private Vector3 previousPosition = Vector3.zero;

    [HideInInspector] public GameObject currentDrawingParent;
    private HandsAggregatorSubsystem aggregator;
    private string timestamp;
    [HideInInspector] public bool newPeriodic = false;
    public bool scaleMode = false;
    private bool delState = false;
    private bool lineChanged = true;
    private bool activateStandBy = false;
    private int indexToChange;
    private GameObject selectedLine;
    private GameObject prevSelLine = null;
    private Color prevColor;
  
    [HideInInspector] public string team;


    public void ToggleScale()
    {
        scaleMode =!scaleMode;

        if (currentDrawingParent==null)
        {
            return;
        }

        currentDrawingParent.MakeInteractableBoundBox(BoundsBox, false);

        if (scaleMode)
        {
            currentDrawingParent.MakeInteractableBoundBox(BoundsBox, true, true); 
        }
    }

    public void SendToRhino()
    {
        string dkey = "drawing" + EnabledDrawingIndex.ToString();
        SavePinPoints(dkey);
        UpdatePositionsInDictionary (dkey);
        object message = storedDrawings.drawings[dkey];
        
        Dictionary<string, object> msg_dict = new Dictionary<string, object>
        {
            {"result", message}
        };
        mqttController.message = msg_dict;
        mqttController.Publish(mqttController.topicsPublish[0]);

    }

    IEnumerator EnableWhenSubsystemAvailable()
    {
        // Wait until the HandsAggregatorSubsystem is available
        yield return new WaitUntil(() => aggregator != null);

        // Once available, you can access its properties
        string isPhysicalData = aggregator.subsystemDescriptor.id;
        
    }

    void Start()
    {
        timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");
        storedDrawings = new Drawings
        {
            uid = timestamp,
            drawings = new Dictionary<string, Drawing>()
        };

        reticleVisual = reticleTransform.gameObject.transform.Find("RingVisual").gameObject;

        aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();

        if (aggregator != null)
        {
            // If the aggregator is available, enable functionality
            StartCoroutine(EnableWhenSubsystemAvailable());
        }
        uIController.ModeText.text = "DRAW mode: Hand Ray";
    }

    void Update()
    {   
        if (drawingMode == "farDrawing")
        {
            if (reticleVisual.activeSelf==false)
            {
                foreach (Transform child in meshGenerator.elementsParent.transform)
                {
                    StatefulInteractable meshInteractable = child.gameObject.GetComponent<StatefulInteractable>();
                    meshInteractable.ForceSetToggled(true);
                    meshInteractable.TryToggle();
                }
                
            }
            if (drawingOn)
            {
                // Check if this is the first time the line is being created
                if (linePointIndex == -1)
                {
                    // Set the initial position for the line
                    previousPosition = reticleTransform.position;
                    linePointIndex = 0;
                    lineIndex++;
                    // Instantiate a new line renderer for the current line index
                    lineRenderer_realtime = InstantiateLine(lineIndex, "realtime");
                    
                    Debug.Log("Line index is" + lineIndex + "lineRenderer" + lineRenderer_realtime);
                }

                if (Vector3.Distance(previousPosition, reticleTransform.position)> minDistance)
                {
                    AddPointsToLine(lineRenderer_realtime, reticleTransform.position);
                    previousPosition = reticleTransform.position;
                }

            }
            else
            {
                
                if (linePointIndex != -1 && lineIndex != -1)
                {
                    SimplifyDrawing(false, newPeriodic);
                    //StoreDrawnLine(lineRenderer_realtime);
                    //GenerateMeshCollider();
                }

                linePointIndex = -1;

            }
        }
        else if (drawingMode == "pinchDrawing")
        {
            if (aggregator != null)
            {
                // Get a single joint (Index tip, on left hand, for example)
                bool jointIsValid = aggregator.TryGetJoint(TrackedHandJoint.IndexTip, XRNode.RightHand, out HandJointPose jointPose);

                // Check whether the user's left hand is facing away (commonly used to check "aim" intent)
                bool handIsValid = aggregator.TryGetPalmFacingAway(XRNode.LeftHand, out bool isLeftPalmFacingAway);

                // Query pinch characteristics from the left hand.
                bool handIsValidPinch = aggregator.TryGetPinchProgress(XRNode.RightHand, out bool isReadyToPinch, out bool isPinching, out float pinchAmount);

                if (isPinching)
                {
                    if (linePointIndex == -1)
                    {
                        previousPosition = jointPose.Pose.position;
                        linePointIndex = 0;
                        lineIndex++;
                        // Instantiate a new line renderer for the current line index
                        lineRenderer_realtime = InstantiateLine(lineIndex, "realtime");
                        Debug.Log("Line index is" + lineIndex + "lineRenderer" + lineRenderer_realtime);
                    }

                    if (Vector3.Distance(previousPosition, jointPose.Pose.position)> minDistance)
                    {
                        AddPointsToLine(lineRenderer_realtime, jointPose.Pose.position);
                        previousPosition = jointPose.Pose.position;
                    }


                }
                else
                {
                    if (linePointIndex != -1 && lineIndex!=-1)
                    {
                        SimplifyDrawing(true, newPeriodic);
                        //GenerateMeshCollider();
                    }
                    linePointIndex = -1;
                }
            }
        }
        
    }

    void SimplifyDrawing(bool pinch, bool periodic, bool simplify=true)
    {
        lineObjectSimplified = new GameObject("");
        CurveManipulator curveManipulator = lineObjectSimplified.AddComponent<CurveManipulator>();
        lineObjectSimplified = curveManipulator.SimplifyCurve(lineObject, periodic, simplify);

        Destroy(lineObject);
        lineRenderer_simplified = lineObjectSimplified.GetComponent<LineRenderer>();
        if (pinch)
        {
            lineRenderer_simplified = curveManipulator.FlattenCurve(lineRenderer_simplified);
        }  
         
        //then we store it
        StoreDrawnLine(lineRenderer_simplified);

        curveManipulator.CreateControlPoints(lineObjectSimplified, ControlPointMaterial, periodic);

    }

    void CreatePlanks()
    {
        float[] plankLengths = meshGenerator.stock.priorityLengths;
        string[] planknames = meshGenerator.stock.priority;


        foreach (Transform child in currentDrawingParent.transform)
        {
            if (child.name != "pins" && child.gameObject.name != "BoundingBox(Clone)")
            {
                GameObject bPlanks = child.gameObject.FindObject("bendedPlanks");
                if (bPlanks != null)
                {
                    bPlanks.DestroyGameObjectAndChildren("all",true);
                }
                GameObject bendedPlanks = new GameObject("bendedPlanks");
                bendedPlanks.transform.parent = child.transform;
                // bendedPlanks.transform.localRotation = Quaternion.identity;
                // bendedPlanks.transform.localScale = Vector3.one;

                CurveManipulator lnManipulator = child.gameObject.GetComponent<CurveManipulator>();
                LineRenderer lnR = child.gameObject.GetComponent<LineRenderer>();
                Vector3[] points = new Vector3[lnR.positionCount];
                lnR.GetPositions(points);


                (List<Vector3[]> segmentedPoints, float[] updPlankLengths, List<string> usedNames) = lnManipulator.SegmentCurve(points.ToList(), plankLengths, planknames);
               plankLengths = updPlankLengths;

                int j =0;
                foreach (Vector3[] spoints in segmentedPoints)  
                {
                    PlankBender plankBender = new PlankBender();

                    string usedname = usedNames[j];
                    GameObject plank = new GameObject(usedname);
                    plank.transform.parent = bendedPlanks.transform;
                    // plank.transform.localRotation = Quaternion.identity;
                    // plank.transform.localScale = Vector3.one;
                    
                    Plank plankToUse = meshGenerator.stock.planks[usedname];
                    // Generate the plank mesh
                    Color elColor= new Color(plankToUse.color[0], plankToUse.color[1], plankToUse.color[2], plankToUse.color[3]);
                    plank = plankBender.GeneratePlankMesh( lnManipulator.projectionPlane.normal, currentDrawingParent.transform.localScale.x, plankToUse.dimensions.width, plankToUse.dimensions.height, spoints, plank, elColor);

                    j++;
                }

            }
        }

    }

    public void Test()
    {
        CreatePlanks();
    }

    public void GenerateNewDrawings(Drawings data)
    {
        foreach(KeyValuePair<string, Drawing> drawEntry in data.drawings)
        {
            Drawing drawing = drawEntry.Value;
            StartNewDrawing();
            foreach (KeyValuePair<string, DrawingLine> lineEntry in drawing.lines)
            {
                
                DrawingLine line = lineEntry.Value;
                lineIndex++;
                Debug.Log("Hoiii" + lineIndex.ToString());
                
                lineRenderer_realtime = InstantiateLine(lineIndex, "realtime");
                lineRenderer_realtime.positionCount = line.positions.Length;
                lineRenderer_realtime.SetPositions(line.positions);

                SimplifyDrawing(false, line.periodic ,false);
            }
        }
        linePointIndex =-1;
    }

    public void ActivateStandBy()
    {
        activateStandBy = !activateStandBy;
        if (activateStandBy)
        {
            lastDrawingMode = drawingMode;
            pointsOn = false;
            pinPointsOn = false;
            farDrawing = false;
            drawingMode ="";
            uIController.ModeText.text = "DRAW mode: Editing";
            uIController.Periodic.SetActive(false);
            EnableControlPoints(pointsOn);
            ActivatePinPoints(pinPointsOn);
            pinManager.newPin.SetActive(pinPointsOn);

            foreach (Transform child in meshGenerator.elementsParent.transform)
            {
                child.gameObject.GetComponent<MeshCollider>().enabled=farDrawing;
                child.gameObject.GetComponent<StatefulInteractable>().enabled=farDrawing;
            }
        }
        else
        {
            if (lastDrawingMode == "pinchDrawing")
            {
                drawingMode ="farDrawing";
            }
            else if (lastDrawingMode == "controlPoints")
            {
                drawingMode ="pinchDrawing";
            }
            else if (lastDrawingMode == "pinPoints")
            {
                drawingMode ="controlPoints";
            }
            else if (lastDrawingMode == "farDrawing")
            {
                drawingMode ="pinPoints";
            }
            ToggleDrawingMode();
        }
    }
    public void ToggleDrawingMode()
    {

        if (drawingMode =="farDrawing")
        {
            drawingMode ="pinchDrawing";
            pointsOn = false;
            farDrawing = false;
            uIController.ModeText.text = "DRAW mode: Pinch Gesture";
            uIController.Periodic.SetActive(true);
        }
        else if (drawingMode =="pinchDrawing")
        {
            drawingMode ="controlPoints";
            pointsOn = true;
            farDrawing = false;
            uIController.ModeText.text = "DRAW mode: Control Points";
            uIController.Periodic.SetActive(false);

            EnableControlPoints(pointsOn);
        }
        else if (drawingMode =="controlPoints")
        {
            drawingMode ="pinPoints";
            pointsOn = false;
            pinPointsOn = true;
            farDrawing = false;
            uIController.ModeText.text = "DRAW mode: Pin Points";
            ActivatePinPoints(pinPointsOn);
            pinManager.newPin.SetActive(pinPointsOn);
            EnableControlPoints(pointsOn);
            uIController.Periodic.SetActive(false);
        }
        else
        {
            drawingMode ="farDrawing";
            farDrawing = true;
            pinPointsOn = false;
            ActivatePinPoints(pinPointsOn);
            pinManager.newPin.SetActive(pinPointsOn);
            uIController.ModeText.text = "DRAW mode: Hand Ray";
            uIController.Periodic.SetActive(true);
        }

        foreach (Transform child in meshGenerator.elementsParent.transform)
        {
            child.gameObject.GetComponent<MeshCollider>().enabled=farDrawing;
            child.gameObject.GetComponent<StatefulInteractable>().enabled=farDrawing;
        }
    }

    void EnableControlPoints(bool enable)
    {
        // Iterate over each child of currentDrawingParent
        foreach (Transform child in currentDrawingParent.transform)
        {
            // Check if the child GameObject has the specific tag you are interested in
            if (child.CompareTag("simplified"))
            {
                foreach (Transform grandchild in child.transform)
            {
                if (grandchild.name== "ControlPoint")
                {
                    grandchild.gameObject.GetComponent<Renderer>().enabled = enable;
                    grandchild.gameObject.GetComponent<ObjectManipulator>().enabled = enable;
                }
            }
            }
        }
    }

    void ActivatePinPoints(bool activate)
    {
        // Iterate over each child of currentDrawingParent

        GameObject pins = currentDrawingParent.FindObject("pins");
        foreach (Transform child in pins.transform)
        {
            child.gameObject.GetComponent<ObjectManipulator>().enabled = activate;
            child.gameObject.GetComponent<BoxCollider>().enabled = activate;
        }
    }

    public void StartNewDrawing()
    {
        HideChildren(linesParent.transform);

        string dkey = "drawing" + DrawingIndex.ToString();
        pinManager.newPin.name ="pin0";
        pinManager.pinIndex =1;

        
        Transform parent = meshGenerator.elementsParent.transform;
        storedDrawings.drawings[dkey].MCF = new Frame
            {
                point = parent.position,
                xaxis = parent.right,
                zaxis = parent.forward
            };
        storedDrawings.drawings[dkey].objectFrames = new Dictionary<string, Frame>();

        foreach (Transform child in meshGenerator.elementsParent.transform)
        {
            storedDrawings.drawings[dkey].objectFrames[child.name] = new Frame
            {
                point = child.position,
                xaxis = child.right,
                zaxis = child.forward
            };
        }

        CreateDrawingParent();
        Debug.Log("Start drawing No:" + DrawingIndex + "  "+ linePointIndex);
        


    }

    public void VisualizeDrawing()
    {
        HideChildren(linesParent.transform);
        EnabledDrawingIndex ++;
        if (EnabledDrawingIndex == linesParent.transform.childCount)
        {
            EnabledDrawingIndex=0;
        }
        currentDrawingParent = linesParent.transform.GetChild(EnabledDrawingIndex).gameObject;
        lineIndex = currentDrawingParent.transform.childCount -1;
        currentDrawingParent.SetActive(true);

        int existingPins = currentDrawingParent.FindObject("pins").transform.childCount;
        pinManager.newPin.name ="pin" + existingPins;
        pinManager.pinIndex = existingPins + 1;


        EnableControlPoints(pointsOn);
        ActivatePinPoints(pinPointsOn);

        foreach (var kvp in storedDrawings.drawings["drawing"+ EnabledDrawingIndex.ToString()].objectFrames)
        {
            string object_name = kvp.Key;
            Frame object_frame = kvp.Value;

            GameObject gobject = meshGenerator.elementsParent.FindObject(object_name);
            gobject.Orient(object_frame);
        }

    }


    public void CopySelectedLine()
    {
       if (selectedLine != null)
        {

            GameObject clonedLine = Instantiate(selectedLine, currentDrawingParent.transform);

            LineRenderer lineR = clonedLine.GetComponent<LineRenderer>();

            // Adjust each point's position in the LineRenderer
            int positionCount = lineR.positionCount;
            Vector3[] positions = new Vector3[positionCount];
            lineR.GetPositions(positions);

            for (int i = 0; i < positionCount; i++)
            {
                positions[i] += new Vector3(0, 0.2f, 0);
            }

            lineR.SetPositions(positions);
            clonedLine.GetComponent<CurveManipulator>().lineRenderer = lineR;

            clonedLine.transform.position += new Vector3(0, 0.2f, 0);

            int originalIndex = selectedLine.transform.GetSiblingIndex();
            clonedLine.transform.SetSiblingIndex(originalIndex + 1);

            int lineCounter = 0;
            foreach (Transform child in currentDrawingParent.transform)
            {
                if (child.name != "pins" && child.name != "BoundingBox(Clone)")
                {
                    child.name = "line" + lineCounter.ToString();
                    lineCounter++;
                }
            }
            SelectLine();
        } 
    }
    public void EnableDeleteLineState()
    {
        delState = ! delState;
        if (delState)
        {
            indexToChange = lineIndex;
            SelectLine();
        }
        else
        {
            prevSelLine.GetComponent<LineRenderer>().material.color = prevColor;

            selectedLine.MakeInteractableBoundBox(BoundsBox, false);

            if (lineChanged)
            {
                int lineCounter = 0;
                foreach (Transform child in currentDrawingParent.transform)
                {
                    if (child.name != "pins" && child.name != "BoundingBox(Clone)")
                    {
                        child.name = "line" + lineCounter.ToString();
                        child.gameObject.MakeInteractableBoundBox(BoundsBox, false);
                        lineCounter++;
                    }
                }
            }
            lineChanged = false;
        }
    }

    public void DeleteLine()
    {
        if (selectedLine != null)
        {
            
            selectedLine.DestroyGameObjectAndChildren("all",true);
            
            prevSelLine = null;
            
            lineChanged = true;

            SelectLine();
        }
    }

    public void SelectLine()
    {

        if (indexToChange == currentDrawingParent.transform.childCount -1)
        {
            indexToChange = 0;
        }
        selectedLine = currentDrawingParent.FindObject("line"+ indexToChange.ToString());
        indexToChange++;

        if (selectedLine != null)
        {
            LineRenderer lineR = selectedLine.GetComponent<LineRenderer>();
            if (lineR.positionCount < 2)
            {
                Destroy(selectedLine);
                SelectLine();
                return;
            }
            
            selectedLine.MakeInteractableBoundBox(BoundsBox, true);
            
            if (prevSelLine!=null)
            {
                prevSelLine.GetComponent<LineRenderer>().material.color = prevColor;

                prevSelLine.MakeInteractableBoundBox(BoundsBox, false);
            }
            
            prevSelLine = selectedLine;
            prevColor = lineR.material.color;
            
            lineR.material.color = Color.blue;
        }
        else
        {
            SelectLine();
        }

    }

    public void CreateDrawingParent()
    {
        DrawingIndex++;
        
        if (DrawingIndex == 0)
        {
            Vector3 newPos = meshGenerator.elementsParent.transform.position + (meshGenerator.elementsParent.transform.rotation * pinManager.initialPinPosition);
            Quaternion newRot = meshGenerator.elementsParent.transform.rotation;
            pinManager.InstantiatePin(newPos, newRot);
            pinManager.newPin.SetActive(false); 
        }

        uIController.DrawingText.text = "Drawing No: " + DrawingIndex.ToString() + "    |";
        currentDrawingParent = new GameObject("drawing" + DrawingIndex);
        GameObject pinPoints = new GameObject("pins");
        pinPoints.transform.parent = currentDrawingParent.transform;
        currentDrawingParent.transform.parent = linesParent.transform;

        string dkey = "drawing" + DrawingIndex.ToString();

        if (!storedDrawings.drawings.ContainsKey(dkey))
        {
            Transform parent = meshGenerator.elementsParent.transform;
            storedDrawings.drawings[dkey] = new Drawing
            {   
                MCF = new Frame
                {
                    point = parent.position,
                    xaxis = parent.right,
                    zaxis = parent.forward
                },
                lines = new Dictionary<string, DrawingLine>(),
                objectFrames = new Dictionary<string, Frame>(),
            };
        }

        foreach (Transform child in meshGenerator.elementsParent.transform)
        {
            storedDrawings.drawings[dkey].objectFrames[child.name] = new Frame
            {
                point = child.position,
                xaxis = child.right,
                zaxis = child.forward
            };
        }

        EnabledDrawingIndex = DrawingIndex;
        lineIndex = -1;
        linePointIndex = 0;

    }

    public void HideChildren(Transform transform)
    {
        // Iterate through the children of RTSpheres
        foreach (Transform child in transform)
        {
            // Deactivate the child GameObject
            child.gameObject.SetActive(false);
        }
    }

    public void StartDrawing()
    {
        drawingOn = true;
        //Debug.Log("Drawing on a mesh is ON");
    }

    public void StopDrawing()
    {
        drawingOn = false;
        //Debug.Log("Drawing on a mesh is OFF");
    }

    public void DeleteLines()
    {
        foreach (Transform child in currentDrawingParent.transform)
        {
            // Destroy the child game object
            Destroy(child.gameObject);
        }

        lineIndex = -1;
        linePointIndex = 0;
    }

    public void UndoLine()
    {
        if (lineIndex >=0)
        {
            Debug.Log (currentDrawingParent.name + " ! Line" + lineIndex.ToString());
            currentDrawingParent.DestroyGameObjectAndChildren("line" + lineIndex.ToString(), false);
            

            // Remove the line from the dictionary
            string dkey = "drawing" + EnabledDrawingIndex.ToString();
            if (storedDrawings.drawings.ContainsKey(dkey))
            {
                string lkey = "line"+ lineIndex.ToString();
                if (storedDrawings.drawings[dkey].lines.ContainsKey(lkey))
                {
                    storedDrawings.drawings[dkey].lines.Remove(lkey);
                }
            }

            // Decrement line index and reset line point index
            lineIndex--;
            linePointIndex = -1;
        }
    }

    void AddPointsToLine(LineRenderer lineRenderer, Vector3 position)
    {
        if (lineRenderer == null)
        {
            Debug.LogError("LineRenderer is null. Cannot add points.");
            return;
        }

        Debug.Log($"Adding point at index {linePointIndex} with position {position}");

        lineRenderer.positionCount = linePointIndex + 1;

        lineRenderer.SetPosition(linePointIndex, position);

        linePointIndex++;
    }

    LineRenderer InstantiateLine(int index, string type)
    {
        LineRenderer newLine = new LineRenderer();
        
        if (type=="realtime")
        {
            lineObject = new GameObject("line" + index.ToString());
            lineObject.transform.parent = currentDrawingParent.transform;
            //lineObject.transform.position = linesParent.transform.position;
            lineObject.transform.localRotation = Quaternion.identity;
            lineObject.transform.localScale = Vector3.one;
            newLine = lineObject.AddComponent<LineRenderer>();
        }
        else
        {
            lineObjectSimplified = new GameObject("line" + index.ToString());
            lineObjectSimplified.transform.parent = currentDrawingParent.transform;
            //lineObjectSimplified.transform.position = linesParent.transform.position;
            lineObjectSimplified.transform.localRotation = Quaternion.identity;
            lineObjectSimplified.transform.localScale = Vector3.one;
            newLine = lineObjectSimplified.AddComponent<LineRenderer>();
            lineObjectSimplified.tag = "simplified";
        }

        newLine.material = lineMaterial;
        newLine.startWidth = lineWidth;
        newLine.endWidth = lineWidth;
        newLine.alignment = LineAlignment.TransformZ;
        //newLine.positionCount = 0;

        //newLine.loop = true; 
        return newLine;
    }

    void StoreDrawnLine(LineRenderer name)
    {
        if (name == null || name.positionCount == 0)
            return;

        Vector3[] positions = new Vector3[name.positionCount];
        name.GetPositions(positions);
        
        string dkey = "drawing" + DrawingIndex.ToString();

        if (!storedDrawings.drawings.ContainsKey(dkey))
        {
            Transform parent = meshGenerator.elementsParent.transform;
            storedDrawings.drawings["drawing" + DrawingIndex.ToString()] = new Drawing
            {   
                MCF = new Frame
                {
                    point = parent.position,
                    xaxis = parent.right,
                    zaxis = parent.forward
                },
                lines = new Dictionary<string, DrawingLine>()
            };
        }

        string lkey =  "line"+ lineIndex.ToString();
        
        if (!storedDrawings.drawings[dkey].lines.ContainsKey(dkey))
        {
            storedDrawings.drawings[dkey].lines[lkey] = new DrawingLine
            {
                color = new float[]
                {
                    lineMaterial.color.r,
                    lineMaterial.color.g,
                    lineMaterial.color.b,
                    lineMaterial.color.a
                },
                positions = new Vector3[positions.Length]
            };
        }
        storedDrawings.drawings[dkey].lines[lkey].positions = positions;

        storedDrawings.drawings[dkey].objectFrames = new Dictionary<string, Frame>();

        foreach (Transform child in meshGenerator.elementsParent.transform)
        {
            storedDrawings.drawings[dkey].objectFrames[child.name] = new Frame
            {
                point = child.position,
                xaxis = child.right,
                zaxis = child.forward
            };
        }

    }

    public void SaveEnabledDrawing()
    {
        // Step 1: Get the name of the file
        string dkey = "drawing" + EnabledDrawingIndex.ToString();

        SavePinPoints(dkey);
        UpdatePositionsInDictionary(dkey);
        // Step 2: Get the timestamp at that time
        string timestampDrawing = DateTime.Now.ToString("yyyyMMddHHmmss");

        // Step 3: Retrieve the value of the dictionary based on EnabledDrawingIndex
        if (storedDrawings.drawings.TryGetValue(dkey, out Drawing drawing))
        {
            // Step 4: Serialize the value of the dictionary to JSON
            string json = JsonConvert.SerializeObject(drawing);

            // Step 5: Save the JSON data to a file on the HoloLens 2 device
            string filePath = Path.Combine(Application.persistentDataPath, $"{timestamp}_{timestampDrawing}_{team}_{dkey}.json");
            // byte[] data = Encoding.ASCII.GetBytes(json);
            // UnityEngine.Windows.File.WriteAllBytes(filePath, data);
            File.WriteAllText(filePath, json);

            Debug.Log($"Drawing saved to: {filePath}");
        }
        else
        {
            Debug.LogWarning("Drawing not found in the dictionary.");
        }
    }

    public void GenerateMeshCollider(LineRenderer name)
    {
        MeshCollider collider = GetComponent<MeshCollider>();

        if (collider == null)
        {
            collider = lineObject.AddComponent<MeshCollider>();
        }


        Mesh mesh = new Mesh();
        name.BakeMesh(mesh, true);

        // if you need collisions on both sides of the line, simply duplicate & flip facing the other direction!
        // This can be optimized to improve performance ;)
        int[] meshIndices = mesh.GetIndices(0);
        int[] newIndices = new int[meshIndices.Length * 2];

        int j = meshIndices.Length - 1;
        for (int i = 0; i < meshIndices.Length; i++)
        {
            newIndices[i] = meshIndices[i];
            newIndices[meshIndices.Length + i] = meshIndices[j];
        }
        mesh.SetIndices(newIndices, MeshTopology.Triangles, 0);

        collider.sharedMesh = mesh;

        PressableButton lineButton = lineObject.AddComponent<PressableButton>();
        lineButton.OnClicked.AddListener(() => DestroyLine(lineButton.gameObject));
    }

    public void DestroyLine(GameObject gObject) 
    {
        Destroy(gObject);
    }
    
    
    void UpdatePositionsInDictionary(string dkey)
    {
        GameObject drawing = linesParent.FindObject(dkey);

        foreach (Transform child in drawing.transform)
        {
            
            if (child.CompareTag("simplified"))
            {
                string lKey = child.gameObject.name;

                CurveManipulator crvManipulator = child.gameObject.GetComponent<CurveManipulator>();
                
                if (crvManipulator == null)
                {
                    Debug.LogError("CurveManipulator component not found on: " + lKey);
                    continue;
                }

                if (crvManipulator.saved)
                {
                    continue;
                }

                if (!storedDrawings.drawings.TryGetValue(dkey, out var drawingData) || !drawingData.lines.TryGetValue(lKey, out var lineData))
                {
                    Debug.LogError("Key not found in drawings dictionary: " + dkey + " or line key: " + lKey);
                    continue;
                }

                lineData.positions = new Vector3[crvManipulator.controlPositions.Length];
                for (int i = 0; i < crvManipulator.controlPositions.Length; i++)
                {
                    lineData.positions[i] = crvManipulator.controlPositions[i];
                } 

                crvManipulator.saved = true;  
            }
        }
    }

    void SavePinPoints(string dkey)
    {
        GameObject pinsToSave = linesParent.FindObject(dkey).FindObject("pins");

        if ( storedDrawings.drawings[dkey].pinPoints ==null)
        {
            storedDrawings.drawings[dkey].pinPoints = new Dictionary<string, Frame>();
        }

        foreach (Transform child in pinsToSave.transform)
        {
            string pKey = child.gameObject.name;
            storedDrawings.drawings[dkey].pinPoints[pKey] = new Frame
            {
                point = child.position,
                xaxis = child.right,
                zaxis = child.forward
            };
        }
    }


    void OnApplicationQuit()
    {   
        if (storedDrawings.drawings.ContainsKey("drawing0")) //storedDrawings.drawings["drawing0"].lines.Count !=0)
        {
            foreach (var dKey in storedDrawings.drawings.Keys)
            {
                UpdatePositionsInDictionary(dKey);
                SavePinPoints(dKey);
            }

            // Step 2: Serialize the value of the dictionary to JSON
            string json = JsonConvert.SerializeObject(storedDrawings);

            // Step 3: Save the JSON data to a file on the HoloLens 2 device
            string filePath = Path.Combine(Application.persistentDataPath, $"{timestamp}_{team}_backup.json");
            File.WriteAllText(filePath, json);

            Debug.Log($"Drawing saved to: {filePath}");
        }

    }
}
