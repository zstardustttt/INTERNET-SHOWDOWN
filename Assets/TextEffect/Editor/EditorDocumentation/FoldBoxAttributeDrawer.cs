#if UNITY_EDITOR
#if UNITY_2022_3_OR_NEWER
using UnityEditor;
using UnityEngine;

namespace EasyTextEffects.Editor.EditorDocumentation
{
    [CustomPropertyDrawer(typeof(FoldBoxAttribute))]
    public class FoldBoxAttributeDrawer : PropertyDrawer
    {
        private bool foldoutState = false;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var foldBoxAttribute = attribute as FoldBoxAttribute;
            if (foldBoxAttribute == null) return;

            if (foldBoxAttribute.Inline)
            {
                EditorDocumentation.InlineFoldBox(position, property, label, ref foldoutState);
                if (foldoutState)
                {
                    EditorGUILayout.BeginVertical("HelpBox");
                    DrawContents(foldBoxAttribute);
                    EditorGUILayout.EndVertical();
                }
            }
            else
            {
                EditorGUI.PropertyField(position, property, label);
                EditorDocumentation.BeginFoldBox(foldBoxAttribute.Title, ref foldoutState);
                if (foldoutState)
                {
                    DrawContents(foldBoxAttribute);
                }

                EditorDocumentation.EndFoldBox();
            }
        }

        private void DrawContents(FoldBoxAttribute _attribute)
        {
            for (int i = 0; i < _attribute.Content.Length; i++)
            {
                FoldBoxAttribute.ContentType contentType =
                    (_attribute.ContentTypes[Mathf.Min(i, _attribute.ContentTypes.Length - 1)]);
                if (contentType == FoldBoxAttribute.ContentType.Text)
                {
                    EditorGUILayout.LabelField(_attribute.Content[i], EditorStyles.wordWrappedLabel);
                }
                else
                {
                    var param = _attribute.Content[i].Split(',');
                    var image = AssetDatabase.LoadAssetAtPath<Texture2D>(param[0]);
                    if (image != null)
                    {
                        if (param.Length == 1)
                            EditorGUILayout.LabelField(new GUIContent(image), GUILayout.Height(200));
                        else if (param.Length == 2)
                        {
                            var width = int.Parse(param[1]);
                            var height = image.height * width / image.width;
                            EditorGUILayout.LabelField(new GUIContent(image), GUILayout.Width(width),
                                GUILayout.Height(height));
                        }
                        else if (param.Length == 3)
                            EditorGUILayout.LabelField(new GUIContent(image),
                                GUILayout.Width(int.Parse(param[1])),
                                GUILayout.Height(int.Parse(param[2])));
                    }
                    else
                    {
                        EditorGUILayout.LabelField(_attribute.Content[i], EditorStyles.boldLabel);
                    }
                }
            }
        }
    }
}
#endif
#endif
