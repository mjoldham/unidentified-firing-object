using UnityEngine;

namespace UFO
{
    public class EmitterMirror : EmitterAction
    {
        public override bool Execute()
        {
            Emitter.Mirror();
            return true;
        }
    }
}
