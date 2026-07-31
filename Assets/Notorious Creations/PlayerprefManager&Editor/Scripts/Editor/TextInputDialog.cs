using UnityEditor;
using UnityEngine;
using System;

namespace NotoriousCreations.PlayerPrefsEditor
{
    public class TextInputDialog : EditorWindow
    {
        private string description;
        private string inputText;
        private Action<string> onConfirm;
        private bool isInitialized = false;

        public static void Show(string title, string description, string defaultText, Action<string> onConfirm)
        {
            var window = ScriptableObject.CreateInstance<TextInputDialog>();
            window.titleContent = new GUIContent(title);
            window.description = description;
            window.inputText = defaultText;
            window.onConfirm = onConfirm;
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 100);
            window.ShowUtility();
        }

        private void OnGUI()
        {
            if (!isInitialized)
            {
                // Request focus on the input field when window opens
                EditorGUI.FocusTextInControl("TextInputField");
                isInitialized = true;
            }

            EditorGUILayout.Space();
            GUILayout.Label(description, EditorStyles.wordWrappedLabel);
            
            GUI.SetNextControlName("TextInputField");
            inputText = EditorGUILayout.TextField("", inputText);

            EditorGUILayout.Space();

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Confirm", GUILayout.Width(80)) || 
                (Event.current.type == EventType.KeyDown && (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter)))
            {
                onConfirm?.Invoke(inputText);
                Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(80)) || 
                (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
            {
                onConfirm?.Invoke(""); // or null
                Close();
            }

            GUILayout.EndHorizontal();
        }
    }
}
