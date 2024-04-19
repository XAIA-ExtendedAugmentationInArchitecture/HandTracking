using System;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;


public class Instantiator : MonoBehaviour


{   [System.Serializable]
    public class JointPoseArrList
    {
        public List<float[]> jointPosesArrList;
    }

    [System.Serializable]
    public class JointPoseDict
    {
        public Dictionary<string, JointPoseArrList[]> JointPoses;
    }
    // public JointPoseDict newJointPoseDict = new JointPoseDict();
    // public JointPoseArrList newJointPoseArrList = new JointPoseArrList();


    public string filepath = "Assets/Data/joint_poses.json";
    public GameObject sphere;


    // Start is called before the first frame update
    void Start()
    {
        try
    {
    string content = System.IO.File.ReadAllText(filepath);
    Debug.Log("Imported content: " + content);

    JointPoseDict json = JsonConvert.DeserializeObject<JointPoseDict>(content);

    Debug.Log("Total object: " + json.JointPoses.Count);

    if (json != null)
    {
        Debug.Log("Deserialization successful!");
        if (json.JointPoses != null && json.JointPoses.ContainsKey("1"))
            {
                Debug.Log("Deserialized object: " + json.JointPoses["1"]);
            }
            else
            {
                Debug.LogError("Key '1' not found in JointPoses dictionary or JointPoses is null.");
            }
        }
        else
        {
            Debug.LogError("Deserialized object is null.");
        }
    }
    catch (Exception e)
    {
        Debug.LogError("Error reading or deserializing JSON: " + e.Message);
    }
    }
    // Update is called once per frame
    void Update()
    {

        //Debug.Log("Deserialized object " + json);
        
        
    }

    void PlaceSpheres()
    {

        
        // foreach (var kvp in pointPositions)
        // {
        //     // Instantiate a sphere at the position defined in the dictionary
        //     GameObject sphere = Instantiate(spherePrefab, kvp.Value, Quaternion.identity);
        //     // Set the name of the sphere
        //     sphere.name = kvp.Key;
        // }
    }
}
