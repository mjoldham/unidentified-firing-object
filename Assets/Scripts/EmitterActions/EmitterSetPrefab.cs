using System;
using UnityEngine;

namespace UFO
{
    public class EmitterSetPrefab : EmitterAction
    {
        public string ShotPrefabName;

        public override bool Execute(ref int index)
        {
            index++;
            Emitter.ShotPrefabName = ShotPrefabName;
            return true;
        }
    }
}
