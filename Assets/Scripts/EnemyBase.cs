using UnityEngine;

namespace UFO
{
    public abstract class EnemyBase : MonoBehaviour
    {
        public float Speed = 2f;

        public abstract void Tick(Vector3 playerPosition, float deltaTime);
    }
}
