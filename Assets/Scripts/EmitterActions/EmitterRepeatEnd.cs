using UnityEngine;

namespace UFO
{
    public class EmitterRepeatEnd : EmitterAction
    {
        public override bool Execute()
        {
            Emitter.RepeatEnd();
            return true;
        }
    }
}
