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

public class DrawingController : MonoBehaviour
{
    public bool drawingOn = false;
    public bool farDrawing = true;
    public UIController uIController;
    public MeshGeneratorFromJson meshGenerator;
    public Drawings storedDrawings; 
    public MqttController mqttController;
    public Transform reticleTransform;
    //public GameObject reticleVisual;
    public Material lineMaterial;
    public GameObject linesParent;

    public int EnabledDrawingIndex = -1;

    private LineRenderer lineRenderer_realtime;
    private LineRenderer lineRenderer_simplified;

    private float lineWidth = 0.002f;
    private int DrawingIndex = -1;
    private int lineIndex = -1;
    private int linePointIndex = 0;
    private GameObject reticleVisual;
    private GameObject line;
    private float minDistance = 0.0025f;
    private Vector3 previousPosition = Vector3.zero;

    private GameObject currentDrawingParent;
    private HandsAggregatorSubsystem aggregator;
    private string timestamp;
    public string team;

    //curve
    public List<Vector3> originalCurvePoints = new List<Vector3>();
    int targetPointCount = 10;

    public void SendToRhino()
    {
        object message = storedDrawings.drawings["drawing" + EnabledDrawingIndex.ToString()];
        
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
        if (farDrawing)
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
                    // Initialize the line point index
                    linePointIndex = 0;
                    // Increment the line index for creating a new line
                    lineIndex++;
                    // Instantiate a new line renderer for the current line index
                    InstantiateLine(lineIndex);
                    Debug.Log("Line index is" + lineIndex + "lineRenderer" + lineRenderer_realtime);
                }

                if (Vector3.Distance(previousPosition, reticleTransform.position)> minDistance)
                {
                    Debug.Log("Position is" + reticleTransform.position);
                    AddPointsToLine(lineRenderer_realtime, reticleTransform.position);
                    lineRenderer_realtime.Simplify(0.025f);
                    originalCurvePoints.Add(reticleTransform.position);
                    previousPosition = reticleTransform.position;
                }

                
            }
            else
            {
                
                if (linePointIndex != -1 && lineIndex != -1)
                {
                    Debug.Log("origianl points are" + originalCurvePoints.Count);
                    Debug.Log("targetPointCount are" + targetPointCount);
                    List<Vector3> simplifiedCurve = SimplifyCurve(originalCurvePoints, targetPointCount);
                    InstantiateLine(targetPointCount);
                    StoreDrawnLine(lineRenderer_simplified);
                    //GenerateMeshCollider();
                }

                linePointIndex = -1;

            }
        }
        else
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
                //Debug.Log("The pinch is" + isPinching);


                if (isPinching)
                {
                    Debug.Log("Pinch is " + isPinching);
                    if (linePointIndex == -1)
                    {
                        // Set the initial position for the line
                        previousPosition = reticleTransform.position;
                        // Initialize the line point index
                        linePointIndex = 0;
                        // Increment the line index for creating a new line
                        lineIndex++;
                        // Instantiate a new line renderer for the current line index
                        lineRenderer_realtime = InstantiateLine(lineIndex);
                        Debug.Log("Line index is" + lineIndex + "lineRenderer" + lineRenderer_realtime);
                    }

                    if (Vector3.Distance(previousPosition, jointPose.Pose.position)> minDistance)
                    {
                        Debug.Log("there is a line with a material" + lineRenderer_realtime.material);
                        AddPointsToLine(lineRenderer_realtime, jointPose.Pose.position);
                        originalCurvePoints.Add(jointPose.Pose.position);
                        previousPosition = jointPose.Pose.position;
                        Debug.Log("origianl points are" + originalCurvePoints.Count);
                    }


                }
                else
                {

                    if (linePointIndex != -1)
                    {
                        Debug.Log("Pinch is" + isPinching);
                        Debug.Log("originalCurvePoints is " + originalCurvePoints.Count);

                        //Once we stop pinching we simplify
                        List<Vector3> simplifiedPoints = SimplifyCurve(originalCurvePoints, targetPointCount);
                        lineRenderer_simplified = InstantiateLine(lineIndex);
                        lineRenderer_simplified.positionCount = simplifiedPoints.Count;
                        // Set the positions of the LineRenderer
                        for (int i = 0; i < simplifiedPoints.Count; i++)
                        {
                            lineRenderer_simplified.SetPosition(i, simplifiedPoints[i]);
                        }
                        //then we store it
                        StoreDrawnLine(lineRenderer_simplified);
                        //GenerateMeshCollider();
                    }
                    linePointIndex = -1;
                }
            }
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
        farDrawing =!farDrawing;

        if (farDrawing)
        {
            uIController.ModeText.text = "DRAW mode: Hand Ray";
        }
        else
        {
            uIController.ModeText.text = "DRAW mode: Pinch Gesture";
        }

        foreach (Transform child in meshGenerator.elementsParent.transform)
        {
            child.gameObject.GetComponent<MeshCollider>().enabled=farDrawing;
            child.gameObject.GetComponent<StatefulInteractable>().enabled=farDrawing;
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
        Debug.Log("Start drawing No:" + DrawingIndex);


    }

    public void VisualizeDrawing()
    {
        HideChildren(linesParent.transform);
        EnabledDrawingIndex ++;
        if (EnabledDrawingIndex == linesParent.transform.childCount)
        {
            EnabledDrawingIndex=0;
        }
        linesParent.transform.GetChild(EnabledDrawingIndex).gameObject.SetActive(true);

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
        currentDrawingParent = new GameObject("Drawing_" + DrawingIndex);
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
        Debug.Log("Drawing on a mesh is ON");
    }

    public void StopDrawing()
    {
        drawingOn = false;
        Debug.Log("Drawing on a mesh is OFF");
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
            Destroy(currentDrawingParent.FindObject("Line" + lineIndex.ToString()));
            

            // Remove the line from the dictionary
            string dkey = "drawing" + EnabledDrawingIndex.ToString();
            if (storedDrawings.drawings.ContainsKey(dkey))
            {
                string lkey = lineIndex.ToString();
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

    LineRenderer InstantiateLine(int index)
    {
        GameObject lineObject = new GameObject("Line " + index.ToString());
        lineObject.transform.parent = currentDrawingParent.transform;
        LineRenderer newLine = lineObject.AddComponent<LineRenderer>();

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

        for (int i = 0; i < name.positionCount; i++)
        {
            positions[i] = name.GetPosition(i);
        }
        
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

        string lkey =  lineIndex.ToString();
        
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
        string drawing_name = "drawing" + EnabledDrawingIndex.ToString();

        // Step 2: Get the timestamp at that time
        string timestampDrawing = DateTime.Now.ToString("yyyyMMddHHmmss");

        // Step 3: Retrieve the value of the dictionary based on EnabledDrawingIndex
        if (storedDrawings.drawings.TryGetValue(drawing_name, out Drawing drawing))
        {
            // Step 4: Serialize the value of the dictionary to JSON
            string json = JsonConvert.SerializeObject(drawing);

            // Step 5: Save the JSON data to a file on the HoloLens 2 device
            string filePath = Path.Combine(Application.persistentDataPath, $"{timestamp}_{timestampDrawing}_{team}_{drawing_name}.json");
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
            collider = line.AddComponent<MeshCollider>();
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

        PressableButton lineButton = line.AddComponent<PressableButton>();
        lineButton.OnClicked.AddListener(() => DestroyLine(lineButton.gameObject));
    }

    public void DestroyLine(GameObject gObject) 
    {
        Destroy(gObject);
    }
    

    void OnApplicationQuit()
    {   
        if (storedDrawings.drawings.ContainsKey("drawing0")) //storedDrawings.drawings["drawing0"].lines.Count !=0)
        {
            // Step 2: Serialize the value of the dictionary to JSON
            string json = JsonConvert.SerializeObject(storedDrawings);

            // Step 3: Save the JSON data to a file on the HoloLens 2 device
            string filePath = Path.Combine(Application.persistentDataPath, $"{timestamp}_{team}_backup.json");
            File.WriteAllText(filePath, json);

            Debug.Log($"Drawing saved to: {filePath}");
        }

    }
}
