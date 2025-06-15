using UnityEngine;

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
  [System.Serializable]
  public class SoundEffect
  {
    public string name;
    public AudioClip clip;
    public float volume = 1f;
    public float pitch = 1f;
  }

  public SoundEffect[] soundEffects;
}
