using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class FindMissingScripts : EditorWindow
{
    [MenuItem("Tools/Find Missing Scripts In Project")]
    static void ShowWindow() { GetWindow<FindMissingScripts>("Find Missing Scripts"); }

    void OnGUI()
    {
        if (GUILayout.Button("Search Scenes and Prefabs"))
        {
            FindInOpenScenes();
            FindInPrefabs();
            Debug.Log("Búsqueda completada.");
        }
    }

    static void FindInOpenScenes()
    {
        var roots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var root in roots) FindMissing(root);
    }

    static void FindInPrefabs()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab");
        foreach (var g in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(g);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) FindMissing(prefab);
        }
    }

    static void FindMissing(GameObject go)
    {
        var components = go.GetComponentsInChildren<Component>(true);
        foreach (var c in components)
        {
            if (c == null)
            {
                Debug.LogWarning($"Missing script in GameObject: {GetGameObjectPath(go)}", go);
            }
        }
    }

    static string GetGameObjectPath(GameObject go)
    {
        string path = go.name;
        Transform t = go.transform.parent;
        while (t != null)
        {
            path = t.name + "/" + path;
            t = t.parent;
        }
        return path;
    }
}