using UnityEngine;

namespace UFO
{
    public class EmitterWait : EmitterAction
    {
        [Min(1)]
        public int FramesToWait = 1;

        public override bool Execute()
        {
            Emitter.Wait(FramesToWait);
            return false;
        }
    }
}
