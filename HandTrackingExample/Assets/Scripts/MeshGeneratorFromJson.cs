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
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;
using System.Text.RegularExpressions;

public class MeshGeneratorFromJson : MonoBehaviour
{
    public GameObject loading; // A GameObject that is disabled after the data is generated
    [HideInInspector] public GameObject elementsParent;
    [HideInInspector] public Stock stock;
    [HideInInspector] public GameObject locksParent;
    public DrawingController drawController;
    public GameObject padlocks;
    private string path; 
    private string path2;
    public string fileName;
    public string fileNameStock; 
    public Material material;     			
    private GameObject element;
    private float alphaValue = 0.20f;

    [HideInInspector] public GameObject inventoryParent;
    [HideInInspector] public GameObject detailsParent;
    public Material[] materials;
    public Inventory inventory;
    private string folderpath =""; // "C:\\Users\\eleni\\Documents\\GitHub\\IntuitiveRobotics-AugmentedTechnologies\\HandTracking\\data\\" ;
    private string library =  ""; //"C:\\Users\\eleni\\Documents\\GitHub\\IntuitiveRobotics-AugmentedTechnologies\\HandTracking\\data\\material_library.json" ;

	void Start()
    {   
        elementsParent = new GameObject("Elements");
        locksParent = new GameObject("Locks");
        locksParent.SetActive(true);
        elementsParent.SetActive(false);

        string dataFolderPath =Application.dataPath; // Constructing the path dynamically 
        path = dataFolderPath + "/Data/" + fileName + ".json";
        path2 = dataFolderPath + "/Data/" + fileNameStock + ".json";
        //string dataFolderPath = Directory.GetParent(Application.dataPath).Parent.FullName; // Constructing the path dynamically 
        //path = dataFolderPath + "/data/" + fileName + ".json";

        Debug.Log ("Path: " + path);
        // LoadFromJson(path);
    
        // string jsonContent = File.ReadAllText(path2);
        // stock = JsonConvert.DeserializeObject<Stock>(jsonContent);

        folderpath = dataFolderPath + "/Data/" ;
        library = folderpath  + "material_library" + ".json";

        inventoryParent = new GameObject("Inventory");

        LoadInventoryFromJson(library);

        detailsParent = Instantiate(inventoryParent);
        detailsParent.name = "DetailedView";
        CorrectDetailed();

        detailsParent.SetActive(false);
    }

    void LoadInventoryFromJson(string path) 
	{
		// Create mesh Reader
		MeshReader meshReader = new MeshReader();
		meshReader.GetInventoryFromFilePath(path);
        inventory = meshReader.libraryData;

        for(int i=0; i< inventory.priority.Length; i++)
        {
            string name = inventory.priority[i];
            Match match = Regex.Match(name, @"^[A-Za-z]+_\d+");

            if (match.Success)
            {
                string elementpath = Path.Combine(folderpath, match.Value + ".json"); 
                Debug.Log(match.Value);
                meshReader.GetMemberFromFilePath(elementpath);
                inventory.members[match.Value] = meshReader.memberData; 
                GenerateMember(meshReader.memberData, match.Value, inventoryParent);
            }
        }

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
	
    public void GenerateMultiple(MultipleMeshesData data, GameObject elParent)
    {
        foreach (var elementPair in data.elements) // Loop through each element in the dictionary
        {
            MeshData meshData = elementPair.Value;
            Generate( meshData, elParent);
        }
    }

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
            alphaValue =alphaValue + 0.1f;
        }
        else if (!transparencyUp && alphaValue>0.05f)
        {
            alphaValue =alphaValue - 0.1f;
        }

        infoText.text = Mathf.RoundToInt(alphaValue * 100).ToString() + "%";


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

    public void GenerateMember(MemberData memberData, string name, GameObject elParent)
    {
        element = memberData.mesh.GenerateMesh();
        element.name = name;
        element.transform.parent = elParent.transform;

        foreach (Material mat in materials)
        {
            if (mat != null && mat.name == name)
            {
                memberData.mesh.AssignMaterial(element, mat);
                break; 
            }
        }
        element.AddComponent<MeshCollider>();

        TimberElement timberEl = element.AddComponent<TimberElement>();
        timberEl.types = memberData.types;
        timberEl.width = memberData.dimensions.length;
        timberEl.height = memberData.dimensions.height;
        timberEl.length = memberData.dimensions.width;
        timberEl.segments = memberData.Vector3Parts();
        timberEl.MarkDefects();


        var interactable =element.AddComponent<StatefulInteractable>();
        interactable.ToggleMode = StatefulInteractable.ToggleType.Toggle;
        interactable.OnToggled.AddListener(() => drawController.StartDrawing());
        interactable.OnUntoggled.AddListener(() => drawController.StopDrawing());


        GameObject lockInstance = Instantiate(padlocks);


        //Grab world-space bounds from the Renderer
        Bounds worldBounds = element.GetComponent<Renderer>().bounds;
        Vector3 worldCenter = worldBounds.center;
        Vector3 worldSize   = worldBounds.size;

        // Convert them to element-local coordinates
        Vector3 localCenter = element.transform.InverseTransformPoint(worldCenter);
        Vector3 localSize   = element.transform.InverseTransformVector(worldSize);

        // Adjust local offset to be "above" the item
        Vector3 localOffset = new Vector3(
            localCenter.x,
            localCenter.y + (localSize.y / 2.0f) + 0.075f,
            localCenter.z
        );

        // 4) Assign to the lock's Orbital LocalOffset
        lockInstance.GetComponent<Orbital>().LocalOffset = localOffset;

        lockInstance.name ="lock_" + element.name;
        lockInstance.transform.parent = locksParent.transform;
        SolverHandler lockSolver = lockInstance.GetComponent<SolverHandler>();
        lockSolver.TrackedTargetType = TrackedObjectType.CustomOverride;
        lockSolver.TransformOverride = element.transform;
        lockInstance.GetComponent<ElementStateController>().target = element;

        element.transform.localPosition = Vector3.zero;
        element.transform.localRotation = Quaternion.identity;

        lockInstance.SetActive(false);
        element.SetActive(false); 
	}

    void CorrectDetailed()
    {
        foreach (Transform child in detailsParent.transform)
        {
            var interactable =child.GetComponent<StatefulInteractable>();
            var timberEl =child.GetComponent<TimberElement>();
            if (interactable != null)
            {
                Destroy(interactable);
            }
            if (timberEl!= null)
            {
                Destroy(timberEl);
            }

            interactable =child.AddComponent<StatefulInteractable>();
            interactable.ToggleMode = StatefulInteractable.ToggleType.Button;
            
            UIController uiController = GameObject.Find("UIController").GetComponent<UIController>();
            interactable.OnClicked.AddListener(() => 
            {
                uiController.arrow.SetActive(true);
                uiController.detailElement= child.name;
                uiController.SetDetailElement();

                GameObject elementToTrack = child.gameObject;

                Bounds worldBounds = elementToTrack.GetComponent<Renderer>().bounds;
                Vector3 worldCenter = worldBounds.center;
                Vector3 worldSize   = worldBounds.size;

                // Convert them to element-local coordinates
                Vector3 localCenter = elementToTrack.transform.InverseTransformPoint(worldCenter);
                Vector3 localSize   = elementToTrack.transform.InverseTransformVector(worldSize);

                // Adjust local offset to be "above" the item
                Vector3 localOffset = new Vector3(
                    localCenter.x,
                    localCenter.y + (localSize.y / 2.0f) + 0.075f,
                    localCenter.z
                );

                // 4) Assign to the lock's Orbital LocalOffset
                var arrowOrbital = uiController.arrow.GetComponent<Orbital>();
                arrowOrbital.LocalOffset = localOffset;

                var Arsolver = uiController.arrow.GetComponent<SolverHandler>();
                Arsolver.TrackedTargetType = TrackedObjectType.CustomOverride;
                Arsolver.TransformOverride = elementToTrack.transform;
            });


        }
    }

}
