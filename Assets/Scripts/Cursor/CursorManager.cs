using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class CursorData
{
    public string stateName;        
    public Texture2D cursorTexture;
    public Vector2 hotspot = Vector2.zero;
}

public class CursorManager : MonoBehaviour
{
    [Header("Cursor Settings")]
    public CursorData[] cursors;

    [Header("Default Cursor")]
    public Texture2D defaultCursor;

    private string currentState = "Default";
    private Dictionary<string, CursorData> cursorMap;

    public static CursorManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeCursors();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitializeCursors()
    {
        cursorMap = new Dictionary<string, CursorData>();
        foreach (var cursor in cursors)
        {
            if (!string.IsNullOrEmpty(cursor.stateName))
                cursorMap[cursor.stateName] = cursor;
        }
    }

    public void SetCursor(string stateName)
    {
        if (currentState == stateName) return;

        currentState = stateName;

        if (cursorMap.TryGetValue(stateName, out CursorData data))
        {
            Cursor.SetCursor(data.cursorTexture, data.hotspot, CursorMode.Auto);
        }
        else
        {
            Cursor.SetCursor(defaultCursor, Vector2.zero, CursorMode.Auto);
        }
    }

    public void ResetToDefault()
    {
        SetCursor("Default");
    }

    public string GetCurrentState() => currentState;
}
