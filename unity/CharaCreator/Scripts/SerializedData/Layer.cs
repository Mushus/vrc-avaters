
using System;
using UnityEngine;

namespace Mushus
{
    namespace CharaCreatorN
    {
        [Serializable]
        public class Layer
        {
            [SerializeField] public string name;
            [SerializeField] public int blendMode;
            [SerializeField] public Texture2D texture;
            [SerializeField] public Color color;
            [SerializeField] public float opacity = 1;
            [SerializeField] public Rect offset = new Rect(0, 0, 1, 1);

            public Layer(string name, Texture2D texture, Color color)
            {
                this.name = name;
                this.texture = texture;
                this.color = color;
            }
        }
    }
}