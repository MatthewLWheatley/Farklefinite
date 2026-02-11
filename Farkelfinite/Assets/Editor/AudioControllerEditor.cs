// Assets/Editor/AudioControllerEditor.cs
using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(audioController))]
public class AudioControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        audioController controller = (audioController)target;

        GUILayout.Space(10);

        if (GUILayout.Button("Load Music from Folder"))
        {
            LoadAudioClips("Assets/Audio/Music", ref controller.music);
        }

        if (GUILayout.Button("Load Ambient from Folder"))
        {
            LoadAudioClips("Assets/Audio/Ambient", ref controller.ambient);
        }

        if (GUILayout.Button("Load SFX from Folder"))
        {
            LoadAudioClips("Assets/Audio/SFX", ref controller.audioClips);
            LoadAudioNames("Assets/Audio/SFX", ref controller.audioNames);
        }
    }

    void LoadAudioClips(string folderPath, ref AudioClip[] clipArray)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });

        if (guids.Length == 0)
        {
            Debug.LogWarning($"No audio clips found in {folderPath}");
            return;
        }

        clipArray = new AudioClip[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            clipArray[i] = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        EditorUtility.SetDirty(target);
        Debug.Log($"Loaded {clipArray.Length} clips from {folderPath}");
    }

    void LoadAudioNames(string folderPath, ref string[] nameArray)
    {
        string[] guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderPath });

        nameArray = new string[guids.Length];

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            nameArray[i] = clip.name;
        }

        EditorUtility.SetDirty(target);
    }
}