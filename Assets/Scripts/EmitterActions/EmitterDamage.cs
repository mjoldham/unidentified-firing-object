using UnityEngine;

namespace UFO
{
    public class EmitterDamage : EmitterAction
    {
        [Min(0.1f)]
        public float Damage = 1.0f;

        public override bool Execute(ref int index)
        {
            index++;
            Emitter.Damage = Damage;
            return true;
        }
    }
}
