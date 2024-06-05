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
using MixedReality.Toolkit;

public class MeshGeneratorFromJson : MonoBehaviour
{
    public GameObject loading; // A GameObject that is disabled after the data is generated
    public GameObject elementsParent;
    public DrawingController drawController;
    private string path; 
    public string fileName; 
    public Material material;     			
    private GameObject element;

	void Start()
    {   
        elementsParent = new GameObject("Elements");
        elementsParent.SetActive(false);


        string dataFolderPath = Directory.GetParent(Application.dataPath).Parent.FullName; // Constructing the path dynamically 
        path = dataFolderPath + "/data/" + fileName + ".json";
        Debug.Log(path);


        // If it's URL, download data in a coroutine
        if (path.StartsWith("https:") || path.StartsWith("http:"))
        {
            StartCoroutine(LoadFromURL(path));
        }
        // If it's a file, load data in a thread
        else
        {
            Debug.Log ("Path: " + path);
            LoadFromJson(path);
        }
        
    }
	
	void LoadFromJson(string path) 
	{
		// Create mesh Reader
		MeshReader meshReader = new MeshReader();
		meshReader.GetJsonFromFilePath(path);

		// Make sure to lock to avoid multithreading problems
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
	
    private void Generate(MeshData data, GameObject elParent)
    {

        element = data.GenerateMesh();
        element.transform.parent = elParent.transform;
        data.AssignMaterial(element, material);
        
        drawController.meshCollider = element.AddComponent<MeshCollider>();
        
        
        var interactable =element.AddComponent<StatefulInteractable>();
        interactable.ToggleMode = StatefulInteractable.ToggleType.Toggle;
        interactable.OnToggled.AddListener(() => drawController.StartDrawing());
        interactable.OnUntoggled.AddListener(() => drawController.StopDrawing());

        drawController.meshInteractable = interactable;
	}
}
