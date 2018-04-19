using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VSSettings))]
public class VSSettingsEditor : Editor {
    public override void OnInspectorGUI()
    {
        VSSettings myTarget = (VSSettings) target;

        var obj = EditorGUILayout.ObjectField(myTarget.Manager, typeof(VSManager), true);
        myTarget.Manager = (VSManager) obj;

        myTarget.StrategySettings.OnInspectorGUI();

        var actualColor = GUI.color;
        GUI.color = new Color(204, 132, 99);
        if (GUILayout.Button("Update"))
        {
            myTarget.SendToBackend();
        }
        GUI.color = actualColor;
    }
}
