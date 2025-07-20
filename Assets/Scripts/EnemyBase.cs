using UnityEngine;

namespace UFO
{
    public abstract class EnemyBase : MonoBehaviour
    {
        public float Speed = 2.0f;

        protected ShotEmitter[] _emitters;
        protected bool _isFiring;

        protected void Start()
        {
            _emitters = GetComponentsInChildren<ShotEmitter>();
        }

        public abstract void Tick(Vector3 playerPosition, float deltaTime);
    }
}
