using System;
using UnityEngine;

namespace UFO
{
    public class EmitterShotType : EmitterAction
    {
        public ShotBase.ShotType NewShotType;

        public override bool Execute()
        {
            Emitter.SetShotType(NewShotType);
            return true;
        }
    }
}
