using System;
using UnityEngine;

namespace UFO
{
    public abstract class ShotBase : MonoBehaviour
    {
        [Serializable]
        public enum ShotType
        {
            Aimed,
            Static,
            Random
        }

        public ShotType Type = ShotType.Aimed;
        public float Speed = 5.0f;
        public float Angle = 0.0f;

        // TODO: Probably want a ShotPattern class too?
    }
}
