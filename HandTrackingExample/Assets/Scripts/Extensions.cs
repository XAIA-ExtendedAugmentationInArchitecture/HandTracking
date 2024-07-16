using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DrawingsData;


public static class GameObjectExtensions
{ 
    public static GameObject FindObject(this GameObject parent, string name)
    {
        Transform[] trs= parent.GetComponentsInChildren<Transform>(true);
        foreach(Transform t in trs){
            if(t.name == name){
                return t.gameObject;
            }
        }
        return null;
    }

    public static void Orient(this GameObject gobject, Frame frame)
    {
        Quaternion  rotation = QuaternionExtensions.SetRotationfromXZAxis(frame.xaxis, frame.zaxis);
        Vector3 position = frame.point;

        gobject.transform.position = position;
        gobject.transform.rotation = rotation;

    }

    public static void SetColor(this GameObject gobject, Color color)
    {
         // Get or add a MeshRenderer component
        Renderer renderer = gobject.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = gobject.AddComponent<MeshRenderer>();
        }
        // Set the color for the child
        renderer.material.color = color;
    }

    public static void SetMaterial(this GameObject gobject, Material material)
    {
         // Get or add a MeshRenderer component
        Renderer renderer = gobject.GetComponent<Renderer>();
        if (renderer == null)
        {
            renderer = gobject.AddComponent<MeshRenderer>();
        }
        // Set the color for the child
        renderer.material = material;
    }

    public static void SetColorForChildren(this GameObject parent, Color color, string childName = "all")
    {
        // Iterate through all child objects
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;

            // Check if the child's name matches the target name or if the target name is "all"
            if (childName == "all" || child.name == childName)
            {
                child.SetColor(color);
            }
        }
    }

    public static void SetMaterialForChildren(this GameObject parent, Material material, string childName = "all", string exceptName = "none")
    {
        // Iterate through all child objects
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;

            // Check if the child's name matches the target name or if the target name is "all"
            if ((childName == "all" || child.name == childName) & child.name != exceptName)
            {
                child.SetMaterial(material);
            }
        }
    }

    public static void SetActiveChildren(this GameObject parent, bool activate, string childName ="all", string exceptName = "none", string exceptTag = "none" )
    {
        // Iterate through all child objects
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;

            // Check if the child's name matches the target name
            if ((childName == "all" || child.name == childName) & child.name != exceptName & child.tag != exceptTag)
            {
                child.SetActive(activate);
            }
        }
    }
    public static void DestroyGameObjectAndChildren(this GameObject parent, string name="all", bool andParent=true)
    {
        if (parent!=null)
        {
            // Iterate through all child objects
            for (int i = 0; i < parent.transform.childCount; i++)
            {
                // Get the child GameObject
                GameObject child = parent.transform.GetChild(i).gameObject;

                if ((name == "all" || name == child.name))
                {
                    // Recursively destroy children of the child
                    DestroyGameObjectAndChildren(child);
                }
                
            }

            // Destroy the target GameObject
            if (andParent)
            {
                Object.Destroy(parent);
            } 
        }  
    }

    public static void SetBoundingBoxCollider(this GameObject gobject)
    {
        // Store the original rotation
        Quaternion originalRotation = gobject.transform.localRotation;

        // Reset rotation temporarily
        gobject.transform.localRotation = Quaternion.identity;

        Renderer renderer = gobject.GetComponent<Renderer>();
        Bounds bounds = renderer.bounds;

        // Calculate the center of the bounds in world space
        Vector3 center = bounds.center;

        // Restore the original rotation
        gobject.transform.rotation = originalRotation;

        // Create a collider based on bounds
        BoxCollider collider = gobject.AddComponent<BoxCollider>();
        collider.center = bounds.center - gobject.transform.position;
        collider.size = bounds.size;
    }
    public static void SetBoundingBoxColliderAsParent(this GameObject gobject)
    {
        Renderer[] renderers = gobject.GetComponentsInChildren<Renderer>();
        Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

        if (renderers.Length == 0)
        {
            Debug.LogWarning("No renderers found in the GameObject hierarchy.");
            
        }
        else
        {
            bounds = renderers[0].bounds;

            for (int i = 1; i < renderers.Length; i++)
            {
                bounds.Encapsulate(renderers[i].bounds);
            }
        }

        BoxCollider collider = gobject.AddComponent<BoxCollider>();
        Renderer renderer =  gobject.AddComponent<MeshRenderer>();
        renderer.bounds = bounds;
        collider.center = bounds.center - gobject.transform.position;
        collider.size = bounds.size;
        
    }
    public static void SetActiveChildrenColliders(this GameObject parent, bool activate, string childName ="all")
    {
        // Iterate through all child objects
        for (int i = 0; i < parent.transform.childCount; i++)
        {
            GameObject child = parent.transform.GetChild(i).gameObject;

            // Check if the child's name matches the target name
            if (childName == "all" || child.name == childName )
            {
                Collider collider = child.GetComponent<Collider>();
                if (collider != null)
                {
                    collider.enabled = activate;
                }
            }
        }
    }

    public static void InstantiateInside(this GameObject gobject, GameObject container, bool xz_to_xy = true)
    {
        //TODO: Add a margin in the scaled 
        
        GameObject clone = Object.Instantiate(gobject, container.transform, false);

        Vector3 containerSize = container.GetComponent<BoxCollider>().size;
        Vector3 cloneSize = clone.GetComponent<BoxCollider>().size;

        Debug.Log (containerSize.x +"/" + containerSize.y + "/" + containerSize.z);

        float scaleX = containerSize.x / cloneSize.x;
        float scaleY = containerSize.y / cloneSize.y;
        float scaleZ = containerSize.z / cloneSize.z;

        if (xz_to_xy)
        {
            scaleY = containerSize.y / cloneSize.z;
            scaleZ = containerSize.z / cloneSize.y;
        }

        float scaleFactor = Mathf.Min(scaleX, scaleY, scaleZ);
        Debug.Log (scaleX +"/" + scaleY + "/" + scaleZ);
        clone.transform.parent = container.transform;
        clone.transform.localScale *= scaleFactor;
        clone.transform.position = container.transform.position;
        clone.transform.rotation = container.transform.rotation;

        if (xz_to_xy)
        {
            clone.transform.Rotate(-90f, 0f, 0f);
        }

    }

    public static void InstantiateInsideButton(this GameObject gobject, GameObject container, float sizeX, float sizeY, bool xz_to_xy = true)
    {
        GameObject clone = Object.Instantiate(gobject, container.transform, false);
        Vector3 cloneSize = clone.GetComponent<BoxCollider>().size;

        float scaleX = sizeY / cloneSize.x;
        float scaleY = sizeX / cloneSize.y;

        if (xz_to_xy)
        {
            scaleY = sizeX / cloneSize.z;
        }

        float scaleFactor = Mathf.Min(scaleX, scaleY);

        clone.transform.parent = container.transform;
        clone.transform.localScale *= scaleFactor -0.002f;  //TODO: Add a margin in the scaled
        clone.transform.position = container.transform.position;
        clone.transform.rotation = container.transform.rotation;

        if (xz_to_xy)
        {
            clone.transform.Rotate(-90f, 0f, 0f);
        }

    }
    public static void FaceCamera(this GameObject gobject, bool xz_to_xy = true, Vector3? translationVector = null)
    {
        Vector3 finalTranslation = translationVector ?? Vector3.zero;

        gobject.transform.position = Camera.main.transform.position + Camera.main.transform.rotation * finalTranslation;
        Debug.Log("Bazingaaa1");
        gobject.transform.rotation =  Camera.main.transform.rotation;
        if (xz_to_xy)
        {
            gobject.transform.Rotate(-90f, 0f, 0f);
        }
        Debug.Log("Bazingaaa2");
        
    }
}

public static class ColorExtensions
{
    public static Color RainbowColor(this Color color, float factor)
    {
        float hue = Mathf.Lerp(0f, 1f, factor); // Interpolate between 0 (Red) and 1 (Violet)
        return new Color(Color.HSVToRGB(hue, 1f, 1f).r, Color.HSVToRGB(hue, 1f, 1f).g, Color.HSVToRGB(hue, 1f, 1f).b, 0.1f);

    }
}

public static class QuaternionExtensions
{
    public static Quaternion SetRotationfromXZAxis(Vector3 xaxis, Vector3 zaxis)
    {
        xaxis.Normalize();
        zaxis.Normalize();
        Quaternion rotation = Quaternion.LookRotation(zaxis, Vector3.Cross(xaxis, -zaxis));
        
        return rotation;

    }
}

public static class MathExtensions
{
    public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
    {
        return (value - fromMin) / (fromMax - fromMin) * (toMax - toMin) + toMin;
    }
}

public static class DictionaryExtensions
{
    public static string ToDebugString<TKey, TValue> (this IDictionary<TKey, TValue> dictionary)
    {
        
        return string.Join("\n", dictionary.Select(kv => kv.Key + ":\t" + kv.Value).ToArray());
    }
}

