using UnityEngine;

namespace UFO
{
    public abstract class BaseSpawnable : MonoBehaviour
    {

        public SpawnParams Parameters;
        public abstract void Spawn(SpawnInfo spawnInfo);
    }
}
