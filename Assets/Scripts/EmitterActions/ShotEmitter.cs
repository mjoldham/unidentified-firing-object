using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UFO
{
    public class ShotEmitter : MonoBehaviour
    {
        public ShotParams Parameters;

        public enum ShotMode
        {
            Static,
            Aimed,
            Random
        }

        private ShotMode _currentMode;
        public ShotMode CurrentMode
        {
            get => _currentMode;
            set
            {
                if (value == _currentMode)
                {
                    return;
                }

                if (_currentMode != ShotMode.Static && value == ShotMode.Static)
                {
                    CurrentOffset = _lastAngle;
                }
                else
                {
                    CurrentOffset = 0;
                }

                _currentMode = value;
            }
        }

        private int _currentOffset;
        public int CurrentOffset
        {
            get => _currentOffset;
            set
            {
                if (value >= -180 && value <= 180)
                {
                    _currentOffset = value;
                    return;
                }

                _currentOffset = Wrap(value);
            }
        }

        [HideInInspector]
        public bool IsMirrored;

        private GameManager _gm;

        private EmitterAction[] _sequence;
        private int _currentIndex;

        public class RepeatManager
        {
            public struct RepeatItem
            {
                public int Index, Count;
                public RepeatItem(int index, int times)
                {
                    Index = index;
                    Count = times;
                }
            }

            private Stack<RepeatItem> _repeats = new Stack<RepeatItem>();

            public void StartSequence(int index, int times)
            {
                _repeats.Push(new RepeatItem(index, times));
            }

            public bool EndSequence(ref int index)
            {
                // If there are no preceeding starts, or the last start has been exhausted, go to next step in sequence.
                if (!_repeats.TryPop(out RepeatItem repeat) || repeat.Count == 0)
                {
                    index++;
                    return false;
                }

                // Decrements counter ready for next time we reach this point.
                repeat.Count--;
                _repeats.Push(repeat);

                index = repeat.Index;
                return true;
            }

        }

        private RepeatManager _repeatManager = new RepeatManager();

        [HideInInspector]
        public int WaitFrames, WaitBeats;

        private int _lastAngle;
        private bool _overrideFire;

        public static int Wrap(int angle)
        {
            if (angle < -180)
            {
                return 180 - (-angle % 180);
            }

            if (angle > 180)
            {
                return angle % 180 - 180;
            }

            return angle;
        }

        public static void Tick(IEnumerable<ShotEmitter> emitters, ref bool isFiring, ref int fireFrames)
        {
            if (emitters == null)
            {
                return;
            }

            fireFrames--;
            if (isFiring)
            {
                isFiring = false;
                foreach (ShotEmitter emitter in emitters)
                {
                    isFiring |= emitter.Tick();
                }

                if (isFiring)
                {
                    return;
                }
            }

            if (fireFrames < 0)
            {
                return;
            }

            isFiring = true;
            foreach (ShotEmitter emitter in emitters)
            {
                emitter.Restart();
            }
        }

        // Returns true if still firing.
        public static bool Tick(IEnumerable<ShotEmitter> emitters, bool overrideFire = false)
        {
            if (emitters == null)
            {
                return false;
            }

            bool stillFiring = false;
            foreach (ShotEmitter emitter in emitters)
            {
                stillFiring |= emitter.Tick(overrideFire);
            }

            return stillFiring;
        }

        public void Fire()
        {
            _lastAngle = CurrentOffset;
            if (_overrideFire)
            {
                return;
            }

            if (!_gm.SpawnShot(CurrentMode, Parameters, transform.position, ref _lastAngle))
            {
                Debug.Log($"{gameObject.name} has stalled!");
            }
        }

        // Signals start of repeat sequence.
        public void RepeatStart(ref int index, int times)
        {
            if (++index == _sequence.Length)
            {
                return;
            }

            _repeatManager.StartSequence(index, times);
        }

        // Signals end of repeat sequence.
        public bool RepeatEnd(ref int index)
        {
            return _repeatManager.EndSequence(ref index);
        }

        public void Init()
        {
            _gm = GameManager.Instance;

            _sequence = GetComponents<EmitterAction>();
            foreach (EmitterAction action in _sequence)
            {
                action.Emitter = this;
            }
        }

        public void Restart()
        {
            CurrentOffset = _currentIndex = _lastAngle = 0;
        }

        // Returns true so long as the sequence isn't finished.
        public bool Tick(bool overrideFire = false)
        {
            _overrideFire = overrideFire;
            if (WaitBeats > 0 || --WaitFrames > 0)
            {
                return true;
            }

            bool next;
            do
            {
                if (_currentIndex >= _sequence.Length && !RepeatEnd(ref _currentIndex))
                {
                    return false;
                }

                next = _sequence[_currentIndex].Execute(ref _currentIndex);
            }
            while (next);

            return true;
        }

        private void OnBeat()
        {
            WaitBeats--;
        }

        private void OnEnable()
        {
            GameManager.OnBeat += OnBeat;
        }

        private void OnDisable()
        {
            GameManager.OnBeat -= OnBeat;
        }
    }
}
