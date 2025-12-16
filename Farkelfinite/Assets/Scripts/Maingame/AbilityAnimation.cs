using UnityEngine;

[CreateAssetMenu(fileName = "New Ability Animation", menuName = "Farkle/Ability Animation")]
public class AbilityAnimation : ScriptableObject
{
    [Header("Particle Effects")]
    [Tooltip("Particle system to spawn at dice position")]
    public GameObject particlePrefab;

    [Tooltip("Offset from dice position")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("How long to wait for particles to finish")]
    public float particleDuration = 1f;

    [Header("Screen Effects")]
    [Tooltip("Should the screen shake?")]
    public bool enableScreenShake = false;

    [Tooltip("Shake intensity")]
    public float shakeIntensity = 0.2f;

    [Tooltip("Shake duration")]
    public float shakeDuration = 0.3f;

    [Header("UI Effects")]
    [Tooltip("Popup text prefab (optional)")]
    public GameObject popupTextPrefab;

    [Tooltip("Text to display (use {value} for dynamic values)")]
    public string popupText = "+{value}";

    [Tooltip("Color of popup text")]
    public Color popupColor = Color.white;

    [Header("Audio")]
    [Tooltip("Sound effect to play")]
    public AudioClip soundEffect;

    [Tooltip("Volume (0-1)")]
    [Range(0f, 1f)]
    public float volume = 1f;

    [Header("Timing")]
    [Tooltip("Delay before starting animation")]
    public float startDelay = 0f;

    [Tooltip("Total duration to wait (overrides particle duration if set)")]
    public float totalDuration = 0f;
}