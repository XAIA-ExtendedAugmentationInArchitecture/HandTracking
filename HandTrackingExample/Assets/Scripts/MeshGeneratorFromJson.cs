// using System;
// using System.Collections;
// using System.Collections.Generic;
// using System.Threading;
// using UnityEngine;
// using System.IO;
// using UnityEngine.Networking;
// using System.Text;
// using Newtonsoft.Json;
// using System.Dynamic;
// using MeshElementData;
// using Unity.VisualScripting;
// using UnityEngine.XR.Interaction.Toolkit;
// using TMPro;
// using MixedReality.Toolkit;
// using MixedReality.Toolkit.SpatialManipulation;
// using System.Text.RegularExpressions;

// public class MeshGeneratorFromJson : MonoBehaviour
// {
//     public GameObject loading; // A GameObject that is disabled after the data is generated
//     [HideInInspector] public GameObject elementsParent;
//     [HideInInspector] public Stock stock;
//     [HideInInspector] public GameObject locksParent;
//     public DrawingController drawController;
//     public UIController uiController;
//     public GameObject padlocks;
//     private string path; 
//     public string fileName;
//     public string fileNameStock; 
//     public Material material;     			
//     private GameObject element;
//     private float alphaValue = 0.20f;

//     [HideInInspector] public GameObject inventoryParent;
//     public Material[] materials;
//     public Inventory inventory;
//     private string folderpath =""; 
//     private string library =  ""; 

// 	void Start()
//     {   
//         elementsParent = new GameObject("Elements");
//         locksParent = new GameObject("Locks");
//         locksParent.SetActive(true);
//         elementsParent.SetActive(false);

//         // string dataFolderPath =Application.dataPath; // Constructing the path dynamically 
//         // path = dataFolderPath + "/Data/" + fileName + ".json";

//         string dataFolderPath = Application.streamingAssetsPath;
//         path = Path.Combine(dataFolderPath, fileName + ".json");


//         Debug.Log ("Path: " + path);
//         // LoadFromJson(path);
    

//         // folderpath = dataFolderPath + "/Data/" ;
//         // library = folderpath  + "material_library" + ".json";

//         folderpath = dataFolderPath;  // no /Data needed anymore
//         library = Path.Combine(folderpath, "material_library.json");


//         inventoryParent = new GameObject("Inventory");

//         LoadInventoryFromJson(library);

//     }

//     // void LoadInventoryFromJson(string path) 
// 	// {
// 	// 	// Create mesh Reader
// 	// 	MeshReader meshReader = new MeshReader();
// 	// 	meshReader.GetInventoryFromFilePath(path);
//     //     inventory = meshReader.libraryData;

//     //     for(int i=0; i< inventory.priority.Length; i++)
//     //     {
//     //         string name = inventory.priority[i];
//     //         Match match = Regex.Match(name, @"^[A-Za-z]+_\d+");

//     //         if (match.Success)
//     //         {
//     //             string elementpath = Path.Combine(folderpath, match.Value + ".json"); 
//     //             Debug.Log(match.Value);
//     //             meshReader.GetMemberFromFilePath(elementpath);
//     //             inventory.members[match.Value] = meshReader.memberData; 
//     //             GenerateMember(meshReader.memberData, match.Value, inventoryParent);
//     //         }
//     //     }

//     //     // Arrange members in a column
//     //     float yOffset = 0;
//     //     float zoffset = 0;
//     //     float spacing = 0.25f; // Space between elements
//     //     foreach (Transform child in inventoryParent.transform)
//     //     {
//     //         Renderer renderer = child.GetComponent<Renderer>();
//     //         if (renderer != null)
//     //         {
//     //         Bounds bounds = renderer.bounds;
//     //         float height = bounds.size.y;
//     //         float width = bounds.size.z;
            
//     //         // Position the element
//     //         child.localPosition = new Vector3(0, yOffset, 0);
            
//     //         // Update offset for next element
//     //         yOffset += height + spacing;
//     //         zoffset += width + spacing;
//     //         }
//     //     }
//     //     inventoryParent.SetActive(false);
//     //     locksParent.SetActive(false);

// 	// }
//     IEnumerator LoadInventoryFromJson(string path) 
//     {
//         MeshReader meshReader = gameObject.AddComponent<MeshReader>(); // Add as a component!
        
//         yield return StartCoroutine(meshReader.GetInventoryFromFilePath(path, (inventoryData) =>
//         {
//             if (inventoryData != null)
//             {
//                 inventory = inventoryData;
//             }
//         }));

//         for (int i = 0; i < inventory.priority.Length; i++)
//         {
//             string name = inventory.priority[i];
//             Match match = Regex.Match(name, @"^[A-Za-z]+_\d+");

//             if (match.Success)
//             {
//                 string elementpath = Path.Combine(folderpath, match.Value + ".json");

//                 yield return StartCoroutine(meshReader.GetMemberFromFilePath(elementpath, (memberData) =>
//                 {
//                     if (memberData != null)
//                     {
//                         inventory.members[match.Value] = memberData;
//                         GenerateMember(memberData, match.Value, inventoryParent);
//                     }
//                 }));
//             }
//         }

//         ArrangeInventory();
//     }

	
// 	void LoadFromJson(string path) 
// 	{
// 		// Create mesh Reader
// 		MeshReader meshReader = new MeshReader();
// 		// meshReader.GetJsonFromFilePath(path);

//         StartCoroutine(meshReader.GetJsonFromFilePath(path, (meshData) => 
//         {
//             if (meshData != null)
//             {
//                 Generate(meshData, elementsParent);
//             }
//         }));


// 		//Generate(meshReader.data, elementsParent);
// 	}

		
// 	IEnumerator AfterLoading() {
// 		if(loading != null)
// 		loading.SetActive(false);
		
// 		yield return null;
// 	}

//     // To dispatch coroutines
// 	public readonly Queue<Action> ExecuteOnMainThread = new ();
	
//     public void GenerateMultiple(MultipleMeshesData data, GameObject elParent)
//     {
//         foreach (var elementPair in data.elements) // Loop through each element in the dictionary
//         {
//             MeshData meshData = elementPair.Value;
//             Generate( meshData, elParent);
//         }
//     }

//     public void Generate(MeshData data, GameObject elParent)
//     {

//         element = data.GenerateMesh();
//         element.transform.parent = elParent.transform;

//         Material uniqueMaterial = new Material(material);
//         uniqueMaterial.name = element.name;
//         data.AssignMaterial(element, uniqueMaterial);

//         Color elColor= new Color(data.color[0], data.color[1], data.color[2], alphaValue);
//         element.SetColor(elColor);
        
//         element.AddComponent<MeshCollider>();
        
//         var interactable =element.AddComponent<StatefulInteractable>();
//         interactable.ToggleMode = StatefulInteractable.ToggleType.Toggle;
//         interactable.OnToggled.AddListener(() => drawController.StartDrawing());
//         interactable.OnUntoggled.AddListener(() => drawController.StopDrawing());


//         GameObject lockInstance = Instantiate(padlocks);

//         Renderer renderer = element.GetComponent<Renderer>();
//         Bounds bounds = renderer.bounds;
//         Vector3 center = bounds.center;
//         Vector3 size = bounds.size;

//         lockInstance.GetComponent<Orbital>().LocalOffset = new Vector3(center[0], center[1]+ size[1]/2 + 0.05f , center[2]);
        
//         lockInstance.name ="lock_" + element.name;
//         lockInstance.transform.parent = locksParent.transform;
//         SolverHandler lockSolver = lockInstance.GetComponent<SolverHandler>();
//         lockSolver.TrackedTargetType = TrackedObjectType.CustomOverride;
//         lockSolver.TransformOverride = element.transform;
//         lockInstance.GetComponent<ElementStateController>().target = element;

//         element.transform.localPosition = Vector3.zero;
//         element.transform.localRotation = Quaternion.identity;
// 	}

//     public void AdjustTransparency(bool transparencyUp, TMP_Text infoText)
//     {
//         if (transparencyUp && alphaValue<0.95f)
//         {
//             alphaValue =alphaValue + 0.1f;
//         }
//         else if (!transparencyUp && alphaValue>0.05f)
//         {
//             alphaValue =alphaValue - 0.1f;
//         }

//         infoText.text = Mathf.RoundToInt(alphaValue * 100).ToString() + "%";


//         foreach (Transform child in elementsParent.transform)
//         {
//             MeshRenderer mRenderer= child.gameObject.GetComponent<MeshRenderer>();
            
//             if (mRenderer != null)
//             {
//                 foreach (Material mat in mRenderer.materials)
//                 {
//                     Color color = mat.color;
//                     color.a = alphaValue;
//                     mat.color = color;
//                 }
//             }
//         }
//     }

//     public void GenerateMember(MemberData memberData, string name, GameObject elParent)
//     {
//         element = memberData.mesh.GenerateMesh();
//         element.name = name;
//         element.transform.parent = elParent.transform;

//         foreach (Material mat in materials)
//         {
//             if (mat != null && mat.name == name)
//             {
//                 memberData.mesh.AssignMaterial(element, mat);
//                 break; 
//             }
//         }
//         element.AddComponent<MeshCollider>();


//         var interactable =element.AddComponent<StatefulInteractable>();
//         interactable.ToggleMode = StatefulInteractable.ToggleType.Toggle;
//         interactable.OnToggled.AddListener(() => drawController.StartDrawing());
//         interactable.OnUntoggled.AddListener(() => drawController.StopDrawing());


//         GameObject lockInstance = Instantiate(padlocks);


//         //Grab world-space bounds from the Renderer
//         Bounds worldBounds = element.GetComponent<Renderer>().bounds;
//         Vector3 worldCenter = worldBounds.center;
//         Vector3 worldSize   = worldBounds.size;

//         // Convert them to element-local coordinates
//         Vector3 localCenter = element.transform.InverseTransformPoint(worldCenter);
//         Vector3 localSize   = element.transform.InverseTransformVector(worldSize);

//         // Adjust local offset to be "above" the item
//         Vector3 localOffset = new Vector3(
//             localCenter.x,
//             localCenter.y + (localSize.y / 2.0f) + 0.075f,
//             localCenter.z
//         );

//         // 4) Assign to the lock's Orbital LocalOffset
//         lockInstance.GetComponent<Orbital>().LocalOffset = localOffset;

//         lockInstance.name ="lock_" + element.name;
//         lockInstance.transform.parent = locksParent.transform;
//         SolverHandler lockSolver = lockInstance.GetComponent<SolverHandler>();
//         lockSolver.TrackedTargetType = TrackedObjectType.CustomOverride;
//         lockSolver.TransformOverride = element.transform;
//         lockInstance.GetComponent<ElementStateController>().target = element;

//         element.transform.localPosition = Vector3.zero;
//         element.transform.localRotation = Quaternion.identity;

//         //lockInstance.SetActive(false);
//         //element.SetActive(false); 
// 	}

// }

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Networking;
using System.Text;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using MeshElementData;
using Unity.VisualScripting;
using UnityEngine.XR.Interaction.Toolkit;
using TMPro;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;

public class MeshGeneratorFromJson : MonoBehaviour
{
    public GameObject loading; 
    [HideInInspector] public GameObject elementsParent;
    [HideInInspector] public Stock stock;
    [HideInInspector] public GameObject locksParent;
    public DrawingController drawController;
    public UIController uiController;
    public GameObject padlocks;
    private string path; 
    public string fileName;
    public string fileNameStock; 
    public Material material;     			
    private GameObject element;
    private float alphaValue = 0.20f;

    [HideInInspector] public GameObject inventoryParent;
    public Material[] materials;
    public Inventory inventory;
    private string folderpath = ""; 
    private string library = ""; 

    private MeshReader meshReader; // Reference to MeshReader

	void Start()
    {   
        elementsParent = new GameObject("Elements");
        locksParent = new GameObject("Locks");
        locksParent.SetActive(true);
        elementsParent.SetActive(false);

        inventoryParent = new GameObject("Inventory");

        meshReader = gameObject.AddComponent<MeshReader>(); // Add MeshReader as a component

        string dataFolderPath = Application.streamingAssetsPath;
        path = Path.Combine(dataFolderPath, fileName + ".json");

        folderpath = dataFolderPath;
        library = Path.Combine(folderpath, "material_library.json");

        Debug.Log("Path: " + path);

        StartCoroutine(LoadInventoryFromJson(library));
    }

    IEnumerator LoadInventoryFromJson(string path) 
	{
		yield return StartCoroutine(meshReader.GetInventoryFromFilePath(path, (inventoryData) =>
        {
            if (inventoryData != null)
            {
                inventory = inventoryData;
            }
        }));

        for (int i = 0; i < inventory.priority.Length; i++)
        {
            string name = inventory.priority[i];
            Match match = Regex.Match(name, @"^[A-Za-z]+_\d+");

            if (match.Success)
            {
                string elementpath = Path.Combine(folderpath, match.Value + ".json");

                yield return StartCoroutine(meshReader.GetMemberFromFilePath(elementpath, (memberData) =>
                {
                    if (memberData != null)
                    {
                        inventory.members[match.Value] = memberData;
                        GenerateMember(memberData, match.Value, inventoryParent);
                    }
                }));
            }
        }

        ArrangeInventory();
	}

    IEnumerator LoadFromJson(string path) 
	{
		yield return StartCoroutine(meshReader.GetJsonFromFilePath(path, (meshData) =>
        {
            if (meshData != null)
            {
                Generate(meshData, elementsParent);
            }
        }));
	}
		
	IEnumerator AfterLoading() 
    {
		if (loading != null)
		    loading.SetActive(false);
		
		yield return null;
	}

    public readonly Queue<Action> ExecuteOnMainThread = new ();

    public void GenerateMultiple(MultipleMeshesData data, GameObject elParent)
    {
        foreach (var elementPair in data.elements)
        {
            MeshData meshData = elementPair.Value;
            Generate(meshData, elParent);
        }
    }

    public void Generate(MeshData data, GameObject elParent)
    {
        element = data.GenerateMesh();
        element.transform.parent = elParent.transform;

        Material uniqueMaterial = new Material(material);
        uniqueMaterial.name = element.name;
        data.AssignMaterial(element, uniqueMaterial);

        Color elColor = new Color(data.color[0], data.color[1], data.color[2], alphaValue);
        element.SetColor(elColor);
        
        element.AddComponent<MeshCollider>();
        
        var interactable = element.AddComponent<StatefulInteractable>();
        interactable.ToggleMode = StatefulInteractable.ToggleType.Toggle;
        interactable.OnToggled.AddListener(() => drawController.StartDrawing());
        interactable.OnUntoggled.AddListener(() => drawController.StopDrawing());

        GameObject lockInstance = Instantiate(padlocks);

        Renderer renderer = element.GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;
        Vector3 center = bounds.center;
        Vector3 size = bounds.size;

        lockInstance.GetComponent<Orbital>().LocalOffset = new Vector3(center.x, center.y + size.y / 2 + 0.05f, center.z);
        
        lockInstance.name = "lock_" + element.name;
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
        if (transparencyUp && alphaValue < 0.95f)
        {
            alphaValue += 0.1f;
        }
        else if (!transparencyUp && alphaValue > 0.05f)
        {
            alphaValue -= 0.1f;
        }

        infoText.text = Mathf.RoundToInt(alphaValue * 100).ToString() + "%";

        foreach (Transform child in elementsParent.transform)
        {
            MeshRenderer mRenderer = child.gameObject.GetComponent<MeshRenderer>();
            
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

        var interactable = element.AddComponent<StatefulInteractable>();
        interactable.ToggleMode = StatefulInteractable.ToggleType.Toggle;
        interactable.OnToggled.AddListener(() => drawController.StartDrawing());
        interactable.OnUntoggled.AddListener(() => drawController.StopDrawing());

        GameObject lockInstance = Instantiate(padlocks);

        Bounds worldBounds = element.GetComponent<Renderer>().bounds;
        Vector3 worldCenter = worldBounds.center;
        Vector3 worldSize = worldBounds.size;

        Vector3 localCenter = element.transform.InverseTransformPoint(worldCenter);
        Vector3 localSize = element.transform.InverseTransformVector(worldSize);

        Vector3 localOffset = new Vector3(
            localCenter.x,
            localCenter.y + (localSize.y / 2.0f) + 0.075f,
            localCenter.z
        );

        lockInstance.GetComponent<Orbital>().LocalOffset = localOffset;

        lockInstance.name = "lock_" + element.name;
        lockInstance.transform.parent = locksParent.transform;
        SolverHandler lockSolver = lockInstance.GetComponent<SolverHandler>();
        lockSolver.TrackedTargetType = TrackedObjectType.CustomOverride;
        lockSolver.TransformOverride = element.transform;
        lockInstance.GetComponent<ElementStateController>().target = element;

        element.transform.localPosition = Vector3.zero;
        element.transform.localRotation = Quaternion.identity;
	}

    private void ArrangeInventory()
    {
        float yOffset = 0;
        float zoffset = 0;
        float spacing = 0.25f;

        foreach (Transform child in inventoryParent.transform)
        {
            Renderer renderer = child.GetComponent<Renderer>();
            if (renderer != null)
            {
                Bounds bounds = renderer.bounds;
                float height = bounds.size.y;
                float width = bounds.size.z;

                child.localPosition = new Vector3(0, yOffset, 0);
                yOffset += height + spacing;
                zoffset += width + spacing;
            }
        }

        inventoryParent.SetActive(false);
        locksParent.SetActive(false);
    }
}
