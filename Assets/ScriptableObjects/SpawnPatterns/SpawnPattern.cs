using UnityEngine;

namespace UFO
{
    [CreateAssetMenu(fileName = "SpawnPattern", menuName = "Scriptable Objects/SpawnPattern")]
    public class SpawnPattern : ScriptableObject
    {
        public SpawnInfo[] Spawns;
    }
}
