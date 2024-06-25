using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;

namespace MeshElementData
{
    [System.Serializable]
    public class Result
    {
        public string result { get; set; }
    }
    
    [System.Serializable]
    public class MeshData
    {
        public string name { get; set; }

        public float[] color { get; set; }

        [JsonConverter(typeof(IntArrayArrayConverter))]
        public int[][] faces { get; set; }

        [JsonConverter(typeof(Vector3ArrayConverter))]
        public Vector3[] vertices { get; set; }

        [JsonConverter(typeof(Vector3ArrayConverter))]
        public Vector3[] normals { get; set; }

        [JsonConverter(typeof(Vector2ArrayConverter))]
        public Vector2[] uv { get; set; }

        private int[] tris { get; set; }

        // Method to calculate triangles of the mesh
        public void CalculateTriangles()
        {
            List<int> triangles = new List<int>();

            foreach (var face in faces)
            {
                // Assuming each face is a triangle (consists of three vertices)
                if (face.Length != 3)
                {
                    throw new System.InvalidOperationException("Each face must have exactly 3 vertices to calculate triangles.");
                }

                triangles.AddRange(face);
            }

            tris = triangles.ToArray();
        }


        // Method to generate a mesh and attach it to a new game object
        public GameObject GenerateMesh()
        {
            if (vertices == null || faces == null)
            {
                Debug.LogError("Vertices or faces data is missing.");
                return null;
            }

            // Calculate triangles if not already done
            if (tris == null || tris.Length == 0)
            {
                CalculateTriangles();
            }

            // Create the new mesh
            Mesh mesh = new Mesh
            {
                name = this.name,
                vertices = this.vertices,
                triangles = this.tris,
                normals = this.normals,
                uv = this.uv
            };

            // Recalculate bounds and normals if not provided
            if (this.normals == null || this.normals.Length == 0)
            {
                mesh.RecalculateNormals();
            }
            mesh.RecalculateBounds();

            // Create a new game object and add necessary components
            GameObject meshObject = new GameObject(this.name);
            MeshFilter meshFilter = meshObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = meshObject.AddComponent<MeshRenderer>();

            // Assign the mesh to the mesh filter
            meshFilter.mesh = mesh;

            return meshObject;
        }

        // Method to assign a material to the generated game object
        public void AssignMaterial(GameObject meshObject, Material material)
        {
            if (meshObject == null)
            {
                Debug.LogError("The provided game object is null.");
                return;
            }

            MeshRenderer meshRenderer = meshObject.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.material = material;
            }
            else
            {
                Debug.LogError("The game object does not have a MeshRenderer component.");
            }
        }
    }
}
