using UnityEngine;

namespace UFO
{
    public class EmitterFire : EmitterAction
    {
        public override bool Execute()
        {
            Emitter.Fire();
            return true;
        }
    }
}
