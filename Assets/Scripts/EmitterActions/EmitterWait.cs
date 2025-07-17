using UnityEngine;

namespace UFO
{
    public class EmitterWait : EmitterAction
    {
        public enum BeatSubdivision
        {
            QuaterNote = 1,
            EightNote = 2,
            Triplet = 3,
            SixteenthNote = 4,
            ThirtySecondNote = 8
        }

        public BeatSubdivision BeatType;

        [Min(1)]
        public int BeatsToWait = 1;

        public override bool Execute()
        {
            double beats = (double)BeatsToWait / (int)BeatType;
            Emitter.Wait(beats);
            return false;
        }
    }
}
