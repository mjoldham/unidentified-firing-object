using System;
using UnityEngine;

namespace UFO
{
    public abstract class ShotBase : MonoBehaviour
    {
        public enum ShotType
        {
            Aimed,
            Static,
            Random
        }

        public ShotType Type = ShotType.Aimed;
        public float Speed = 5.0f;
        public int Angle = 0;
    }
}
