using System;
using UnityEngine;
using static UFO.ShotEmitter;

namespace UFO
{
    public class EmitterSetMode : EmitterAction
    {
        public ShotEmitter.ShotMode NewMode;

        public override bool Execute(ref int index)
        {
            index++;
            Emitter.CurrentMode = NewMode;
            return true;
        }
    }
}
