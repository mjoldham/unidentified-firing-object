using UnityEngine;

namespace UFO
{
    [CreateAssetMenu(fileName = "AudioAssets", menuName = "Scriptable Objects/AudioAssets")]
    public class AudioAssets : ScriptableObject
    {
        public AudioClip PlayerFire, PlayerDie;
        public AudioClip BombGet, BombUse;

        public float MusicVolume = 0.7f;
    }
}
