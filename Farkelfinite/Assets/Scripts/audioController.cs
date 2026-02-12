using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class audioController : MonoBehaviour
{
    public AudioClip[] audioClips;
    public string[] audioNames;
    public AudioClip[] music;
    public AudioClip[] ambient;

    public AudioSource musicSource;
    public AudioSource ambientSource;
    public AudioSource sfxSource;

    public float masterVolume;

    public Setting settingsManager;

    private void Start()
    {
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.loop = true;

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;

        ChangeVolume();

        StartCoroutine(StartBackGround());
    }

    public void ChangeVolume()
    {
        musicSource.volume = settingsManager._musicVolume * (settingsManager._mainVolume);
        sfxSource.volume = settingsManager._sfxVolume * (settingsManager._mainVolume);
        ambientSource.volume = settingsManager._ambientVolume * (settingsManager._mainVolume);
    }

    public void PlayAudio(int id)
    {
        if (id >= 0 && id < audioClips.Length && audioClips[id] != null)
        {
            sfxSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            sfxSource.PlayOneShot(audioClips[id]);
        }
        else
        {
            Debug.LogWarning($"Invalid audio clip id: {id}");
        }
    }

    public void PlayAudio(int id, float pitchRange)
    {
        if (id >= 0 && id < audioClips.Length && audioClips[id] != null)
        {
            sfxSource.pitch = UnityEngine.Random.Range(0.9f - pitchRange, 1.1f + pitchRange);
            sfxSource.PlayOneShot(audioClips[id]);
        }
        else
        {
            Debug.LogWarning($"Invalid audio clip id: {id}");
        }
    }

    public void PlayAudio(string name)
    {
        for (int i = 0; i < audioNames.Length; i++)
        {
            if (audioNames[i] == name)
            {
                PlayAudio(i);
                return;
            }
        }
        Debug.LogWarning($"Audio clip '{name}' not found in audioNames array");
    }

    public IEnumerator StartBackGround()
    {
        int musicIndex = 0;
        int ambientIndex = 0;
        List<AudioClip> musiclist = new List<AudioClip>();
        musiclist = music.ToList();

        int n = musiclist.Count;
        System.Random rand = new System.Random();
        music = musiclist.OrderBy(x => rand.Next()).ToArray();

        while (true)
        {
            if (music.Length > 0 && music[musicIndex] != null)
            {
                Debug.Log($"playing {music[musicIndex].name}");
                musicSource.clip = music[musicIndex];
                musicSource.Play();
                musicIndex = (musicIndex + 1) % music.Length;
            }

            if (ambient.Length > 0 && ambient[ambientIndex] != null)
            {

                Debug.Log($"playing {ambient[ambientIndex].name}");
                ambientSource.clip = ambient[ambientIndex];
                ambientSource.Play();
                ambientIndex = (ambientIndex + 1) % ambient.Length;
            }

            float musicLength = musicSource.clip != null ? musicSource.clip.length : 0;
            float ambientLength = ambientSource.clip != null ? ambientSource.clip.length : 0;
            float waitTime = Mathf.Max(musicLength, ambientLength);

            yield return new WaitForSeconds(waitTime);
        }
    }
}