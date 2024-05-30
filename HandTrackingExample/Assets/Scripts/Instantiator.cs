using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
using System.Linq;
using Unity.VisualScripting;
using Unity.XR.CoreUtils;


public class Instantiator : MonoBehaviour
{   
    public GameObject spherePrefab;
    [SerializeField]
    public Dictionary<string, List<List<float>>> JointPoseDict;
    public GameObject parentgameobject;
    public GameObject RTparent;
    private GameObject parent;
    private Transform parenttransform;
    public string filepath = "Assets/Data/joint_poses.json";
    private string content;

    // Start is called before the first frame update
    void Start()
    {

    RTparent = new GameObject("RTSpheres");
    }


    Dictionary<string, List<List<float>>> Deserialize(string content)
    {
        Debug.Log("Deserializing content..." + content);
        JointPoseDict = JsonConvert.DeserializeObject<Dictionary<string, List<List<float>>>>(content);
        // if (JointPoseDict != null)
        // {

        //     Debug.Log("Deserialization successful!");
        //     if (JointPoseDict.Values != null && JointPoseDict.ContainsKey("1"))
        //         {
        //             Debug.Log("Deserialized object type: " + JointPoseDict["1"].GetType());
        //         }
        //         else
        //         {
        //             Debug.LogError("Key '0' not found in JointPoses dictionary or JointPoses is null.");
        //         }
        //     }
        //     else
        //     {
        //         Debug.LogError("Deserialized object is null.");
    
        // }

        return JointPoseDict;
    }
    
    

    public void InstantiateSpheresDict()
    {
        content = System.IO.File.ReadAllText(filepath);
        Deserialize(content);


        if (JointPoseDict == null)
        {
            Debug.LogError("JointPoseDict is null.");
        }       
        else
        {
            for (int i = RTparent.transform.childCount - 1; i >= 0; i--)
                {
                Debug.Log("Destroying child: " + RTparent.transform.GetChild(i).gameObject);
                Destroy(RTparent.transform.GetChild(i).transform.gameObject);
                }

            for(int i = parentgameobject.transform.childCount - 1; i >= 0; i--)
                {
                    Debug.Log("Destroying child: " + parentgameobject.transform.GetChild(i).gameObject);
                    Destroy(parentgameobject.transform.GetChild(i).transform.gameObject);
                }

            foreach (var (key, value) in JointPoseDict)
                {
                    //define the key as a parent object
                    Debug.Log("Instantiating spheres for key: " + key);
                    GameObject parent = new GameObject(key);
                    parent.transform.parent = parentgameobject.transform;
                    Debug.Log("Key: " + key);
                    Debug.Log("Value: " + value);
                    List<Vector3> veclist = new List<Vector3>();
                    foreach (var item in value)
                    {
                        Vector3 vec = new Vector3(item[0], item[1], item[2]);
                        veclist.Add(vec);
                    }
                    foreach (var (index, item) in veclist.Select((item, index) => (index, item)))
                    {
                        GameObject sphere = Instantiate(spherePrefab, item, Quaternion.identity);
                        sphere.transform.parent = parent.transform;
                        sphere.name = parent.name + "_" + index;
                    }
                }
        }
        
    }

    public void InstantiateSpheresRT(Pose pose, GameObject parent)
    {
        Vector3 vec = new Vector3(pose.position.x, pose.position.y, pose.position.z);
        GameObject sphere = Instantiate(spherePrefab, vec, Quaternion.identity);
        sphere.transform.parent = parent.transform;
        sphere.name = parent.name + "_Sphere";

        //Debug.Log("Instantiating spheres..." + spherePrefab);
        //Debug.Log("Index: " + index);
        //Debug.Log("child: " + RTparent.transform.Find(index));
        //int childcount = 0;

        //if (RTparent.transform.Find(index))
        //{
        //    Debug.Log("Parent with name " + index + " already exists.");
        //    parenttransform = RTparent.transform.Find(index).transform;
        //    // count the amount of children in gameobject
        //    childcount = parenttransform.childCount;
        //}
        //else
        //{
        //    GameObject parent = new GameObject(index);
        //    parent.transform.parent = RTparent.transform;
        //    parenttransform = parent.transform;
        //}

        //Vector3 vec = new Vector3(pose.position.x, pose.position.y, pose.position.z);
        //GameObject sphere = Instantiate(spherePrefab, vec, Quaternion.identity);
        //sphere.transform.parent = parenttransform;
        //sphere.name = index + "_" + childcount;
    }
}
