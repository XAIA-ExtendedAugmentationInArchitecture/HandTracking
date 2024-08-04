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
    public class DrawingLine
    {
        public float[] color { get; set; } 

        [JsonConverter(typeof(Vector3ArrayConverter))]
        public Vector3[] positions { get; set; }

    }

    [System.Serializable]
    public class Drawing
    {
        public Frame MCF { get; set;}
        public Dictionary<string, Frame> objectFrames { get; set; }
        public Dictionary<string, DrawingLine> lines { get; set; }
        public Dictionary<string, Frame> pinPoints { get; set; }

    }

    [System.Serializable]
    public class Drawings
    {
        public string uid { get; set;}
        public Dictionary<string, Drawing> drawings { get; set;}

    }

}
