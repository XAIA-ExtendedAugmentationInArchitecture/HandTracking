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

public class DrawingController : MonoBehaviour
{
    public bool drawingOn = false;
    public bool farDrawing = true;

    public GameObject Geometry;
    public Drawings storedDrawings; 
    public MqttPublisher mqttPublisher;
    public Transform reticleTransform;
    public MeshCollider meshCollider;
    public StatefulInteractable meshInteractable;
    public Material lineMaterial;
    public GameObject linesParent;
    public GameObject recordStateIndicator;
    public TextMeshPro DrawingText;

    public TextMeshPro ModeText;
    private int EnabledDrawingIndex = -1;
    private LineRenderer lineRenderer;

    private float lineWidth = 0.002f;
    private int DrawingIndex = -1;
    private int lineIndex = -1;
    private int linePointIndex = 0;
    private int print = 0;
    private GameObject reticleVisual;
    private GameObject line;
    private float minDistance = 0.0025f;
    private Vector3 previousPosition = Vector3.zero;

    private GameObject currentDrawingParent;

    private HandsAggregatorSubsystem aggregator;
    
    public void SendToRhino()
    {
        mqttPublisher.PublishMessage("/kit_handtracking_drawings/", storedDrawings.drawings["drawing" + EnabledDrawingIndex.ToString()]);
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
        storedDrawings = new Drawings
        {
            uid = meshCollider.gameObject.name,
            drawings = new Dictionary<string, Drawing>()
        };

        reticleVisual = reticleTransform.gameObject.transform.Find("RingVisual").gameObject;
        recordStateIndicator.SetActive(false);
        CreateDrawingParent();

        aggregator = XRSubsystemHelpers.GetFirstRunningSubsystem<HandsAggregatorSubsystem>();

        if (aggregator != null)
        {
            // If the aggregator is available, enable functionality
            StartCoroutine(EnableWhenSubsystemAvailable());
        }
        ModeText.text = "DRAW mode: Hand Ray";
    }

    void Update()
    {   
        if (farDrawing)
        {
            if (reticleVisual.activeSelf==false)
            {
                meshInteractable.ForceSetToggled(false);
            }
            if (drawingOn)
            {
                if (linePointIndex == -1)
                {
                    previousPosition = reticleTransform.position;
                    linePointIndex = 0;
                    lineIndex++;
                    InstantiateLine(lineIndex);
                }

                if (Vector3.Distance(previousPosition, reticleTransform.position)> minDistance)
                {
                    AddPointsToLine(reticleTransform.position);
                    previousPosition = reticleTransform.position;
                }

                recordStateIndicator.SetActive(true);
                
            }
            else
            {
                
                if (linePointIndex != -1 && lineIndex != -1)
                {
                    StoreDrawnLine();
                    //GenerateMeshCollider();
                }
                recordStateIndicator.SetActive(false);

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
                Debug.Log("The pose of the joint is" + jointIsValid + jointPose);

                // Query pinch characteristics from the left hand.
                bool handIsValidPinch = aggregator.TryGetPinchProgress(XRNode.RightHand, out bool isReadyToPinch, out bool isPinching, out float pinchAmount);
                Debug.Log("The pinch is" + isPinching);


                if (isPinching )
                {
                    if (linePointIndex == -1)
                    {
                        previousPosition = jointPose.Pose.position;
                        linePointIndex = 0;
                        lineIndex++;
                        InstantiateLine(lineIndex);
                    }

                    if (Vector3.Distance(previousPosition, jointPose.Pose.position)> minDistance)
                    {
                        AddPointsToLine(jointPose.Pose.position);
                        previousPosition = jointPose.Pose.position;
                    }

                    recordStateIndicator.SetActive(true);

                }
                else
                {
                    if (linePointIndex != -1)
                    {
                        StoreDrawnLine();
                        //GenerateMeshCollider();
                    }
                    recordStateIndicator.SetActive(false);
                    linePointIndex = -1;
                }
            }
        }
        
    }

    public void ToggleDrawingMode()
    {
        farDrawing =!farDrawing;

        if (farDrawing)
        {
            meshInteractable.enabled=true;
            meshCollider.enabled=true;
            ModeText.text = "DRAW mode: Hand Ray";
        }
        else
        {
            ModeText.text = "DRAW mode: Pinch Gesture";
            meshInteractable.enabled=false;
            meshCollider.enabled=false;
        }

    }

    public void StartNewDrawing()
    {
        HideChildren(linesParent.transform);
        Debug.Log("Start drawing No:" + DrawingIndex);
        CreateDrawingParent();


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
    }

    void CreateDrawingParent()
    {
        DrawingIndex++;
        
        DrawingText.text = "Drawing No: " + DrawingIndex.ToString() + "    |";
        currentDrawingParent = new GameObject("Drawing_" + DrawingIndex);
        currentDrawingParent.transform.parent = linesParent.transform;

        EnabledDrawingIndex = DrawingIndex;
        lineIndex = -1;
        linePointIndex = 0;
        print=0;

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
        foreach (Transform child in linesParent.transform)
        {
            // Destroy the child game object
            Destroy(child.gameObject);
        }

        lineIndex = -1;
        linePointIndex = 0;
        print=0;
    }

    void AddPointsToLine(Vector3 position)
    {
        lineRenderer.positionCount = linePointIndex + 1;

        lineRenderer.SetPosition(linePointIndex, position);

        linePointIndex++;
    }

    void InstantiateLine(int index)
    {
        line = new GameObject("Line" + index.ToString());
        line.transform.parent = currentDrawingParent.transform;
        lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.positionCount =0;
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
    }

    void StoreDrawnLine()
    {
        if (lineRenderer == null || lineRenderer.positionCount == 0)
            return;

        Vector3[] positions = new Vector3[lineRenderer.positionCount];

        for (int i = 0; i < lineRenderer.positionCount; i++)
        {
            positions[i] = lineRenderer.GetPosition(i);
        }

        if (!storedDrawings.drawings.ContainsKey("drawing" + DrawingIndex.ToString()))
        {
            storedDrawings.drawings["drawing" + DrawingIndex.ToString()] = new Drawing
            {
                frame = new Frame
                {
                    point = Geometry.transform. position,
                    xaxis = Geometry.transform.right,
                    zaxis = Geometry.transform.forward
                },
                lines = new Dictionary<string, Vector3[]>()
            };
        }

        storedDrawings.drawings["drawing" + DrawingIndex.ToString()].lines["line" + lineIndex.ToString()] = positions;
    }

    public void SaveEnabledDrawing()
    {
        // Step 1: Get the name of the file
        string name = storedDrawings.uid;
        string drawing_name = "drawing" + EnabledDrawingIndex;

        // Step 2: Get the timestamp at that time
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        // Step 3: Retrieve the value of the dictionary based on EnabledDrawingIndex
        if (storedDrawings.drawings.TryGetValue("drawing" + EnabledDrawingIndex.ToString(), out Drawing drawing))
        {
            // Step 4: Serialize the value of the dictionary to JSON
            string json = JsonConvert.SerializeObject(drawing);

            // Step 5: Save the JSON data to a file on the HoloLens 2 device
            string filePath = Path.Combine(Application.persistentDataPath, $"{timestamp}_{name}_{drawing_name}.json");
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
    public void Print()
    {
        Debug.Log("Hello" + print);
        print++;
    }


    public void GenerateMeshCollider()
    {
        MeshCollider collider = GetComponent<MeshCollider>();

        if (collider == null)
        {
            collider = line.AddComponent<MeshCollider>();
        }


        Mesh mesh = new Mesh();
        lineRenderer.BakeMesh(mesh, true);

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

        // line.AddComponent
    }

    void OnApplicationQuit()
    {
        //Step 1: Get the name of the file
        string name = storedDrawings.uid;
        string drawing_name = "drawing" + EnabledDrawingIndex;

        // Step 2: Get the timestamp at that time
        string timestamp = DateTime.Now.ToString("yyyyMMddHHmmss");

        // Step 4: Serialize the value of the dictionary to JSON
        string json = JsonConvert.SerializeObject(storedDrawings);

        // Step 5: Save the JSON data to a file on the HoloLens 2 device
        string filePath = Path.Combine(Application.persistentDataPath, $"{timestamp}_{name}_backup.json");
        File.WriteAllText(filePath, json);

        Debug.Log($"Drawing saved to: {filePath}");
    }
}
