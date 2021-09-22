// Derived from https://forum.unity.com/threads/audio-realistic-sound-rolloff-tool.543362/

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AudioSourceExtensions
{
#if UNITY_EDITOR
  [MenuItem("CONTEXT/AudioSource/Realistic Rolloff")]
  public static void RealisticRolloff(MenuCommand command)
  {
    Undo.RecordObject(command.context, "AudioSource Realistic Rolloff");
    ((AudioSource)command.context).RealisticRolloff();
    EditorUtility.SetDirty(command.context);
  }
#endif

  public static void RealisticRolloff(this AudioSource audioSource)
  {
    AnimationCurve curve = new AnimationCurve(
      new Keyframe(audioSource.minDistance, 1.0f),
      new Keyframe(audioSource.minDistance + (audioSource.maxDistance - audioSource.minDistance) / 4.0f, 0.35f),
      new Keyframe(audioSource.maxDistance, 0.0f)
      );

    audioSource.rolloffMode = AudioRolloffMode.Custom;
    curve.SmoothTangents(1, 0.025f);
    audioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, curve);
  }
}
