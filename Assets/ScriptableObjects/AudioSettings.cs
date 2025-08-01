using UnityEngine;

namespace UFO
{
    [CreateAssetMenu(fileName = "AudioAssets", menuName = "Scriptable Objects/AudioAssets")]
    public class AudioSettings : ScriptableObject
    {
        public AudioClip PlayerFire, PlayerStartDie, PlayerDeath;
        public AudioClip HitHurt, HitShield, Kill;
        public AudioClip ShieldGet, PowerGet, BombGet, ExtendGet, ItemScore;
        public AudioClip ShieldDown, BombUse;
        public AudioClip GameOver;
        public AudioClip MenuLoop;

        public float MusicVolume = 0.7f;
        public float MusicDuckScale = 0.3f;
    }
}
