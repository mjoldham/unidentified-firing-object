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

        public ShotMode CurrentMode { get; private set; }
        public int CurrentOffset { get; private set; }

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

        private double _waitFrames;

        private int _lastAngle;
        private bool _isMirrored;

        private int Wrap(int angle)
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

        // TODO: shots should have flags for what they damage, based off that change this function.
        private int AngleToTarget()
        {
            Vector2 playerPosition = _gm.Player.position;
            Vector2 toPlayer = (playerPosition - (Vector2)transform.position).normalized;
            return (int)Vector2.SignedAngle(Vector2.down, toPlayer);
        }

        public void SetMode(ShotMode mode)
        {
            _currentIndex++;
            if (mode == CurrentMode)
            {
                return;
            }

            if (CurrentMode != ShotMode.Static && mode == ShotMode.Static)
            {
                CurrentOffset = _lastAngle;
            }
            else
            {
                CurrentOffset = 0;
            }

            CurrentMode = mode;
        }

        public void SetPrefab(string name)
        {
            _currentIndex++;
            ShotPrefabName = name;
        }

        public void Fire()
        {
            _currentIndex++;
            if (CurrentMode == ShotMode.Static)
            {
                _lastAngle = CurrentOffset;
                _gm.SpawnShot(ShotPrefabName, transform.position, _lastAngle);
                return;
            }

            _lastAngle = AngleToTarget();
            if (CurrentMode == ShotMode.Aimed)
            {
                _lastAngle += CurrentOffset;
            }
            else if (CurrentMode == ShotMode.Random)
            {
                _lastAngle += UnityEngine.Random.Range(-CurrentOffset, CurrentOffset + 1);
            }

            _lastAngle = Wrap(_lastAngle);
            _gm.SpawnShot(ShotPrefabName, transform.position, _lastAngle);
        }

        public void ResetAngle()
        {
            _currentIndex++;
            CurrentOffset = 0;
        }

        // Offsets current angle.
        public void OffsetAngle(int degrees)
        {
            _currentIndex++;
            CurrentOffset = Wrap(CurrentOffset + (_isMirrored ? -degrees : degrees));
        }

        public void Wait(int frames)
        {
            _currentIndex++;
            _waitFrames = frames;
        }

        // Signals start of repeat sequence.
        public void RepeatStart(int times)
        {
            int start = ++_currentIndex;
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
        public bool RepeatEnd()
        {
            if (_repeatSequences.Count == 0)
            {
                _currentIndex++;
                return false;
            }

            RepeatSequence repeat = _repeatSequences.Pop();
            if (repeat.End == 0)
            {
                repeat.End = _currentIndex;
            }
            else if (repeat.End != _currentIndex)
            {
                _currentIndex++;
                _repeatSequences.Push(repeat);
                return false;
            }

            if (--repeat.Times > 0)
            {
                _repeatSequences.Push(repeat);
            }

            _currentIndex = repeat.Start;
            return true;
        }

        public void Mirror()
        {
            _currentIndex++; // TODO: refactor so it's clear these functions must incr index!
            _isMirrored = !_isMirrored;
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
            if (--_waitFrames > 0)
            {
                return true;
            }

            bool next;
            do
            {
                if (_currentIndex >= _sequence.Length && !RepeatEnd())
                {
                    return false;
                }

                next = _sequence[_currentIndex].Execute();
            }
            while (next);

            return true;
        }
    }
}
