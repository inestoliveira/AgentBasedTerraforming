using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MapGenerator))]
public class MapGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        MapGenerator mapGen = (MapGenerator)target;

        if (mapGen.autoUpdate)
        {
            if (DrawDefaultInspector())
            {
                mapGen.GenerateMap();
            }
        }
        else
        {
            DrawDefaultInspector();

            if (GUILayout.Button("Generate"))
            {
                mapGen.GenerateMap();
            }
        }
    }
}
