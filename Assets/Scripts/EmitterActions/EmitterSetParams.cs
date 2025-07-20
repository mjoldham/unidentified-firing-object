using System;
using UnityEngine;

namespace UFO
{
    public class EmitterSetParams : EmitterAction
    {
        public ShotParams Parameters;

        public override bool Execute(ref int index)
        {
            index++;
            Emitter.Parameters = Parameters;
            return true;
        }
    }
}
