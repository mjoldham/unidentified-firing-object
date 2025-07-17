using UnityEngine;

namespace UFO
{
    public class EmitterOffset : EmitterAction
    {
        [Range(-180, 180)]
        public int Degrees;

        public override bool Execute()
        {
            Emitter.OffsetAngle(Degrees);
            return true;
        }
    }
}
