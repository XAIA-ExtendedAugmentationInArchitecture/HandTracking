// using System;
// using System.Collections.Generic;
// using UnityEngine;
// using System.IO;
// using Newtonsoft.Json;

// using System.Linq;
// using MeshElementData;

// public class MeshReader : MonoBehaviour
// {
//     public MeshData data;

//     public Inventory libraryData;
//     public MemberData memberData;

//     public void GetJsonFromFilePath(string filePath)
//     {
//         // Check if the file exists
//         if (File.Exists(filePath))
//         {
//             // Read the content of the JSON file
//             string jsonContent = File.ReadAllText(filePath);

//             // Deserialize JSON into an object
//             data = JsonConvert.DeserializeObject<MeshData>(jsonContent);
//         }
//         else
//         {
            
//             Debug.LogError("JSON file not found!");
//         }
//     }

//     public void GetInventoryFromFilePath(string filePath)
//     {
//         // Check if the file exists
//         if (File.Exists(filePath))
//         {
//             // Read the content of the JSON file
//             string jsonContent = File.ReadAllText(filePath);

//             // Deserialize JSON into an object
//             libraryData = JsonConvert.DeserializeObject<Inventory>(jsonContent);
//             Debug.Log("Holaaaa" + filePath);
//         }
//         else
//         {
            
//             Debug.LogError("JSON file not found!");
//         }
//     }

//     public void GetMemberFromFilePath(string filePath)
//     {

//         // Check if the file exists
//         if (File.Exists(filePath))
//         {
//             // Read the content of the JSON file
//             string jsonContent = File.ReadAllText(filePath);

//             // Deserialize JSON into an object
//             memberData = JsonConvert.DeserializeObject<MemberData>(jsonContent);
//         }
//         else
//         {
            
//             Debug.LogError("JSON file not found!");
//         }
//     }

//     public void GetDataFromString(string jsonContent)
//     {
//         // Check if the file exists
//         if (jsonContent!=null)
//         {
//             // Deserialize JSON into a dynamic object
//             data = JsonConvert.DeserializeObject<MeshData>(jsonContent);
//         }
//         else
//         {
//             Debug.LogError("JSON file not found!");
//         }
//     }


// }
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.Networking;
using MeshElementData;

public class MeshReader : MonoBehaviour
{
    public MeshData data;
    public Inventory libraryData;
    public MemberData memberData;

    public IEnumerator GetJsonFromFilePath(string filePath, Action<MeshData> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(filePath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load MeshData JSON: " + www.error);
                callback?.Invoke(null);
            }
            else
            {
                string jsonContent = www.downloadHandler.text;
                data = JsonConvert.DeserializeObject<MeshData>(jsonContent);
                callback?.Invoke(data);
            }
        }
    }

    public IEnumerator GetInventoryFromFilePath(string filePath, Action<Inventory> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(filePath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load Inventory JSON: " + www.error);
                callback?.Invoke(null);
            }
            else
            {
                string jsonContent = www.downloadHandler.text;
                libraryData = JsonConvert.DeserializeObject<Inventory>(jsonContent);
                Debug.Log("Inventory loaded from: " + filePath);
                callback?.Invoke(libraryData);
            }
        }
    }

    public IEnumerator GetMemberFromFilePath(string filePath, Action<MemberData> callback)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(filePath))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to load Member JSON: " + www.error);
                callback?.Invoke(null);
            }
            else
            {
                string jsonContent = www.downloadHandler.text;
                memberData = JsonConvert.DeserializeObject<MemberData>(jsonContent);
                callback?.Invoke(memberData);
            }
        }
    }

    public void GetDataFromString(string jsonContent)
    {
        if (!string.IsNullOrEmpty(jsonContent))
        {
            data = JsonConvert.DeserializeObject<MeshData>(jsonContent);
        }
        else
        {
            Debug.LogError("JSON content string is null or empty!");
        }
    }
}
