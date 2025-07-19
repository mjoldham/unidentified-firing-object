using System;
using UnityEngine;

namespace UFO
{
    public class EmitterSetMode : EmitterAction
    {
        public ShotEmitter.ShotMode NewMode;

        public override bool Execute()
        {
            Emitter.SetMode(NewMode);
            return true;
        }
    }
}
