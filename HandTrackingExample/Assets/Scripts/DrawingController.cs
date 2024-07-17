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
using Newtonsoft.Json;
using MixedReality.Toolkit.UX;
using MixedReality.Toolkit.SpatialManipulation;

public class DrawingController : MonoBehaviour
{
    public bool drawingOn = false;
    public bool pointsOn = false;
    private bool farDrawing = false;

    public string drawingMode = "farDrawing";
    public UIController uIController;
    public MeshGeneratorFromJson meshGenerator;
    public Drawings storedDrawings; 
    public MqttController mqttController;
    public Transform reticleTransform;
    //public GameObject reticleVisual;
    public Material lineMaterial;
    public GameObject linesParent;

    public int EnabledDrawingIndex = -1;

    private LineRenderer lineRenderer;
    private LineRenderer lineRenderer_realtime;
    private LineRenderer lineRenderer_simplified;

    public Material ControlPointMaterial;

    private float simplifyingFactor = 0.02f;
    private float lineSegmentSize = 0.01f;
    private float gizmoSize = 0.0035f;

    private float lineWidth = 0.002f;
    private int DrawingIndex = -1;
    private int lineIndex = -1;
    private int linePointIndex = 0;
    private GameObject reticleVisual;
    private GameObject lineObject;
    private GameObject lineObjectSimplified;
    private float minDistance = 0.0025f;
    private Vector3 previousPosition = Vector3.zero;

    private GameObject currentDrawingParent;
    private HandsAggregatorSubsystem aggregator;
    private string timestamp;

    public string team;


    int targetPointCount = 10;

    public void SendToRhino()
    {
        string dkey = "drawing" + EnabledDrawingIndex.ToString();
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

                    SimplifyDrawing();
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
                //Debug.Log("The pose of the joint is" + jointIsValid + jointPose);

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
                        SimplifyDrawing();
                        //GenerateMeshCollider();
                    }
                    linePointIndex = -1;
                }
            }
        }
        
    }

    void SimplifyDrawing()
    {
        lineRenderer_simplified = InstantiateLine(lineIndex, "simplified");
                        
        Vector3[] originalPositions = new Vector3[lineRenderer_realtime.positionCount];
        lineRenderer_realtime.GetPositions(originalPositions);
        lineRenderer_simplified.positionCount = originalPositions.Length;
        lineRenderer_simplified.SetPositions(originalPositions);

        lineObject.SetActive(false);

        // Calculate the total length of the line
        float totalLength = CalculateTotalLength(originalPositions);

        // Check if the total length exceeds the threshold and adjust parameters
        if (totalLength > 0.5f)
        {
            simplifyingFactor = 0.02f;
            lineSegmentSize = 0.01f;
            gizmoSize = 0.0035f;
        }
        else
        {
            simplifyingFactor = 0.01f;
            lineSegmentSize = 0.005f;
            gizmoSize = 0.0035f; 
        }
        
        lineRenderer_simplified.Simplify(simplifyingFactor);
        //then we store it
        StoreDrawnLine(lineRenderer_simplified);

        CreateCurvedLinePoints(lineRenderer_simplified, lineObjectSimplified);
        lineObjectSimplified.AddComponent<CurvedLineRenderer>();
        lineObjectSimplified.GetComponent<CurvedLineRenderer>().lineSegmentSize =lineSegmentSize;
        lineObjectSimplified.GetComponent<CurvedLineRenderer>().gizmoSize = gizmoSize;
}

    float CalculateTotalLength(Vector3[] positions)
    {
        float length = 0.0f;
        for (int i = 0; i < positions.Length - 1; i++)
        {
            length += Vector3.Distance(positions[i], positions[i + 1]);
        }
        return length;
    }

    void CreateCurvedLinePoints(LineRenderer lineRenderer, GameObject parentObject)
    {
        // Get the positions from the simplified LineRenderer
        Vector3[] simplifiedPositions = new Vector3[lineRenderer.positionCount];
        lineRenderer.GetPositions(simplifiedPositions);

        // For each position, create a new GameObject and attach a CurvedLinePoint component
        foreach (Vector3 position in simplifiedPositions)
        {
            GameObject pointObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pointObject.transform.position = position;
            pointObject.transform.localScale = new Vector3(gizmoSize * 2, gizmoSize * 2, gizmoSize * 2);
            pointObject.transform.parent = parentObject.transform;
            pointObject.name = "ControlPoint";

            pointObject.GetComponent<Renderer>().material = ControlPointMaterial;

            ObjectManipulator targetObjectManipulator = pointObject.AddComponent<ObjectManipulator>();
            targetObjectManipulator.AllowedManipulations = TransformFlags.Move | TransformFlags.Rotate;
            targetObjectManipulator.AllowedInteractionTypes = InteractionFlags.Near | InteractionFlags.Ray | InteractionFlags.Gaze | InteractionFlags.Generic;
            


            CurvedLinePoint curvedLinePoint = pointObject.AddComponent<CurvedLinePoint>();
            pointObject.GetComponent<Renderer>().enabled = false;
            targetObjectManipulator.enabled = false; 
        }
    }

    List<Vector3> SimplifyCurve(List<Vector3> points, int targetCount)
    {
        if (points.Count <= targetCount)
        {
            return new List<Vector3>(points);
        }

        List<Vector3> simplifiedPoints = new List<Vector3>(points);
        while (simplifiedPoints.Count > targetCount)
        {
            simplifiedPoints = SimplifyOnce(simplifiedPoints);
        }

        return simplifiedPoints;
    }

    List<Vector3> SimplifyOnce(List<Vector3> points)
    {
        List<Vector3> result = new List<Vector3>();
        for (int i = 0; i < points.Count; i++)
        {
            if (i % 2 == 0)
            {
                result.Add(points[i]);
            }
        }

        return result;
    }

    public void ToggleDrawingMode()
    {

        if (drawingMode =="farDrawing")
        {
            drawingMode ="pinchDrawing";
            pointsOn = false;
            farDrawing = false;
            uIController.ModeText.text = "DRAW mode: Pinch Gesture";
        }
        else if (drawingMode =="pinchDrawing")
        {
            drawingMode ="controlPoints";
            pointsOn = true;
            farDrawing = false;
            uIController.ModeText.text = "DRAW mode: Control Points";

            EnableControlPoints(pointsOn);
        }
        else
        {
            drawingMode ="farDrawing";
            pointsOn = false;
            farDrawing = true;
            uIController.ModeText.text = "DRAW mode: Hand Ray";
            EnableControlPoints(pointsOn);
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
        Debug.Log(currentDrawingParent.name + enable );
        foreach (Transform child in currentDrawingParent.transform)
        {
            // Check if the child GameObject has the specific tag you are interested in
            if (child.CompareTag("simplified"))
            {
                foreach (Transform grandchild in child.transform)
            {
                grandchild.gameObject.GetComponent<Renderer>().enabled = enable;
                grandchild.gameObject.GetComponent<ObjectManipulator>().enabled = enable;
            }
            }
        }
    }

    public void StartNewDrawing()
    {
        HideChildren(linesParent.transform);

        string dkey = "drawing" + DrawingIndex.ToString();

        
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
        lineIndex = (currentDrawingParent.transform.childCount /2 ) -1;
        currentDrawingParent.SetActive(true);
        EnableControlPoints(pointsOn);

        foreach (var kvp in storedDrawings.drawings["drawing"+ EnabledDrawingIndex.ToString()].objectFrames)
        {
            string object_name = kvp.Key;
            Frame object_frame = kvp.Value;

            GameObject gobject = meshGenerator.elementsParent.FindObject(object_name);
            gobject.Orient(object_frame);
        }

    }

    public void CreateDrawingParent()
    {
        DrawingIndex++;
        
        uIController.DrawingText.text = "Drawing No: " + DrawingIndex.ToString() + "    |";
        currentDrawingParent = new GameObject("drawing" + DrawingIndex);
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
            newLine = lineObject.AddComponent<LineRenderer>();
        }
        else
        {
            lineObjectSimplified = new GameObject("line" + index.ToString());
            lineObjectSimplified.transform.parent = currentDrawingParent.transform;
            newLine = lineObjectSimplified.AddComponent<LineRenderer>();
            lineObjectSimplified.tag = "simplified";
        }

        newLine.material = lineMaterial;
        newLine.startWidth = lineWidth;
        newLine.endWidth = lineWidth;
        newLine.positionCount = 0;

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

                CurvedLineRenderer cLine = child.gameObject.GetComponent<CurvedLineRenderer>();
                
                if (cLine == null)
                {
                    Debug.LogError("CurvedLineRenderer component not found on: " + lKey);
                    continue;
                }

                if (cLine.saved)
                {
                    continue;
                }

                if (!storedDrawings.drawings.TryGetValue(dkey, out var drawingData) || !drawingData.lines.TryGetValue(lKey, out var lineData))
                {
                    Debug.LogError("Key not found in drawings dictionary: " + dkey + " or line key: " + lKey);
                    continue;
                }

                lineData.positions = new Vector3[cLine.linePositions.Length];
                for (int i = 0; i < cLine.linePositions.Length; i++)
                {
                    lineData.positions[i] = cLine.linePositions[i];
                } 

                cLine.saved = true;  
            }
        }
    }

     
    void OnApplicationQuit()
    {   
        if (storedDrawings.drawings.ContainsKey("drawing0")) //storedDrawings.drawings["drawing0"].lines.Count !=0)
        {
            foreach (var dKey in storedDrawings.drawings.Keys)
            {
                UpdatePositionsInDictionary(dKey);
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
