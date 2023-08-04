using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;

[CustomEditor(typeof(CharaCreator))]
public class CharaCreatorEditor : Editor
{
    public static string NamePreview = "Preview";
    CharaCreator charaCreator = null;

    void OnEnable()
    {
        charaCreator = target as CharaCreator;
    }

    public override void OnInspectorGUI()
    {
        if (GUILayout.Button("初期化"))
        {
            initializeObjects("TestBody", "TestHead");
        }
        if (GUILayout.Button("FBXを出力"))
        {
            generateFbx("TestBody", "TestHead");
        }
        // if (GUILayout.Button("画像生成"))
        // {
        //     string fileName = "Assets/texture.png";
        //     var tex1 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mushus/CharaCreator/Textures/test.png");
        //     var tex2 = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Mushus/CharaCreator/Textures/test2.png");
        //     var tex = ImageCombiner.CombineTextures(new Texture2D[] { tex1, tex2 });

        //     byte[] pngData = tex.EncodeToPNG();
        //     File.WriteAllBytes(fileName, pngData);
        // }
    }

    private void initializeObjects(string bodyName, string headName)
    {
        var bodyPath = getPartsAssetPath("Body", bodyName);
        var bodyObjects = AssetDatabase.LoadAllAssetsAtPath(bodyPath);
        var assetBodyArmature = ArrayUtility.Find(bodyObjects, isArmature);
        if (assetBodyArmature == null)
        {
            Debug.LogError("BodyのArmatureが見つかりませんでした");
            return;
        }

        var headPath = getPartsAssetPath("Head", headName);
        var headObjects = AssetDatabase.LoadAllAssetsAtPath(headPath);
        var assetHeadArmature = ArrayUtility.Find(headObjects, isArmature);
        if (assetHeadArmature == null)
        {
            Debug.LogError("HeadのArmatureが見つかりませんでした");
            return;
        }

        foreach (Transform child in charaCreator.transform)
        {
            GameObject.DestroyImmediate(child.gameObject);
        }

        var body = castGameObject(assetBodyArmature);
        var head = castGameObject(assetHeadArmature);

        var preview = new GameObject(NamePreview);
        preview.transform.parent = charaCreator.transform;

        var armature = new GameObject("Armature");
        armature.transform.parent = preview.transform;
        mergeArmature(armature, body);
        mergeArmature(armature, head);
        //armature.transform.rotation = Quaternion.Euler(-90, 0, 0);

        var parts = new Part[] {
            new Part() { isBase=true, bone = body.transform.GetChild(0), objects = bodyObjects },
            new Part() { isBase=false, bone = head.transform.GetChild(0), objects = headObjects },
        };

        appendMeshs(preview, armature, parts);
    }

    private void mergeArmature(GameObject armature, GameObject assetArmature)
    {
        // 子要素がなければマージする必要がない
        if (assetArmature.transform.childCount == 0)
        {
            return;
        }
        // Armatureの子要素は一つしかない
        var assetRootBone = assetArmature.transform.GetChild(0);

        // マージ対象はルートボーン名が一致しているもの
        var rootBone = recursiveFindChild(armature.transform, assetRootBone.name);
        if (rootBone == null)
        {
            var child = assetRootBone.GetChild(0);
            var boneInstance = GameObject.Instantiate(assetRootBone.gameObject);
            boneInstance.name = assetRootBone.name;
            boneInstance.transform.SetParent(armature.transform);
            boneInstance.transform.localPosition = new Vector3(assetRootBone.localPosition.x, assetRootBone.localPosition.z, assetRootBone.localPosition.y);
            boneInstance.transform.localRotation = Quaternion.Euler(0, 0, 0);
            boneInstance.transform.localScale = assetRootBone.localScale;
        }
        else
        {
            var children = assetRootBone.childCount;
            for (int i = 0; i < children; ++i)
            {
                var child = assetRootBone.GetChild(i);
                var childBoneInstance = GameObject.Instantiate(child.gameObject);

                childBoneInstance.name = child.name;
                childBoneInstance.transform.SetParent(rootBone);
                // 親を設定し直しただけでは位置関係が崩れるので、ローカル座標を設定し直す
                childBoneInstance.transform.localPosition = child.localPosition;
                childBoneInstance.transform.localRotation = child.localRotation;
                childBoneInstance.transform.localScale = child.localScale;
            }
        }
    }

    private void appendMeshs(GameObject preview, GameObject armature, Part[] parts)
    {
        var rootBone = armature.transform.GetChild(0);
        var bones = rootBone.GetComponentsInChildren<Transform>();
        var boneTable = createBoneTable(bones);
        var partObjects = getPartObjects(parts);
        var srcMeshObj = filterSkinMeshRenderers(partObjects);
        var meshNames = uniqueMeshName(srcMeshObj);

        foreach (var meshName in meshNames)
        {
            var combineInstances = new List<CombineInstance>();
            var combineObjs = new List<GameObject>();
            var mergedObject = new GameObject(meshName);

            foreach (var part in parts)
            {
                var baseBone = boneTable[part.bone.name];
                var srcMeshObjs = filterSkinMeshRenderers(part.objects);

                foreach (var obj in srcMeshObjs)
                {
                    var srcRenderer = obj.GetComponent<SkinnedMeshRenderer>();
                    var srcMesh = srcRenderer.sharedMesh;

                    var dstMesh = new Mesh()
                    {
                        name = srcMesh.name,
                        vertices = srcMesh.vertices,
                        triangles = srcMesh.triangles,
                        uv = srcMesh.uv,
                        subMeshCount = srcMesh.subMeshCount,
                        normals = srcMesh.normals,
                    };

                    var bindposes = new Matrix4x4[srcRenderer.bones.Length];
                    for (var i = 0; i < srcRenderer.bones.Length; ++i)
                    {
                        var poseBone = srcRenderer.bones[i];
                        var mappedBone = boneTable[poseBone.name];
                        bindposes[i] = mappedBone.worldToLocalMatrix * preview.transform.localToWorldMatrix;
                    }

                    dstMesh.boneWeights = srcMesh.boneWeights;
                    dstMesh.bindposes = bindposes;

                    var combineInstance = new CombineInstance();
                    combineInstance.mesh = dstMesh;
                    combineInstance.transform =
                        preview.transform.worldToLocalMatrix * (part.isBase ? preview.transform.localToWorldMatrix : baseBone.transform.localToWorldMatrix);

                    combineInstances.Add(combineInstance);
                    combineObjs.Add(obj);
                }
            }
            var mergedMesh = new Mesh();
            mergedMesh.CombineMeshes(combineInstances.ToArray(), true, true);

            var mergedRenderer = mergedObject.AddComponent<SkinnedMeshRenderer>();
            mergedRenderer.sharedMesh = mergedMesh;
            mergedRenderer.bones = bones;
            mergedRenderer.rootBone = rootBone;
            mergedRenderer.sharedMaterials = new Material[] { new Material(Shader.Find("Standard")) };

            mergedObject.transform.SetParent(preview.transform);

            // ボーン一覧を作成
            var mergedBones = new List<Transform>();
            for (var i = 0; i < combineObjs.Count; i++)
            {
                var srcObj = combineObjs[i];
                var srcRenderer = srcObj.GetComponent<SkinnedMeshRenderer>();
                var srcMesh = srcRenderer.sharedMesh;

                for (var j = 0; j < srcRenderer.bones.Length; j++)
                {
                    var mappedBone = boneTable[srcRenderer.bones[j].name];
                    mergedBones.Add(mappedBone);
                }
            }
            mergedRenderer.bones = mergedBones.ToArray();

            // ブレンドシェイプ取得
            var blendShapeNames = new HashSet<string>();
            var srcMeshes = new Mesh[combineObjs.Count];
            for (var i = 0; i < combineObjs.Count; i++)
            {
                var srcObj = combineObjs[i];
                var srcRenderer = srcObj.GetComponent<SkinnedMeshRenderer>();
                var srcMesh = srcRenderer.sharedMesh;
                srcMeshes[i] = srcMesh;
                for (var j = 0; j < srcMesh.blendShapeCount; j++)
                {
                    blendShapeNames.Add(srcMesh.GetBlendShapeName(j));
                }
            }

            // ブレンドシェイプのマージ
            foreach (var blendShapeName in blendShapeNames)
            {
                var mergedDeltaVertices = new List<Vector3>();
                var mergedDeltaNormals = new List<Vector3>();
                var mergedDeltaTangents = new List<Vector3>();
                for (var i = 0; i < srcMeshes.Length; ++i)
                {
                    var srcMesh = srcMeshes[i];

                    var deltaVertices = new Vector3[srcMesh.vertexCount];
                    var deltaNormals = new Vector3[srcMesh.vertexCount];
                    var deltaTangents = new Vector3[srcMesh.vertexCount];

                    var index = srcMesh.GetBlendShapeIndex(blendShapeName);
                    if (index >= 0)
                    {
                        srcMesh.GetBlendShapeFrameVertices(index, 0, deltaVertices, deltaNormals, deltaTangents);
                    }

                    mergedDeltaVertices.AddRange(deltaVertices);
                    mergedDeltaNormals.AddRange(deltaNormals);
                    mergedDeltaTangents.AddRange(deltaTangents);
                }

                mergedMesh.AddBlendShapeFrame(blendShapeName, 100, mergedDeltaVertices.ToArray(), mergedDeltaNormals.ToArray(), mergedDeltaTangents.ToArray());
            }
        }
    }

    private Object[] getPartObjects(Part[] parts)
    {
        var objects = new Object[0];
        foreach (var part in parts)
        {
            ArrayUtility.AddRange(ref objects, part.objects);
        }
        return objects;
    }

    private HashSet<string> uniqueMeshName(GameObject[] gameObjects)
    {
        var meshNames = new HashSet<string>();
        foreach (var gameObject in gameObjects)
        {
            meshNames.Add(gameObject.name);
        }
        return meshNames;
    }

    private Dictionary<string, Transform> createBoneTable(Transform[] bones)
    {
        var table = new Dictionary<string, Transform>();
        foreach (var bone in bones)
        {
            table.Add(bone.name, bone);
        }
        return table;
    }

    private Transform recursiveFindChild(Transform parent, string childName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == childName)
            {
                return child;
            }
            else
            {
                Transform found = recursiveFindChild(child, childName);
                if (found != null)
                {
                    return found;
                }
            }
        }
        return null;
    }

    private GameObject[] filterSkinMeshRenderers(Object[] assetItems)
    {
        var gameObjects = new List<GameObject>();
        foreach (Object obj in assetItems)
        {
            var gameObject = obj as GameObject;
            if (gameObject == null)
            {
                continue;
            }
            var smr = gameObject.GetComponent<SkinnedMeshRenderer>();
            if (smr == null)
            {
                continue;
            }
            gameObjects.Add(gameObject);
        }

        return gameObjects.ToArray();
    }

    private string getPartsAssetPath(string partsType, string partsName)
    {
        return "Assets/Mushus/CharaCreator/Models/" + partsType + "/" + partsName + ".fbx";
    }

    private bool isArmature(Object obj)
    {
        return obj.name == "Armature";
    }

    private GameObject castGameObject(Object obj)
    {
        var gameObject = obj as GameObject;
        if (gameObject != null)
        {
            return gameObject;
        }

        var transform = obj as Transform;
        return transform.gameObject;
    }

    private void generateFbx(string bodyName, string headName)
    {
        var preview = charaCreator.transform.Find(NamePreview);
        Exporter.ExportBinaryFBX("Assets/test.fbx", preview.gameObject);
    }

    class Part
    {
        public bool isBase;
        public Transform bone;
        public Object[] objects;
    }
}