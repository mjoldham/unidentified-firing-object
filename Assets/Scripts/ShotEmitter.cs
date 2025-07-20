using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UFO
{
    public class ShotEmitter : MonoBehaviour
    {
        public string ShotPrefabName;

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

        public struct RepeatSequence
        {
            public int Start, End, Times;

            public RepeatSequence(int start, int end, int times)
            {
                Start = start;
                End = end;
                Times = times;
            }
        }

        private Stack<RepeatSequence> _repeatSequences = new Stack<RepeatSequence>();

        [HideInInspector]
        public int WaitFrames;

        private int _lastAngle;

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

        public void Fire()
        {
            _lastAngle = CurrentOffset;
            _gm.SpawnShot(CurrentMode, ShotPrefabName, transform.position, ref _lastAngle);
        }

        // Signals start of repeat sequence.
        public void RepeatStart(ref int index, int times)
        {
            int start = ++index;
            while (start < _sequence.Length && _sequence[start] is EmitterRepeatStart)
            {
                start++;
            }

            if (start == _sequence.Length)
            {
                return;
            }

            // Don't know where end is yet, set to zero for now.
            _repeatSequences.Push(new RepeatSequence(start, 0, times));
        }

        // Signals end of repeat sequence.
        public bool RepeatEnd(ref int index)
        {
            if (_repeatSequences.Count == 0)
            {
                index++;
                return false;
            }

            RepeatSequence repeat = _repeatSequences.Pop();
            if (repeat.End == 0)
            {
                repeat.End = index;
            }
            else if (repeat.End != index)
            {
                index++;
                _repeatSequences.Push(repeat);
                return false;
            }

            if (--repeat.Times > 0)
            {
                _repeatSequences.Push(repeat);
            }

            index = repeat.Start;
            return true;
        }

        private void Start()
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
            Tick();
        }

        // Returns true so long as the sequence isn't finished.
        public bool Tick()
        {
            if (--WaitFrames > 0)
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
    }
}
