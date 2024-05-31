using MixedReality.Toolkit.Subsystems;
using MixedReality.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.OpenXR.Input;
using TMPro;

public class DrawingController : MonoBehaviour
{
    public bool drawingOn = false;
    public Transform reticleTransform;
    public StatefulInteractable MeshInteractable;
    public Material lineMaterial;
    public GameObject linesParent;
    public GameObject recordStateIndicator;
    private LineRenderer lineRenderer;
    private float lineWidth = 0.002f;
    private int DrawingIndex = -1;
    private int lineIndex = -1;
    private int linePointIndex = 0;
    private int print=0;
    private GameObject reticleVisual;
    private GameObject line;
    private float minDistance = 0.01f;
    private Vector3 previousPosition = Vector3.zero;

    private GameObject currentDrawingParent;
    public TextMeshProUGUI buttonText;
    

    void Start()
    {
        reticleVisual = reticleTransform.gameObject.transform.Find("RingVisual").gameObject;
        recordStateIndicator.SetActive(false);
        CreateDrawingParent();
    }

    void Update()
    {
        if (reticleVisual.activeSelf==false)
        {
            MeshInteractable.ForceSetToggled(false);
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
                AddPointsToLine(reticleTransform);
                previousPosition = reticleTransform.position;
            }

            recordStateIndicator.SetActive(true);
            
        }
        else
        {
            // if (linePointIndex != -1)
            // {
            //     GenerateMeshCollider();
            // }
            recordStateIndicator.SetActive(false);

            linePointIndex = -1;

        }
        
    }

    public void StartNewDrawing()
    {
        HideChildren(linesParent.transform);
        Debug.Log("Start drawing No:" + DrawingIndex);
        CreateDrawingParent();
        Debug.Log("info button text" + buttonText.text);


    }

    void CreateDrawingParent()
    {
        DrawingIndex++;
        currentDrawingParent = new GameObject("Drawing_" + DrawingIndex);
        currentDrawingParent.transform.parent = linesParent.transform;

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

    void AddPointsToLine(Transform reticleTransform)
    {
        lineRenderer.positionCount = linePointIndex + 1;

        lineRenderer.SetPosition(linePointIndex, reticleTransform.position);

        linePointIndex++;
    }

    void InstantiateLine(int index)
    {
        line = new GameObject("Line" + index.ToString());
        line.transform.parent = currentDrawingParent.transform;
        lineRenderer = line.AddComponent<LineRenderer>();
        lineRenderer.material = lineMaterial;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth;
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


}
