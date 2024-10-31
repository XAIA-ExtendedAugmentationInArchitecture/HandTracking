/*
The MIT License (MIT)

Copyright (c) 2018 Giovanni Paolo Vigano'

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using MeshElementData;
using DrawingsData;
using MixedReality.Toolkit.UX;


public class MqttController : M2MqttUnityClient
{
    

    [Header("MQTT topics")]
    [Tooltip("Set the topic to subscribe. !!!ATTENTION!!! multi-level wildcard # subscribes to all topics")]

    public List<string> topicsSubscribe;

    public List<string> topicsPublish;
    
    public Dictionary<string,object> message; // message to publish
    public UIController uIController; 

    [Tooltip("Set this to true to perform a testing cycle automatically on startup")]
    public bool autoTest = true;


    private string m_msg;
    public MeshData msgData;
    public Drawings msgDataLines;
    public MultipleMeshesData msgDataMeshes;
    //using C# Property GET/SET and event listener to reduce Update overhead in the controlled objects
    

    public string msg
    {
        get
        {
            return m_msg;
        }
        set
        {
            if (m_msg == value) return;
            m_msg = value;
            if (OnMessageArrived != null)
            {
                OnMessageArrived(m_msg);
            }
        }
    }

    public event OnMessageArrivedDelegate OnMessageArrived;
    public delegate void OnMessageArrivedDelegate(string newMsg);

    //using C# Property GET/SET and event listener to expose the connection status
    private bool m_isConnected;

    public bool isConnected
    {
        get
        {
            return m_isConnected;
        }
        set
        {
            if (m_isConnected == value) return;
            m_isConnected = value;
            if (OnConnectionSucceeded != null)
            {
                OnConnectionSucceeded(isConnected);
            }
        }
    }
    public event OnConnectionSucceededDelegate OnConnectionSucceeded;
    public delegate void OnConnectionSucceededDelegate(bool isConnected);

    // a list to store the messages
    private List<string> eventMessages = new List<string>();

    private IEnumerator WaitingMessage(float delay,string info_message)
    {
        yield return new WaitForSeconds(delay);
        //Messages.text = info_message;
        //Messages.enabled= true;
    }
    public void Publish(string topicPublish)
    {   
        string messagePublish = JsonConvert.SerializeObject(message);
        client.Publish(topicPublish, System.Text.Encoding.UTF8.GetBytes(messagePublish), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);

    }
public void SetEncrypted(bool isEncrypted)
    {
        this.isEncrypted = isEncrypted;
    }

protected override void OnConnecting()
    {
        base.OnConnecting();
    }

protected override void OnConnected()
    {
        base.OnConnected();
        isConnected=true;

        // if (autoTest)
        // {
        //     Publish(topicPublish);
        // }
    }

// protected override void OnConnectionFailed()
//     {
//         isConnected=false;
//         uIController.ActivateConnectionDialog("failed");
//     }

protected override void OnDisconnected()
    {
        isConnected=false;
        uIController.ActivateConnectionDialog("disconnected");
        
    }

protected override void OnConnectionLost()
    {
        isConnected=false;
        uIController.ActivateConnectionDialog("lost");
    }

public void subscribeTopics()
    {
        SubscribeTopics();
    }

protected override void SubscribeTopics()
    {
        foreach ( string topic in  topicsSubscribe)
        
        {
            client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }
    }

protected override void UnsubscribeTopics()
    {
        foreach ( string topic in  topicsSubscribe)
        {
            client.Unsubscribe(new string[] { topic });
        }
    }

protected override void Start()
    {
        base.Start();
    }


protected override void DecodeMessage(string topic, byte[] message)
    {
        // The message is decoded
        // msg = System.Text.Encoding.UTF8.GetString(message);

        // // Parse the message into a JObject to access its 'result' field
        // var messageObj = Newtonsoft.Json.Linq.JObject.Parse(msg);
        // var result = messageObj["result"];

        // // Debug message
        // Debug.Log("Received message on topic: " + topic);

        // // Determine the type of the 'result' field
        // if (result.Type == Newtonsoft.Json.Linq.JTokenType.Array)
        // {
        //     // The result is a JSON array (list in Python)
        //     Debug.Log("Received: JSON array ");
        //     // var listResult = (Newtonsoft.Json.Linq.JArray)result;
        //     // ProcessListResult(listResult);
        // }
        // else if (result.Type == Newtonsoft.Json.Linq.JTokenType.Object)
        // {
        //     Debug.Log("Received: JSON object ");
        //     // The result is a JSON object (dict in Python)
        //     // var dictResult = (Newtonsoft.Json.Linq.JObject)result;
        //     // ProcessFullMessage(dictResult);
        // }
        // else if (result.Type == Newtonsoft.Json.Linq.JTokenType.String)
        // {
        //     Debug.Log("Received: JSON string ");
        //     // The result is a string containing JSON
        //     // var strResult = result.ToString();
        //     // var dictResult = Newtonsoft.Json.Linq.JObject.Parse(strResult);
        //     // ProcessFullMessage(dictResult);
        // }
        // else
        // {
        //     Debug.LogWarning("Unknown message format received.");
        // }

        // uIController.MessageReceived(topic, "received");
        //The message is decoded
        msg = System.Text.Encoding.UTF8.GetString(message);
        var result = JsonConvert.DeserializeObject<Result>(msg);
        if (topic == "/kitgkr_teamA_geometry/" || topic == "/kitgkr_teamB_geometry/")
        {
            msgData = JsonConvert.DeserializeObject<MeshData>(result.result);
            uIController.MessageReceived(topic, msgData.name);
        }
        else if (topic == "/kitgkr_teamA_lines/" || topic == "/kitgkr_teamA_lines/")
        {
            msgDataLines = JsonConvert.DeserializeObject<Drawings>(result.result);
            uIController.MessageReceived(topic, msgDataLines.uid);
        }
        else if (topic == "/kitgkr_teamA_geometries/" || topic == "/kitgkr_teamA_geometries/")
        {
            msgDataMeshes = JsonConvert.DeserializeObject<MultipleMeshesData>(result.result);
            uIController.MessageReceived(topic, msgDataMeshes.uid);
        }
        
        Debug.Log("Received: " + msg);
        Debug.Log("from topic: " + topic);

        
        StoreMessage(msg);
    }

private void StoreMessage(string eventMsg)
    {
        if (eventMessages.Count > 50)
        {
            eventMessages.Clear();
        }
        eventMessages.Add(eventMsg);
    }

protected override void Update()
    {
        base.Update(); // call ProcessMqttEvents()

    }

private void OnDestroy()
    {
        Disconnect();
    }

private void OnValidate()
    {
        if (autoTest)
        {
            autoConnect = true;
        }
    }
}
