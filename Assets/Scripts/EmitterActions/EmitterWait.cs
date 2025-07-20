using System;
using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace UFO
{
    public class EmitterWait : EmitterAction
    {
        [Min(1)]
        public int FramesToWait = 1;

        public override bool Execute(ref int index)
        {
            index++;
            Emitter.WaitFrames = FramesToWait;
            return false;
        }
    }
}
