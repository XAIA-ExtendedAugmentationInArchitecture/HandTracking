using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class MqttPublisher : MonoBehaviour
{
    public MqttController mqtt;

    public void PublishMessage(string topicPublish, object message)
    {
        
        Dictionary<string, object> msg_dict = new Dictionary<string, object>
        {
            {"result", message},
        };
        
        mqtt.message = msg_dict;
        mqtt.Publish(topicPublish);
    }

    // private List<string> Prepare_Debug(string topicPublish)
    // {
    //     List<string> info_message = new();
    //     if (topicPublish == topicsPublish[0])
    //     {
    //         info_message.Add("Requesting robot's configuration...");
    //         info_message.Add("Waiting for robot's configuration...");
    //         //info_message.Add("This is a robot's configuration for placing the element: " + currentElement.text);

    //     }
    //     else if(topicPublish== topicsPublish[1])
    //     {
    //         info_message.Add("Requesting a trajectory for element: "+ currentElement.text);
    //         info_message.Add("Waiting for a trajectory...");
    //     }
    //     else if(topicPublish== topicsPublish[2])
    //     {
    //         info_message.Add("Execution starts...");
    //         info_message.Add("");
    //     }
    //     return info_message;
    // }

}
