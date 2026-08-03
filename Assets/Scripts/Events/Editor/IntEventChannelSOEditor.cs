using UnityEditor;
using UnityEngine;

namespace SobGameJam.Events.Editor
{
    [CustomEditor(typeof(IntEventChannelSO))]
    public class IntEventChannelSOEditor : UnityEditor.Editor
    {
        private int _testValue = 0;

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            // The event might only be meaningful during play mode. 
            // We can optionally disable the button if not playing, 
            // but in some architectures, raising events outside playmode is valid.
            // GUI.enabled = Application.isPlaying;

            IntEventChannelSO e = target as IntEventChannelSO;

            GUILayout.Space(10);
            
            EditorGUILayout.LabelField("Testing", EditorStyles.boldLabel);
            _testValue = EditorGUILayout.IntField("Test Value", _testValue);

            if (GUILayout.Button("Raise Event"))
            {
                e.RaiseEvent(_testValue);
            }

            // GUI.enabled = true;
        }
    }
}
