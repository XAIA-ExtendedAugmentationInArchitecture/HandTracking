using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System.Linq;


    public class PlankBender
    {
        [Tooltip("Properties for mesh generation")]

        private int resolution =4; 

        private float width; 

        private float height;

        private float scale;

        private Vector3 normal;

        private Vector3[] polyline { get; set; }      // Polyline vertices

        private Vector3[] vertices { get; set; }

        private Vector3[] normals { get; set; }

        private Vector2[] uv { get; set; }

        private Vector3 position;                     // The position is the vertex in the middle

        private Vector3[] profile;                    // profile to make the bended planks

        private int[] tris { get; set; }
        private int tpr;                             // Number of tube trianges per revolution

        // Properties related to  Mesh Components
        [Tooltip("Properties for mesh generation")]
        private MeshRenderer meshRenderer;
	    private MeshFilter meshFilter;
	    private Mesh mesh;

        public GameObject GeneratePlankMesh(Vector3 planeNormal, float scale, float width, float height, Vector3[] polyline, GameObject gameObject, Color color)
        {
            this.width = width;
            this.height = height;
            this.scale = scale;
            this.polyline = polyline;
            this.normal = planeNormal;

            GenerateBendedPlankData();

            if (vertices == null )
            {
                Debug.LogError("Vertices data is missing.");
                return  null;
            }

            // Create the new mesh
            Mesh mesh = new Mesh
            {
                name = gameObject.name + "_bended",
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

            MeshFilter meshFilter = gameObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = gameObject.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            meshRenderer.sharedMaterial.color = color;
            // Assign the mesh to the mesh filter
            meshFilter.mesh = mesh;

            return gameObject;
        }
        void GenerateBendedPlankData()
        {
            // Create a rectangular profile with a given width and height
            width = width * scale *0.2f;  // Width of the rectangle (scaled to match the diameter)
            height = height * scale *0.2f;  // Height of the rectangle (you can adjust this ratio)

            // Define the rectangular profile (4 corners)
            profile = new Vector3[resolution];
            // Determine scaling factors for width and height based on which is larger
            //float longSide = Mathf.Max(width, height);
            //float shortSide = Mathf.Min(width, height);

            // Define the rectangular profile with consistent orientation
            profile[0] = new Vector3(-width / 2, -height / 2, 0);  // Bottom-left corner
            profile[1] = new Vector3(width / 2, -height / 2, 0);   // Bottom-right corner
            profile[2] = new Vector3(width / 2, height / 2, 0);    // Top-right corner
            profile[3] = new Vector3(-width / 2, height / 2, 0);   // Top-left corner

            // Create vertices
            int nvertices = (polyline.Length + 4) * resolution + 4;
            vertices = new Vector3[nvertices];

            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.zero;
            }

            // Number of tris
            tpr = resolution * 2 * 3;      // by 2 for the two faces (one at each end), 
                                        // by 3 for each triangle being composed of 3 vertices
            tris = new int[(polyline.Length - 1) * tpr + 4 * tpr];

            // Generate uv data
            uv = new Vector2[nvertices];


            // Set the position of the wire
            //position = polyline[polyline.Length / 2];

            // Generate plank
            GeneratePlank();

            // Generate caps
            GenerateCaps();

            // Generate UV for Plank
            GenerateUVForPlank();
        }

        void GeneratePlank() 
        {
            // Handle the first point (index 0) separately
            RevolPoints(0, polyline[0], polyline[0], polyline[1], normal);

            // Handle the middle points
            for (int i = 1; i < polyline.Length - 1; i++)
            {
                RevolPoints(i, polyline[i - 1], polyline[i], polyline[i + 1], normal);
            }

            // Handle the last point (index polyline.Length - 1) separately
            RevolPoints(polyline.Length - 1, polyline[polyline.Length - 2], polyline[polyline.Length - 1], polyline[polyline.Length - 1], normal);

            // Generate tris
            for (int i = 0; i < polyline.Length - 1; i++)
                GenerateTris(i);
        }

        void GenerateCaps() {
            // Set the first point position to the first point of the first cap
            int index = polyline.Length;
            
            for(int i=0; i<resolution; i++) {
                vertices[index*resolution+i] = polyline[0];
            }
            
            // Copy the vertices of the first circle to the second point of the first cap
            for(int i=0; i<resolution; i++) {
                vertices[(index+1)*resolution+i] = vertices[i];
            }
            
            // Generate tris
            GenerateTris(index);
            
            // Copy the vertices of the first circle to the first point of the second cap
            index = polyline.Length+2;
            
            for(int i=0; i<resolution; i++) {
                vertices[index*resolution+i] = vertices[(polyline.Length-1)*resolution+i];
            }
            
            // Set the first point position to the second point of the second cap
            for(int i=0; i<resolution; i++) {
                vertices[(index+1)*resolution+i] = polyline[polyline.Length-1];
            }
            
            // Generate tris
            GenerateTris(index);
        }

        void RevolPoints(int index, Vector3 P, Vector3 Q, Vector3 R, Vector3 planeNormal) 
        {
            // p is the first point
            // q is the middle point
            // r is the end point
            //
            // p----q----r
            

            // If start/end point
            if(Vector3.Distance(P, Q) < 0.0001) {
                Vector3 angle = R-P;
                GenerateRevolPoints(index, 0, Q, angle, planeNormal);
            }
            // If middle points
            else {
                Vector3 PQ = Q-P;
                Vector3 QR = R-Q;
            
                Vector3 angle = (PQ+QR)/2.0f; // The angle is the average
                GenerateRevolPoints(index, 1, Q, angle, planeNormal);
            }
        }

        static float AngleOffAroundAxis(Vector3 v, Vector3 forward, Vector3 axis)
        {
            Vector3 right = Vector3.Cross(axis, forward).normalized;
            forward = Vector3.Cross(right, axis).normalized;
            return Mathf.Atan2(Vector3.Dot(v, right), Vector3.Dot(v, forward)) * (180.0f/3.14f);
        }

        void GenerateRevolPoints(int index, int deltaIndex, Vector3 Q, Vector3 angle, Vector3 planeNormal)
        {
            // Create a tangent vector along the curve (direction of motion)
            Vector3 tangent = angle.normalized;

            // Use cross product to get normal and binormal vectors (to orient the rectangle correctly)
            Vector3 normal = Vector3.Cross(tangent, planeNormal).normalized;  // Use plane normal here
            Vector3 binormal = Vector3.Cross(tangent, normal).normalized;

            // Apply these vectors to generate the rotated rectangle vertices
            for (int i = 0; i < resolution; i++)
            {
                Vector3 vertexOffset = profile[i].x * normal + profile[i].y * binormal;  // Apply rectangle offsets using the predefined profile
                vertices[index * resolution + i] = Q + vertexOffset;  // Offset from the center point
            }
        }
        
        void GenerateTris(int index) {
            int baseIndex = index * resolution;
            int trisBaseIndex = index * tpr;

            // Join
            for (int i = 0; i < resolution; i++) {
                int i6 = i * 6;
                int iPlus1 = i + 1;
                int iPlusResolution = i + resolution;
                int iPlusResPlus1 = i + resolution + 1;

                // lower left triangle
                tris[trisBaseIndex + i6] = baseIndex + i;
                tris[trisBaseIndex + i6 + 1] = baseIndex + iPlus1;
                tris[trisBaseIndex + i6 + 2] = baseIndex + iPlusResPlus1;

                // upper right triangle
                tris[trisBaseIndex + i6 + 3] = baseIndex + i;
                tris[trisBaseIndex + i6 + 4] = baseIndex + iPlusResPlus1;
                tris[trisBaseIndex + i6 + 5] = baseIndex + iPlusResolution;

                if (i == resolution - 1) {
                    // lower left triangle
                    tris[trisBaseIndex + i6] = baseIndex + resolution - 1;
                    tris[trisBaseIndex + i6 + 1] = baseIndex;
                    tris[trisBaseIndex + i6 + 2] = baseIndex + resolution;

                    // upper right triangle
                    tris[trisBaseIndex + i6 + 3] = baseIndex + resolution - 1;
                    tris[trisBaseIndex + i6 + 4] = baseIndex + resolution;
                    tris[trisBaseIndex + i6 + 5] = baseIndex + resolution * 2 - 1;
                }
            }
        }

        void GenerateUVForPlank() {
            float width = 1.0f / (float)(resolution - 1);
            float currentLength = 0;

            for (int j = 0; j < polyline.Length - 1; j++) {
                float distance = Vector3.Distance(polyline[j], polyline[j + 1]);
                float lengthStart = currentLength;
                float lengthEnd = currentLength + distance;
                int uvStartIndex = j * resolution;

                float uvWidthI = 0.0f;
                float uvWidthIPlus1 = width;

                for (int i = 0; i < resolution - 1; i++) {
                    uv[uvStartIndex + i]                    = new Vector2(lengthStart, uvWidthI);       // bottom-left
                    uv[uvStartIndex + i + 1]                = new Vector2(lengthStart, uvWidthIPlus1);  // bottom-right
                    uv[uvStartIndex + i + resolution]       = new Vector2(lengthEnd, uvWidthI);         // top-left
                    uv[uvStartIndex + i + resolution + 1]   = new Vector2(lengthEnd, uvWidthIPlus1);    // top-right
                    
                    uvWidthI += width;
                    uvWidthIPlus1 += width;
                }

                currentLength = lengthEnd;
            }
        }
        
        Vector3 CalculateNormal(Vector3[] polyline)
        {
            // Use two vectors along the plane to compute the normal
            Vector3 v1 = polyline[1] - polyline[0];
            Vector3 v2 = polyline[2] - polyline[0];

            // Compute the cross product of v1 and v2 to get the normal
            return Vector3.Cross(v1, v2).normalized;
        }

    }
