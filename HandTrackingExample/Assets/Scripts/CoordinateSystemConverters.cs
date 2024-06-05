/*
    Author: MESH AG - Eleni Vasiliki Alexi
    License: MIT
    Created on: 14/02/2024 12:00
    Updated on: 04/06/2024 12:00
    Author: Huma Lab - Eleni Vasiliki Alexi

    This Unity file contains custom JSON converters for int[], Vector3, Vector3[], and Dictionary<string, Vector3[]>.
    These converters are designed to facilitate serialization and deserialization of Vector3 data and int[] data  from JSON format.

    The IntArrayConverter class is responsible for handling: int[], like MeshFaces

    The Vector3Converter class is responsible for handling: Vector3

    The Vector3ArrayConverter class is responsible for handling: Vector3[]

    The Vector3DictionaryConverter class is responsible for handling: Dictionary<string, Vector3[]>

    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    All the converters SWAPS the y and z components of each Vector3 object to convert from a right-handed 
    coordinate system to a left-handed coordinate system.
    !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

*/

using UnityEngine;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

public class IntArrayConverter : JsonConverter<int[]>
{
    public override void WriteJson(JsonWriter writer, int[] value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        for (int i = 0; i < value.Length; i += 3)
        {
            if (i + 2 >= value.Length)
            {
                throw new JsonException("Invalid int[] length for writing JSON.");
            }
            // Reverse the order to switch the winding order from left-handed to right-handed
            writer.WriteValue(value[i]);
            writer.WriteValue(value[i + 2]);
            writer.WriteValue(value[i + 1]);
        }
        writer.WriteEndArray();
    }

    public override int[] ReadJson(JsonReader reader, Type objectType, int[] existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JArray array = JArray.Load(reader);
        List<int> faceList = new List<int>();

        for (int i = 0; i < array.Count; i += 3)
        {
            if (i + 2 >= array.Count)
            {
                throw new JsonException("Invalid JSON array for int[] conversion");
            }

            // Reverse the order to switch the winding order from right-handed to left-handed
            faceList.Add((int)array[i]);
            faceList.Add((int)array[i + 2]);
            faceList.Add((int)array[i + 1]);
        }

        return faceList.ToArray();
    }

    public override bool CanRead => true;
    public override bool CanWrite => true;
}

public class IntArrayArrayConverter : JsonConverter<int[][]>
{
    public override void WriteJson(JsonWriter writer, int[][] value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        foreach (var innerArray in value)
        {
            writer.WriteStartArray();
            for (int i = 0; i < innerArray.Length; i += 3)
            {
                if (i + 2 >= innerArray.Length)
                {
                    throw new JsonException("Invalid inner int[] length for writing JSON.");
                }
                // Reverse the order to switch the winding order from left-handed to right-handed
                writer.WriteValue(innerArray[i]);
                writer.WriteValue(innerArray[i + 2]);
                writer.WriteValue(innerArray[i + 1]);
            }
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }

    public override int[][] ReadJson(JsonReader reader, Type objectType, int[][] existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JArray outerArray = JArray.Load(reader);
        List<int[]> resultList = new List<int[]>();

        foreach (var innerArray in outerArray.Children<JArray>())
        {
            List<int> innerList = new List<int>();
            for (int i = 0; i < innerArray.Count; i += 3)
            {
                if (i + 2 >= innerArray.Count)
                {
                    throw new JsonException("Invalid inner JSON array for int[][] conversion");
                }

                // Reverse the order to switch the winding order from right-handed to left-handed
                innerList.Add((int)innerArray[i]);
                innerList.Add((int)innerArray[i + 2]);
                innerList.Add((int)innerArray[i + 1]);
            }
            resultList.Add(innerList.ToArray());
        }

        return resultList.ToArray();
    }
}

public class Vector2Converter : JsonConverter<Vector2>
{
    public override void WriteJson(JsonWriter writer, Vector2 value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.x);
        writer.WriteValue(value.y);
        writer.WriteEndArray();
    }

    public override Vector2 ReadJson(JsonReader reader, System.Type objectType, Vector2 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JArray array = JArray.Load(reader);

        if (array == null || array.Count < 2)
        {
            throw new JsonException("Invalid JSON array for Vector2 conversion");
        }

        return new Vector2((float)array[0], (float)array[1]);
    }

    public override bool CanRead => true;

    public override bool CanWrite => true;
}

public class Vector2ArrayConverter : JsonConverter<Vector2[]>
{
    public override void WriteJson(JsonWriter writer, Vector2[] value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        foreach (var vector in value)
        {
            writer.WriteStartArray();
            writer.WriteValue(vector.x);
            writer.WriteValue(vector.y);
            writer.WriteEndArray();
        }
        writer.WriteEndArray();
    }

    public override Vector2[] ReadJson(JsonReader reader, System.Type objectType, Vector2[] existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JArray outerArray = JArray.Load(reader);
        var resultList = new List<Vector2>();

        foreach (var innerArray in outerArray.Children<JArray>())
        {
            if (innerArray.Count != 2)
            {
                throw new JsonException("Invalid inner JSON array for Vector2 conversion. Each inner array must have exactly 2 elements.");
            }

            resultList.Add(new Vector2((float)innerArray[0], (float)innerArray[1]));
        }

        return resultList.ToArray();
    }

    public override bool CanRead => true;

    public override bool CanWrite => true;
}

public class Vector3Converter : JsonConverter<Vector3>
{
    public override void WriteJson(JsonWriter writer, Vector3 value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        writer.WriteValue(value.x);
        writer.WriteValue(value.z);
        writer.WriteValue(value.y);
        writer.WriteEndArray();
    }

    public override Vector3 ReadJson(JsonReader reader, System.Type objectType, Vector3 existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JArray array = JArray.Load(reader);

        if (array == null || array.Count < 3)
        {   
            throw new JsonException("Invalid JSON array for Vector3 conversion");
        }

        // Swapping y and z components to convert from right-handed to left-handed coordinate system
        return new Vector3((float)array[0], (float)array[2], (float)array[1]);
    }

    public override bool CanRead => true;

    public override bool CanWrite => true;
}

public class Vector3ArrayConverter : JsonConverter<Vector3[]>
{
    
    public override void WriteJson(JsonWriter writer, Vector3[] value, JsonSerializer serializer)
    {
        writer.WriteStartArray();

        foreach (Vector3 vector in value)
        {
            writer.WriteStartArray();
            writer.WriteValue(vector.x);
            writer.WriteValue(vector.z);
            writer.WriteValue(vector.y);
            writer.WriteEndArray();
        }

        writer.WriteEndArray();
    }

    public override Vector3[] ReadJson(JsonReader reader, System.Type objectType, Vector3[] existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        JArray array = JArray.Load(reader);
        List<Vector3> vectorList = new();

        foreach (JArray innerArray in array)
        {
            if (innerArray.Count != 3 || innerArray.Any(item => item.Type != JTokenType.Float))
            {
                throw new JsonException("Invalid JSON array for Vector3 array conversion");
            }

            // Swapping y and z components to convert from right-handed to left-handed coordinate system
            vectorList.Add(new Vector3((float)innerArray[0], (float)innerArray[2], (float)innerArray[1]));
        }

        return vectorList.ToArray();
    }

    public override bool CanRead => true;

    public override bool CanWrite => true;
}

public class Vector3DictionaryConverter : JsonConverter
{
    public override bool CanConvert(Type objectType)
    {
        return (objectType == typeof(Dictionary<string, Vector3[]>));
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        Dictionary<string, Vector3[]> dict = (Dictionary<string, Vector3[]>)value;

        writer.WriteStartObject();

        foreach (var pair in dict)
        {
            writer.WritePropertyName(pair.Key);
            writer.WriteStartArray();

            foreach (Vector3 vector in pair.Value)
            {
                writer.WriteStartArray();
                writer.WriteValue(vector.x);
                writer.WriteValue(vector.z);
                writer.WriteValue(vector.y);
                writer.WriteEndArray();
            }

            writer.WriteEndArray();
        }

        writer.WriteEndObject();
    }

    public override object ReadJson(JsonReader reader, System.Type objectType, object existingValue, JsonSerializer serializer)
    {
        JObject jsonObject = JObject.Load(reader);

        Dictionary<string, Vector3[]> result = new ();

        foreach (var property in jsonObject.Properties())
        {
            string key = property.Name;
            JArray array = (JArray)property.Value;

            // Swapping y and z components to convert from right-handed to left-handed coordinate system
            Vector3[] vectorArray = array.Select(item => new Vector3((float)item[0],(float)item[2],(float)item[1])).ToArray();

            result.Add(key, vectorArray);
        }
        return result;
    }

    public override bool CanRead => true;

    public override bool CanWrite => true;
}
