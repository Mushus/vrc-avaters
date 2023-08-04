using UnityEngine;
using UnityEditor;

public static class AddObject
{

    [MenuItem("GameObject/Chara Creator", false, 0)]
    static void Init()
    {
        var obj = new GameObject("Chara Creator");
        obj.AddComponent<CharaCreator>();
        Selection.activeGameObject = obj;
    }
}