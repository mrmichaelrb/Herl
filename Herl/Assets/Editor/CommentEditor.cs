using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Comment))]
[CanEditMultipleObjects]
class CommentEditor : Editor
{
  const float TextBoxMinHeight = 48.0f; 

  SerializedProperty _text;

  void OnEnable()
  {
    _text = serializedObject.FindProperty("Text");
  }

  public override void OnInspectorGUI()
  {
    serializedObject.Update();
    EditorStyles.textField.wordWrap = true;
    _text.stringValue = EditorGUILayout.TextArea(_text.stringValue, GUILayout.MinHeight(TextBoxMinHeight));
    serializedObject.ApplyModifiedProperties();
  }
}
