using UnityEngine;
using UnityEditor;

namespace Mushus
{
    namespace CharaCreatorN
    {
        [CustomPropertyDrawer(typeof(Layer))]
        public class LayerDrawer : PropertyDrawer
        {
            public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
            {
                var nameProperty = property.FindPropertyRelative("name");
                EditorGUI.LabelField(position, nameProperty.stringValue);
            }
        }
    }
}