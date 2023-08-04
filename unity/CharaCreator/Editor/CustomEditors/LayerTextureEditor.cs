using UnityEngine;
using UnityEditor;
using UnityEditorInternal;

namespace Mushus
{
    namespace CharaCreatorN
    {
        [CustomEditor(typeof(LayerTexture))]
        public class LayerTextureEditor : Editor
        {
            public static readonly string[] blendModePopup = {
                "普通",
                "乗算",
                "スクリーン",
                "オーバーレイ",
                "暗く",
                "明るく",
                "カラードッジ",
                "カラーバーン",
                "ハードライト",
                "ソフトライト",
                "比較",
                "除外",
            };

            public static readonly int previewSize = 256;
            private LayerTexture state;
            private ReorderableList layers;
            private int selectedLayerIndex = -1;
            private Texture2D preview;

            private static GUIStyle headingStyle
            {
                get
                {
                    return new GUIStyle("ShurikenModuleTitle")
                    {
                        font = (new GUIStyle("Label")).font,
                        border = new RectOffset(15, 7, 4, 4),
                        fixedHeight = 22,
                        contentOffset = new Vector2(20, -2),
                    };
                }
            }
            private static void spaceL()
            {
                EditorGUILayout.Space(32);
            }

            private static void spaceM()
            {
                EditorGUILayout.Space(8);
            }

            private static void heading(string label)
            {
                using (new EditorGUILayout.HorizontalScope(headingStyle))
                {
                    GUILayout.Label(label);
                }
                spaceM();
            }

            void OnEnable()
            {
                state = target as LayerTexture;
                setupLayerList();
                previewTexture();
            }

            private void setupLayerList()
            {
                layers = new ReorderableList(serializedObject, serializedObject.FindProperty("layers"), true, true, false, false);

                layers.drawElementCallback += (Rect rect, int index, bool selected, bool focused) =>
                {
                    var property = layers.serializedProperty.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, property, GUIContent.none);
                };

                layers.drawHeaderCallback += rect =>
                {
                    EditorGUI.LabelField(rect, "レイヤー");
                };

                layers.onSelectCallback += list =>
                {
                    selectedLayerIndex = list.index;
                };
            }

            public override void OnInspectorGUI()
            {
                using (EditorGUI.ChangeCheckScope scope = new EditorGUI.ChangeCheckScope())
                {

                    heading("画像設定");

                    EditorGUILayout.LabelField("サイズ");
                    EditorGUI.indentLevel++;
                    state.size.x = EditorGUILayout.IntField("幅", state.size.x);
                    state.size.y = EditorGUILayout.IntField("高さ", state.size.y);
                    EditorGUI.indentLevel--;

                    spaceL();
                    heading("レイヤー設定");

                    var isSelected = isLayerSelected();
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("+"))
                        {
                            addLayer();
                        }
                        EditorGUI.BeginDisabledGroup(!isSelected);
                        if (GUILayout.Button("-"))
                        {
                            removeLayer();
                        }
                        EditorGUI.EndDisabledGroup();
                    }
                    layers.DoLayoutList();
                    serializedObject.ApplyModifiedProperties();

                    if (isSelected)
                    {
                        var layer = state.layers[selectedLayerIndex];
                        layer.name = EditorGUILayout.TextField("レイヤー名", layer.name);
                        layer.blendMode = EditorGUILayout.Popup("ブレンドモード", layer.blendMode, blendModePopup);
                        layer.texture = EditorGUILayout.ObjectField("テクスチャ", layer.texture, typeof(Texture), false) as Texture2D;
                        layer.opacity = EditorGUILayout.Slider("透明度", layer.opacity, 0, 1);
                        layer.color = EditorGUILayout.ColorField("着色", layer.color);
                        layer.offset = EditorGUILayout.RectField("オフセット", layer.offset);
                    }

                    spaceL();
                    heading("書き出し設定");

                    state.exportPath = EditorGUILayout.TextField("出力先", state.exportPath);

                    spaceM();

                    using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                    {
                        EditorGUILayout.Space();
                        GUILayout.Label(preview, GUILayout.Width(previewSize), GUILayout.Height(previewSize));
                        EditorGUILayout.Space();
                    }

                    if (GUILayout.Button("書き出し"))
                    {
                        writeTexture();
                    }

                    if (scope.changed)
                    {
                        previewTexture();
                    }
                }
            }
            private void addLayer()
            {
                if (isLayerSelected() == false)
                {
                    selectedLayerIndex = 0;
                }
                // HACK: ArrayUtility 使ってもうちょっと簡易的にかけそうな気がする
                var layers = state.layers;
                var newLayers = new Layer[layers.Length + 1];
                for (int i = 0; i < newLayers.Length; i++)
                {
                    if (i < selectedLayerIndex)
                    {
                        newLayers[i] = layers[i];
                    }
                    else if (i == selectedLayerIndex)
                    {
                        newLayers[i] = new Layer("新しいレイヤー", null, Color.white);
                    }
                    else
                    {
                        newLayers[i] = layers[i - 1];
                    }

                }
                state.layers = newLayers;
            }

            private void removeLayer()
            {
                // HACK: ArrayUtility 使ってもうちょっと簡易的にかけそうな気がする
                var layers = state.layers;
                var newLayers = new Layer[layers.Length - 1];
                for (int i = 0; i < layers.Length; i++)
                {
                    if (i < selectedLayerIndex)
                    {
                        newLayers[i] = layers[i];
                    }
                    else if (i == selectedLayerIndex)
                    {
                        continue;
                    }
                    else
                    {
                        newLayers[i - 1] = layers[i];
                    }

                }
                state.layers = newLayers;
            }

            private bool isLayerSelected()
            {
                return 0 <= selectedLayerIndex && selectedLayerIndex < state.layers.Length;
            }

            private void previewTexture()
            {
                preview = LayerCombiner.Combine(new Vector2Int(previewSize, previewSize), state.layers);
            }

            private void writeTexture()
            {
                preview = LayerCombiner.Combine(state.size, state.layers);
                LayerCombiner.ExportTexture2D(preview, state.exportPath);
            }
        }
    }
}