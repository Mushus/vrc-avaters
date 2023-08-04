using UnityEngine;

namespace Mushus
{
    namespace CharaCreatorN
    {
        public class LayerTexture : MonoBehaviour
        {
            [SerializeField] public Vector2Int size = new Vector2Int(1024, 1024);
            [SerializeField] public Layer[] layers = new Layer[1] { new Layer("背景", null, Color.white) };
            [SerializeField] public string exportPath = "Assets/Texture.png";
        }
    }
}