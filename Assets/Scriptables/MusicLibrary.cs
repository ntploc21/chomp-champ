using UnityEngine;

[CreateAssetMenu(fileName = "MusicLibrary", menuName = "Audio/Music Library")]
public class MusicLibrary : ScriptableObject
{
  [System.Serializable]
  public class MusicTrack
  {
    public string name;
    public AudioClip clip;
    public float volume = 1f;
    public bool looped = true;
  }

  public MusicTrack[] musicTracks;
}