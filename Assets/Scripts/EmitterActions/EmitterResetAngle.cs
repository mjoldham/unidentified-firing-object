using UnityEngine;

namespace UFO
{
    public class EmitterResetAngle : EmitterAction
    {
        public override bool Execute()
        {
            Emitter.ResetAngle();
            return true;
        }
    }
}
