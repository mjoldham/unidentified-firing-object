using UnityEngine;

namespace UFO
{
    public class EmitterRepeatEnd : EmitterAction
    {
        public override bool Execute(ref int index)
        {
            Emitter.RepeatEnd(ref index);
            return true;
        }
    }
}
