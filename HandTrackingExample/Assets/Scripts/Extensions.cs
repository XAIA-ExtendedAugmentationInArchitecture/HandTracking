using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DrawingsData;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.OpenXR.Input;
using MixedReality.Toolkit;
using MixedReality.Toolkit.SpatialManipulation;


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
    Renderer[] renderers = gobject.GetComponentsInChildren<Renderer>(true);
    Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
    bool hasBounds = false;

    if (renderers.Length == 0)
    {
        Debug.LogWarning("No renderers found in the GameObject hierarchy.");
        return;
    }

    foreach (Renderer renderer in renderers)
    {
        GameObject currentObject = renderer.gameObject;

        // Exclude LineRenderer from bounds calculation
        if (currentObject.GetComponent<LineRenderer>() != null)
            continue;

        // Get the object's bounds in world space
        Bounds objectBounds = renderer.bounds;

        if (!hasBounds)
        {
            // Initialize bounds with the first valid renderer's bounds
            bounds = objectBounds;
            hasBounds = true;
        }
        else
        {
            // Encapsulate the new object's world bounds
            bounds.Encapsulate(objectBounds);
        }

        Debug.Log($"Object: {currentObject.name} | Local Scale: {currentObject.transform.localScale} | Bounds Size: {bounds.size}");
    }

    if (hasBounds)
    {
        // Add or get an existing BoxCollider
        BoxCollider collider = gobject.GetComponent<BoxCollider>();
        if (collider == null)
        {
            collider = gobject.AddComponent<BoxCollider>();
        }

        // Set the size and center of the collider based on world bounds
        // We need to convert the bounds center back to local space relative to the GameObject
        collider.center = gobject.transform.InverseTransformPoint(bounds.center);
        
        // Adjust the size based on the GameObject's local scale
        Vector3 localScale = gobject.transform.lossyScale;
        collider.size = new Vector3(bounds.size.x / localScale.x, bounds.size.y / localScale.y, bounds.size.z / localScale.z);
        
        Debug.Log($"Collider Center: {collider.center} | Collider Size: {collider.size}");
    }
}

    // public static void SetBoundingBoxColliderAsParent(this GameObject gobject)
    // {
    //     Renderer[] renderers = gobject.GetComponentsInChildren<Renderer>(true);
    //     Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);

    //     bool hasBounds = false;

    //     if (renderers.Length == 0)
    //     {
    //         Debug.LogWarning("No renderers found in the GameObject hierarchy.");
    //     }
    //     else
    //     {
    //         for (int i = 0; i < renderers.Length; i++)
    //         {
    //             GameObject currentObject = renderers[i].gameObject;

    //             // Exclude LineRenderer from bounds calculation
    //             if (currentObject.GetComponent<LineRenderer>() != null)
    //             {
    //                 continue;
    //             }

    //             if (!hasBounds)
    //             {
    //                 // Initialize bounds with the first valid renderer (non-LineRenderer)
    //                 bounds = renderers[i].bounds;
    //                 bounds.size = Vector3.Scale(bounds.size, renderers[i].gameObject.transform.localScale);
    //                 hasBounds = true;
    //             }
    //             else
    //             {
    //                 // Encapsulate remaining objects
    //                 Bounds scaledBounds = renderers[i].bounds;
    //                 scaledBounds.size = Vector3.Scale(scaledBounds.size, renderers[i].gameObject.transform.localScale);
    //                 bounds.Encapsulate(scaledBounds);
    //             }

    //             Debug.Log("Object: " + renderers[i].gameObject.name + " | Local Scale: " + renderers[i].gameObject.transform.localScale + " | Bounds Size: " + bounds.size);
    //         }
    //     }

    //     if (hasBounds)
    //     {
    //         // Add BoxCollider and set its properties
    //         Debug.Log("Heyyy3");
    //         BoxCollider collider = gobject.AddComponent<BoxCollider>();
    //         collider.center = bounds.center; // - gobject.transform.position;
    //         collider.size = bounds.size; //Vector3.Scale(bounds.size, gobject.transform.localScale);
    //     }
    // }


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


    public static void MakeInteractableBoundBox(this GameObject gobject, GameObject boundsPrefab, bool make, bool first = false)
    {
        BoxCollider boundingBox = gobject.GetComponent<BoxCollider>();
        ObjectManipulator objManipulator = gobject.GetComponent<ObjectManipulator>();
        MinMaxScaleConstraint scaleConstraint = gobject.GetComponent<MinMaxScaleConstraint>();
        BoundsControl boundsControl = gobject.GetComponent<BoundsControl>();
        if (boundingBox != null)
        {
            Object.Destroy(boundingBox);
        }
        if (objManipulator!=null)
        {
            Object.Destroy(objManipulator);
        }
        if (boundsControl!=null)
        {
            Object.Destroy(boundsControl.boxInstance);
            Object.Destroy(boundsControl);
        }
        if (scaleConstraint!=null)
        {
            Object.Destroy(scaleConstraint);
        }

        if (make)
        {
            gobject.SetBoundingBoxColliderAsParent();

            if (first && gobject.GetComponent<BoxCollider>() == null)
            {
                // Add a BoxCollider with size 30cm x 30cm x 30cm (0.3 units in Unity)
                BoxCollider boxCollider = gobject.AddComponent<BoxCollider>();
                boxCollider.size = new Vector3(0.3f, 0.3f, 0.3f);

                // Calculate the desired center position in world space
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    Vector3 cameraForward = mainCamera.transform.forward;
                    Vector3 cameraPosition = mainCamera.transform.position;

                    // Position 50cm (0.5 units) in front of the camera
                    Vector3 desiredCenterWorldPosition = cameraPosition + cameraForward * 0.5f;

                    // Convert world position to local space of gobject
                    Vector3 desiredCenterLocalPosition = gobject.transform.InverseTransformPoint(desiredCenterWorldPosition);

                    // Set the center of the BoxCollider
                    boxCollider.center = desiredCenterLocalPosition;
                }
            }

            objManipulator = gobject.AddComponent<ObjectManipulator>();
            objManipulator.AllowedManipulations = TransformFlags.Move | TransformFlags.Rotate | TransformFlags.Scale ;
            objManipulator.AllowedInteractionTypes = InteractionFlags.Near | InteractionFlags.Ray | InteractionFlags.Generic; 
            objManipulator.selectMode = InteractableSelectMode.Multiple;
            scaleConstraint = gobject.AddComponent<MinMaxScaleConstraint>();

            scaleConstraint.MinimumScale = Vector3.one * 0.01f; 
            scaleConstraint.MaximumScale = Vector3.one * 1.00f;

            boundsControl = gobject.AddComponent<BoundsControl>();
            boundsControl.BoundsVisualsPrefab = boundsPrefab;
            boundsControl.ConstraintsManager = gobject.GetComponent<ConstraintManager>();
        
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
        gobject.transform.rotation =  Camera.main.transform.rotation;
        if (xz_to_xy)
        {
            gobject.transform.Rotate(-90f, 0f, 0f);
        }
        
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

