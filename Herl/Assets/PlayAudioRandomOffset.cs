using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayAudioRandomOffset : MonoBehaviour
{
  void Awake()
  {
    AudioSource audio = GetComponent<AudioSource>();

    AudioClip clip = audio.clip;

    if (clip != null)
    {
      if (clip.LoadAudioData())
      {
        float clipLength = clip.length;
        float startTime = Random.Range(0.0f, clipLength) % clipLength;

        audio.time = startTime;
        audio.Play();
      }
    }
  }
}
