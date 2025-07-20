using UnityEngine;
using static UFO.ShotEmitter;

namespace UFO
{
    public class EmitterOffset : EmitterAction
    {
        [Range(-180, 180)]
        public int Degrees;

        public override bool Execute(ref int index)
        {
            index++;
            Emitter.CurrentOffset += Emitter.IsMirrored ? -Degrees : Degrees;
            return true;
        }
    }
}
