using UnityEngine;
using UnityEngine.InputSystem;

namespace UFO
{
    [CreateAssetMenu(fileName = "PlayerSettings", menuName = "Scriptable Objects/PlayerSettings")]
    public class PlayerSettings : ScriptableObject
    {
        public InputAction LeftAction, RightAction, UpAction, DownAction, FireAction, BombAction;

        public PlayerShot ShotPrefab;

        public float SlowSpeed = 4.0f;
        public float FastSpeed = 8.0f;

        public int ShotLimit = 20;
        public int ShotFrameDelay = 5;
        public int ShotTimeBuffer = 20;

        public int BombLimit = 5;
        public int PowerLimit = 5;

        public float BombSaveDuration = 0.2f;
    }
}
