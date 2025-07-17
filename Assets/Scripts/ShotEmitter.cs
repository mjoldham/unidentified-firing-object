using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UFO
{
    public class ShotEmitter : MonoBehaviour
    {
        public ShotBase.ShotType CurrentShotType { get; private set; }
        public int CurrentOffset { get; private set; }

        private GameManager _gm;

        private List<EmitterAction> _sequence = new List<EmitterAction>();
        private int _currentIndex;
        private LinkedList<(int, int)> _repeatSequences = new LinkedList<(int, int)>();

        private double _waitTime;

        // TODO: get rid of this and use GM object pooling instead.
        private List<EnemyShot> _shots = new List<EnemyShot>();

        private int _lastAngle;

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

        private int AngleToPlayer()
        {
            Vector2 playerPosition = _gm.Player.position;
            Vector2 toPlayer = (playerPosition - (Vector2)transform.position).normalized;
            return (int)Vector2.SignedAngle(Vector2.down, toPlayer);
        }

        public void SetShotType(ShotBase.ShotType shot)
        {
            if (shot == CurrentShotType)
            {
                return;
            }

            if (CurrentShotType != ShotBase.ShotType.Static && shot == ShotBase.ShotType.Static)
            {
                CurrentOffset = _lastAngle;
            }
            else
            {
                CurrentOffset = 0;
            }

            CurrentShotType = shot;
        }

        public void Fire()
        {
            // TODO: could request bullet type from GM? Then GM handles bullet ticks etc.
            EnemyShot shot = Instantiate(_gm.EnemyShotPrefab, transform.position, Quaternion.identity);
            _shots.Add(shot);
            if (CurrentShotType == ShotBase.ShotType.Static)
            {
                _lastAngle = shot.Angle = CurrentOffset;
                return;
            }

            shot.Angle = AngleToPlayer();
            if (CurrentShotType == ShotBase.ShotType.Aimed)
            {
                shot.Angle += CurrentOffset;
            }
            else if (CurrentShotType == ShotBase.ShotType.Random)
            {
                shot.Angle += UnityEngine.Random.Range(-CurrentOffset, CurrentOffset + 1);
            }

            _lastAngle = shot.Angle = Wrap(shot.Angle);
        }

        // Offsets current angle.
        public void OffsetAngle(int degrees)
        {
            CurrentOffset = Wrap(CurrentOffset + degrees);
        }

        public void Wait(double beats)
        {
            _waitTime = AudioSettings.dspTime + beats * _gm.BeatLength;
        }

        // Signals start of repeat sequence.
        public void RepeatStart(int times)
        {
            _repeatSequences.AddLast((_currentIndex + 1, times));
        }

        // Signals end of repeat sequence. Pops sequence from list if count is exhausted.
        public bool RepeatEnd(bool isImplicit = false)
        {
            if (_repeatSequences.Count == 0)
            {
                return false;
            }

            (int index, int count) = _repeatSequences.Last.Value;
            if (--count == 0)
            {
                _repeatSequences.RemoveLast();
                if (!isImplicit)
                {
                    _sequence.RemoveAt(_currentIndex);
                }
            }
            else
            {
                _repeatSequences.Last.Value = (index, count);
            }

            _currentIndex = index;
            return true;
        }

        private void Start()
        {
            _gm = GameManager.Instance;
            _sequence = GetComponents<EmitterAction>().ToList();
            foreach (EmitterAction action in _sequence)
            {
                action.Emitter = this;
            }
        }

        private void FixedUpdate()
        {
            // TODO: strictly for testing, GM should handle this.
            Tick();
            foreach (EnemyShot shot in _shots)
            {
                shot.Tick(Time.fixedDeltaTime);
            }
        }

        public void Tick()
        {
            bool next;
            do
            {
                if (_sequence.Count == 0)
                {
                    return;
                }

                if (_currentIndex == _sequence.Count && !RepeatEnd(true))
                {
                    // TODO: figure out repeat sequence behaviour.
                    _currentIndex = 0;
                    CurrentOffset = 0;
                    return;
                }

                if (AudioSettings.dspTime < _waitTime)
                {
                    return;
                }

                next = _sequence[_currentIndex].Execute();
                _currentIndex++;
            }
            while (next);
        }
    }
}
