using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using VirtualSpace.Shared;

namespace VirtualSpace
{
    [CustomEditor(typeof(VirtualSpaceCore))]
    public class VirtualSpaceCoreInspector : Editor
    {
        private bool _showReceived = true;
        private bool _showSent = true;

        public void OnEnable()
        {
            //Debug.Log("Activated");
        }

        //public override void OnInspectorGUI()
        //{
        //    VirtualSpaceCore myTarget = (VirtualSpaceCore)target;

        //    EditorGUILayout.Space();
        //    EditorGUILayout.LabelField("Package Received/Sent Statistics", EditorStyles.boldLabel);

        //    _showSent = EditorGUILayout.Foldout(_showSent, "Send Statistics", true);
        //    if (_showSent)
        //    {
        //        if (myTarget.SentInfos != null && myTarget.SentInfos.Count > 0)
        //        {
        //            DrawMessageStatistics(myTarget.SentInterval, myTarget.SentInfos);
        //        }
        //        else
        //        {
        //            DrawNoStastisticsMessage();
        //        }
        //    }

        //    //_showReceived = EditorGUILayout.Foldout(_showReceived, "Receive Statistics", true);
        //    //if (_showReceived)
        //    //{
        //    //    if (myTarget.ReceiveInfos != null && myTarget.ReceiveInfos.Count > 0)
        //    //    {
        //    //        DrawMessageStatistics(myTarget.ReceiveInterval, myTarget.ReceiveInfos);
        //    //    }
        //    //    else
        //    //    {
        //    //        DrawNoStastisticsMessage();
        //    //    }
        //    //}
        //}

        private static void DrawNoStastisticsMessage()
        {
            EditorGUILayout.LabelField("No package info available.");
        }

        //private static void DrawMessageStatistics(int currentInterval, Dictionary<Type, SendInfo> infos)
        //{
        //    EditorGUILayout.IntField("Current interval", currentInterval, GUIStyle.none);

        //    EditorGUI.indentLevel += 1;

        //    foreach (var pair in infos)
        //    {
        //        string messageName = pair.Key.Name;
        //        float kBytesPerSecond = pair.Value.KBytesPerSecond;
        //        float messagesPerSecond = pair.Value.NumberPerSecond;
        //        int intervals = pair.Value.ReceivedAtIntervals;

        //        if (kBytesPerSecond > 0 || messagesPerSecond > 0)
        //        {
        //            EditorGUILayout.LabelField("Message type", messageName, GUIStyle.none);
        //            EditorGUILayout.FloatField("kB/s", kBytesPerSecond, GUIStyle.none);
        //            EditorGUILayout.FloatField("msg/s", messagesPerSecond, GUIStyle.none);
        //            EditorGUILayout.FloatField("Intervals", intervals, GUIStyle.none);
        //        }
        //    }

        //    EditorGUI.indentLevel -= 1;
        //}

        public void OnInspectorUpdate()
        {
            Repaint();
        }
    }
}

