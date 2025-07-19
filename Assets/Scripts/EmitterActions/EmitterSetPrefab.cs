using UnityEngine;

namespace UFO
{
    public class EmitterSetPrefab : EmitterAction
    {
        public string ShotPrefabName;

        public override bool Execute()
        {
            Emitter.SetPrefab(ShotPrefabName);
            return true;
        }
    }
}
