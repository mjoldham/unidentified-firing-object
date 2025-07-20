using UnityEngine;

namespace UFO
{
    public class EmitterResetAngle : EmitterAction
    {
        public override bool Execute(ref int index)
        {
            index++;
            Emitter.CurrentOffset = 0;
            return true;
        }
    }
}
