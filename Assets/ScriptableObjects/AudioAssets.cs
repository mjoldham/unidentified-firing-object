using UnityEngine;

namespace UFO
{
    [CreateAssetMenu(fileName = "AudioAssets", menuName = "Scriptable Objects/AudioAssets")]
    public class AudioAssets : ScriptableObject
    {
        public AudioClip[] TrackList; // TODO: Add metadata e.g. bpm. Possibly better to bundle this in Stage class?
        public AudioClip PlayerFire, PlayerFast, PlayerDie;
        public AudioClip BombGet, BombUse;

        public float MusicVolume = 0.7f;

        //public float PlayerFastDecay = 0.93f, PlayerFastMin = 0.2f;
    }
}
