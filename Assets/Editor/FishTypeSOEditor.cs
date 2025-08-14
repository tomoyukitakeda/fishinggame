// Assets/Editor/FishTypeSOEditor.cs
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(FishTypeSO))]
public class FishTypeSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        var so = (FishTypeSO)target;

        EditorGUILayout.Space();
        using (new EditorGUI.DisabledScope(so.sprite == null))
        {
            if (GUILayout.Button("アセット名をスプライト名にリネーム"))
            {
                RenameToSpriteName(so);
            }
        }
    }

    private static void RenameToSpriteName(FishTypeSO so)
    {
        if (so == null || so.sprite == null) return;

        string path = AssetDatabase.GetAssetPath(so);
        string current = System.IO.Path.GetFileNameWithoutExtension(path);
        string desired = so.sprite.name;

        if (current == desired) return;

        string error = AssetDatabase.RenameAsset(path, desired);
        if (!string.IsNullOrEmpty(error))
        {
            EditorUtility.DisplayDialog("リネーム失敗", error, "OK");
        }
        else
        {
            AssetDatabase.SaveAssets();
            Debug.Log($"Renamed: {current} -> {desired}");
        }
    }
}
#endif
