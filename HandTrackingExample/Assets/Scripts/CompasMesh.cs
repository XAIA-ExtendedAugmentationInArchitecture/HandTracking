// using System;
// using System.Collections.Generic;
// using Newtonsoft.Json;

// [Serializable]
// public class CompasMesh
// {
//     [JsonProperty("dtype")]
//     public string Dtype { get; set; }

//     [JsonProperty("data")]
//     public MeshData Data { get; set; }

//     [JsonProperty("guid")]
//     public string Guid { get; set; }
// }

// [Serializable]
// public class MeshData
// {
//     [JsonProperty("attributes")]
//     public Dictionary<string, object> Attributes { get; set; }

//     [JsonProperty("default_vertex_attributes")]
//     public Dictionary<string, object> DefaultVertexAttributes { get; set; }

//     [JsonProperty("default_edge_attributes")]
//     public Dictionary<string, object> DefaultEdgeAttributes { get; set; }

//     [JsonProperty("default_face_attributes")]
//     public Dictionary<string, object> DefaultFaceAttributes { get; set; }

//     [JsonProperty("vertex")]
//     public Dictionary<string, VertexData> Vertex { get; set; }

//     [JsonProperty("face")]
//     public Dictionary<string, List<int>> Face { get; set; }

//     [JsonProperty("facedata")]
//     public Dictionary<string, object> FaceData { get; set; }

//     [JsonProperty("edgedata")]
//     public Dictionary<string, object> EdgeData { get; set; }

//     [JsonProperty("max_vertex")]
//     public int MaxVertex { get; set; }

//     [JsonProperty("max_face")]
//     public int MaxFace { get; set; }
// }

// [Serializable]
// public class VertexData
// {
//     [JsonProperty("x")]
//     public float X { get; set; }

//     [JsonProperty("y")]
//     public float Y { get; set; }

//     [JsonProperty("z")]
//     public float Z { get; set; }

//     [JsonProperty("normal")]
//     public VectorData Normal { get; set; }

//     [JsonProperty("color")]
//     public object Color { get; set; }
// }

// [Serializable]
// public class VectorData
// {
//     [JsonProperty("dtype")]
//     public string Dtype { get; set; }

//     [JsonProperty("data")]
//     public List<float> Data { get; set; }

//     [JsonProperty("guid")]
//     public string Guid { get; set; }
// }
