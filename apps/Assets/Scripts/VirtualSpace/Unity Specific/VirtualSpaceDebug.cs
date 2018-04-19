using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VirtualSpaceDebug : MonoBehaviour
{
    public Text DebugText;

    private List<string> _lastLogs;

    void Awake()
    {
        _lastLogs = new List<string>();
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }
    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Warning)
            return;
        _lastLogs.Insert(0, logString);
        _lastLogs = _lastLogs.GetRange(0, Mathf.Min(_lastLogs.Count, 5));
        //output = logString;
        //stack = stackTrace;
        var dbgString = "";
        foreach (var ll in _lastLogs)
        {
            dbgString += ll + Environment.NewLine;
        }
        DebugText.text = dbgString;
    }
}
