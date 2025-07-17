using UnityEngine;

namespace UFO
{
    public class EmitterRepeatStart : EmitterAction
    {
        [Min(1)]
        public int Times = 1;

        public override bool Execute()
        {
            Emitter.RepeatStart(Times);
            return true;
        }
    }
}
