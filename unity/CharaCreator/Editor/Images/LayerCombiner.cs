using UnityEngine;
using UnityEditor;
using System.IO;

namespace Mushus
{
    namespace CharaCreatorN
    {
        public class LayerCombiner
        {
            public static Shader normalSharder
            {
                get
                {
                    return Shader.Find("Mushus/ImageBlendNormal");
                }
            }

            public static Texture2D Combine(Vector2Int size, Layer[] layers)
            {
                var width = size.x;
                var height = size.y;

                var combinedTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB, 1);

                var dstTexture = new Texture2D(width, height, TextureFormat.ARGB32, false, false);
                Color fillColor = Color.clear;
                Color[] fillPixels = new Color[dstTexture.width * dstTexture.height];

                for (int i = 0; i < fillPixels.Length; i++)
                {
                    fillPixels[i] = fillColor;
                }

                dstTexture.SetPixels(fillPixels);
                dstTexture.Apply();

                for (int i = layers.Length - 1; i >= 0; i--)
                {
                    Layer layer = layers[i];
                    Texture2D texture = layer.texture;
                    if (texture == null)
                    {
                        continue;
                    }

                    var mat = new Material(normalSharder);
                    mat.SetTexture("_DstTex", dstTexture);
                    mat.SetColor("_Color", layer.color);
                    mat.SetInt("_BlendMode", layer.blendMode);
                    mat.SetFloat("_Opacity", layer.opacity);
                    mat.SetVector("_Offset", new Vector4(layer.offset.x, layer.offset.y, layer.offset.width, layer.offset.height));

                    Graphics.Blit(texture, combinedTexture, mat);

                    var oldRender = RenderTexture.active;
                    RenderTexture.active = combinedTexture;

                    dstTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                    dstTexture.Apply();

                    RenderTexture.active = oldRender;
                }

                return dstTexture;
            }

            public static void ExportTexture2D(Texture2D texture, string path)
            {
                var bytes = texture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.Refresh();
            }
        }
    }
}