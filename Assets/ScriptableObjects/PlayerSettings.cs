using UnityEngine;
using UnityEngine.InputSystem;

namespace UFO
{
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "Scriptable Objects/PlayerSettings")]
    public class PlayerSettings : ScriptableObject
    {
        public InputAction LeftAction, RightAction, UpAction, DownAction, FireAction, BombAction;
        public InputAction PauseAction, RestartAction;

        [Min(1)]
        public int SpawnBeats = 2;

        public float SlowSpeed = 4.0f;
        public float FastSpeed = 8.0f;

        public int ShotTimeBuffer = 20;

        public int BombMaxDamage = 50, BombLimit = 5;
        public float BombSaveDuration = 0.2f, InvincibilityDuration = 2.0f;

        [Min(0)]
        public int HitboxDamage = 5;
    }
}
