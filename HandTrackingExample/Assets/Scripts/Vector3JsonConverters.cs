/*
    Author: MESH AG - Eleni Vasiliki Alexi
    License: MIT
    Created on: 14/02/2024 12:00

    This Unity file contains custom JSON converters for Vector3, Vector3[], and Dictionary<string, Vector3[]>.
    These converters are designed to facilitate serialization and deserialization of Vector3 data from JSON format.

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
