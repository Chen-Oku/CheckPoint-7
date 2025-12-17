using UnityEditor;
using UnityEngine;

public class SelectionDebugger
{
    [MenuItem("Tools/Debug/Print Selected Components")]
    static void PrintSelectedComponents()
    {
        var obj = Selection.activeGameObject;
        if (obj == null) { Debug.Log("No GameObject selected."); return; }
        Debug.Log($"Selected: {obj.name} ({obj.GetInstanceID()})");
        var comps = obj.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            Debug.Log($"  [{i}] {(comps[i] == null ? "<MISSING>" : comps[i].GetType().Name)}");
        }
    }
}