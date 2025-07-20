using UnityEngine;

namespace UFO
{
    [CreateAssetMenu(fileName = "ShotParams", menuName = "Scriptable Objects/ShotParams")]
    public class ShotParams : ScriptableObject
    {
        public AnimationClip Clip;
        public ShotController.TargetType Target;
        public float Speed;
    }
}
