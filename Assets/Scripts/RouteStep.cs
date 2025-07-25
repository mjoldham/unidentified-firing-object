using UnityEngine;

namespace UFO
{
    public class RouteStep : MonoBehaviour
    {
        [Min(0)]
        public int BeatsToComplete;

        [Min(0)]
        public int NumBursts;

        public bool WaitToFire;
    }
}
