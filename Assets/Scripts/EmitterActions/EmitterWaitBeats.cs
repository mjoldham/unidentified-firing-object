using UnityEngine;

namespace UFO
{
    public class EmitterWaitBeats : EmitterAction
    {
        [Min(1)]
        public int BeatsToWait = 1;

        public override bool Execute(ref int index)
        {
            index++;
            Emitter.WaitBeats = BeatsToWait;
            return false;
        }
    }
}
