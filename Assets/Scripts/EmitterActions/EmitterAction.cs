using System;
using UnityEngine;

namespace UFO
{
    // Attach these to the ShotEmitter object in the order they're supposed to execute. Derived components expose appropriate parameters.
    public abstract class EmitterAction : MonoBehaviour
    {
        [HideInInspector]
        public ShotEmitter Emitter;

        // Returns true if next step in sequence should be performed in same frame.
        public abstract bool Execute(ref int index);
    }
}
