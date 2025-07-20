using System;
using UnityEngine;

namespace UFO
{
    public class EmitterMirror : EmitterAction
    {
        public override bool Execute(ref int index)
        {
            index++;
            Emitter.IsMirrored = !Emitter.IsMirrored;
            return true;
        }
    }
}
