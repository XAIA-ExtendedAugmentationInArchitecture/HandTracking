using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;


namespace DrawingsData
{
    [System.Serializable]
    public class Frame
    {
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 point { get; set; }

        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 xaxis { get; set; }

        [JsonProperty("yaxis")]
        [JsonConverter(typeof(Vector3Converter))]
        public Vector3 zaxis { get; set; }  

    }


    [System.Serializable]
    public class Drawing
    {
       // public string[] Colors; 
        public Frame frame;

        [JsonConverter(typeof(Vector3DictionaryConverter))]
        public Dictionary<string, Vector3[]> lines { get; set; }

    }

    [System.Serializable]
    public class Drawings
    {
        public string uid { get; set;}
        public Dictionary<string, Drawing> drawings { get; set;}

    }

}
