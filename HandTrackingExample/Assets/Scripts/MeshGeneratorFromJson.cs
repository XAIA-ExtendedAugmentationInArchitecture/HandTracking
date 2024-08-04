using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.Dynamic;
using MeshElementData;
using Unity.VisualScripting;
using TMPro;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;

public class MeshGeneratorFromJson : MonoBehaviour
{
    public GameObject loading; // A GameObject that is disabled after the data is generated
    [HideInInspector] public GameObject elementsParent;
    [HideInInspector] public GameObject locksParent;
    public DrawingController drawController;
    public GameObject padlocks;
    private string path; 
    public string fileName; 
    public Material material;     			
    private GameObject element;
    private float alphaValue = 0.20f;

	void Start()
    {   
        elementsParent = new GameObject("Elements");
        locksParent = new GameObject("Locks");
        locksParent.SetActive(false);
        elementsParent.SetActive(false);

        string dataFolderPath =Application.dataPath; // Constructing the path dynamically 
        path = dataFolderPath + "/Data/" + fileName + ".json";
        //string dataFolderPath = Directory.GetParent(Application.dataPath).Parent.FullName; // Constructing the path dynamically 
        //path = dataFolderPath + "/data/" + fileName + ".json";

        Debug.Log ("Path: " + path);
        LoadFromJson(path);
    
        
    }
	
	void LoadFromJson(string path) 
	{
		// Create mesh Reader
		MeshReader meshReader = new MeshReader();
		meshReader.GetJsonFromFilePath(path);

		Generate(meshReader.data, elementsParent);
	}

	IEnumerator LoadFromURL(string url)
    {
        UnityWebRequest www = UnityWebRequest.Get(url);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.ConnectionError || www.result == UnityWebRequest.Result.ProtocolError)
        {
            Debug.Log(www.error);
        }
        else
        {
            // Retrieve results as binary data
            byte[] byteArray = www.downloadHandler.data;

            // Convert byte array to string
            string jsonString = Encoding.UTF8.GetString(byteArray);

            // Create mesh reader
            MeshReader meshReader = new ();
			meshReader.GetDataFromString(jsonString);

            Generate(meshReader.data, elementsParent);
            
            
        }
    }

		
	IEnumerator AfterLoading() {
		if(loading != null)
		loading.SetActive(false);
		
		yield return null;
	}

    // To dispatch coroutines
	public readonly Queue<Action> ExecuteOnMainThread = new ();
	
    public void Generate(MeshData data, GameObject elParent)
    {

        element = data.GenerateMesh();
        element.transform.parent = elParent.transform;

        Material uniqueMaterial = new Material(material);
        uniqueMaterial.name = element.name;
        data.AssignMaterial(element, uniqueMaterial);

        Color elColor= new Color(data.color[0], data.color[1], data.color[2], alphaValue);
        element.SetColor(elColor);
        
        element.AddComponent<MeshCollider>();
        
        var interactable =element.AddComponent<StatefulInteractable>();
        interactable.ToggleMode = StatefulInteractable.ToggleType.Toggle;
        interactable.OnToggled.AddListener(() => drawController.StartDrawing());
        interactable.OnUntoggled.AddListener(() => drawController.StopDrawing());


        GameObject lockInstance = Instantiate(padlocks);

        Renderer renderer = element.GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        lockInstance.GetComponent<Orbital>().LocalOffset = new Vector3(center[0], center[1]+ size[1]/2 + 0.075f , center[2]);
        
        lockInstance.name ="lock_" + element.name;
        lockInstance.transform.parent = locksParent.transform;
        SolverHandler lockSolver = lockInstance.GetComponent<SolverHandler>();
        lockSolver.TrackedTargetType = TrackedObjectType.CustomOverride;
        lockSolver.TransformOverride = element.transform;
        lockInstance.GetComponent<ElementStateController>().target = element;

        element.transform.localPosition = Vector3.zero;
        element.transform.localRotation = Quaternion.identity;
	}

    public void AdjustTransparency(bool transparencyUp, TMP_Text infoText)
    {
        if (transparencyUp && alphaValue<0.95f)
        {
           Debug.Log ("Hoii2" + alphaValue);
            alphaValue =alphaValue + 0.1f;
        }
        else if (!transparencyUp && alphaValue>0.05f)
        {
            Debug.Log ("Hoii3" + alphaValue);
            alphaValue =alphaValue - 0.1f;
        }

        infoText.text = Mathf.RoundToInt(alphaValue * 100).ToString() + "%";

        Debug.Log("Hoiii" + infoText.text);

        foreach (Transform child in elementsParent.transform)
        {
            MeshRenderer mRenderer= child.gameObject.GetComponent<MeshRenderer>();
            
            if (mRenderer != null)
            {
                foreach (Material mat in mRenderer.materials)
                {
                    Color color = mat.color;
                    color.a = alphaValue;
                    mat.color = color;
                }
            }
        }
    }

    // public void AdjustTransparency(float transparencyValue)
    // {
    //     alphaValue = transparencyValue;
    //     foreach (Transform child in elementsParent.transform)
    //     {
    //         MeshRenderer mRenderer= child.gameObject.GetComponent<MeshRenderer>();
            
    //         if (mRenderer != null)
    //         {
    //             foreach (Material mat in mRenderer.materials)
    //             {
    //                 Color color = mat.color;
    //                 color.a = alphaValue;
    //                 mat.color = color;
    //             }
    //         }
    //     }
    // }
}
