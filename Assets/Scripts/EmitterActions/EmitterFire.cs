using UnityEngine;

namespace UFO
{
    public class EmitterFire : EmitterAction
    {
        public override bool Execute(ref int index)
        {
            index++;
            Emitter.Fire();
            return true;
        }
    }
}
