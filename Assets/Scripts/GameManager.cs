using UnityEngine;
using System.Collections.Generic;
using System;
using static UFO.ShotEmitter;

namespace UFO
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        private AudioManager _audio;
        private PlayerController _player;

        public const int BeatsPerBar = 4;
        public const int NumLanes = 5;
        public const float ScreenHalfWidth = 3.5f;
        public const float ScreenHalfHeight = 3.5f;

        public StageSettings[] Stages;

        [Serializable]
        public struct ShotPool
        {
            public ShotBase Prefab;
            public int MaxCount;

            private Queue<ShotBase> _inactiveShots;
            private Queue<ShotBase> _activeShots;

            public ShotPool Init(Transform parent)
            {
                _inactiveShots = new Queue<ShotBase>();
                _activeShots = new Queue<ShotBase>();
                for (int i = 0; i < MaxCount; i++)
                {
                    ShotBase shot = Instantiate(Prefab, parent);
                    shot.gameObject.SetActive(false);
                    _inactiveShots.Enqueue(shot);
                }

                return this;
            }

            public bool Spawn(Vector3 position, int angle)
            {
                if (_inactiveShots.Count == 0)
                {
                    return false;
                }

                ShotBase shot = _inactiveShots.Dequeue();
                shot.Spawn(position, angle);
                _activeShots.Enqueue(shot);

                return true;
            }

            public void Tick(float deltaTime)
            {
                int count = _activeShots.Count;
                for (int i = 0; i < count; i++)
                {
                    ShotBase shot = _activeShots.Dequeue();
                    if (shot.Tick(deltaTime))
                    {
                        _activeShots.Enqueue(shot);
                    }
                    else
                    {
                        _inactiveShots.Enqueue(shot);
                    }
                }
            }

            public void Clear()
            {
                int count = _activeShots.Count;
                for (int i = 0; i < count; i++)
                {
                    ShotBase shot = _activeShots.Dequeue();
                    shot.gameObject.SetActive(false);
                    _inactiveShots.Enqueue(shot);
                }
            }
        }

        public ShotPool[] ShotPools;

        private Dictionary<string, ShotPool> _shotPoolDict = new Dictionary<string, ShotPool>();

        private List<EnemyBase> _activeEnemies = new List<EnemyBase>();

        private int _currentStage = -1, _currentBar, _currentBeat;

        public double BeatLength { get; private set; }
        public double BarLength { get; private set; }
        private double _nextStageTime, _nextBarTime, _nextBeatTime;

        private int _currentLoop = 1;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void InitTimelines()
        {
            foreach (StageSettings stage in Stages)
            {
                foreach (SpawnInfo spawn in stage.Spawns)
                {
                    // This means only one enemy can be spawned at a time, which is good for flow!
                    stage.Timeline[(spawn.Bar, spawn.Beat)] = (spawn.Lane, spawn.Enemy);
                }
            }
        }

        void Start()
        {
            _audio = GetComponent<AudioManager>();
            _player = PlayerController.Instance;

            foreach (ShotPool pool in ShotPools)
            {
                _shotPoolDict[pool.Prefab.gameObject.name] = pool.Init(transform);
            }

            InitTimelines();
            _nextStageTime = AudioSettings.dspTime + 1.0;
        }

        private double CalculateNextStageTime(double startTime, AudioClip currentClip)
        {
            return startTime + (double)currentClip.samples / currentClip.frequency;
        }

        private void StartNextStage()
        {
            if (++_currentStage == Stages.Length)
            {
                _currentStage = 0;
                _currentLoop++;
            }

            StageSettings stage = Stages[_currentStage];
            double startTime = AudioSettings.dspTime + 1.0;
            _audio.Play(stage.MusicTrack, startTime);

            _currentBar = -1;

            _nextStageTime = CalculateNextStageTime(startTime, stage.MusicTrack);
            _nextBeatTime = _nextBarTime = startTime;

            BeatLength = 60.0 / stage.BPM;
            BarLength = BeatsPerBar * BeatLength;
        }

        private void TrySpawning(StageSettings stage, int bar, int beat)
        {
            if (!stage.Timeline.TryGetValue((bar, beat), out (int, EnemyBase) result))
            {
                Debug.Log($"Bar: {bar},\tBeat: {beat}");
                return;
            }

            (int lane, EnemyBase enemy) = result;
            if (enemy == null)
            {
                return;
            }

            Vector3 pos = new Vector3(lane, ScreenHalfHeight + 1.0f, 0.0f);
            _activeEnemies.Add(Instantiate(enemy, pos, Quaternion.identity)); // TODO: switch to an object pooling scheme per stage.

            Debug.Log($"Bar: {bar},\tBeat: {beat},\tLane: {lane}");
        }

        private void StartNextBar()
        {
            if (_currentStage == -1)
            {
                return;
            }

            TrySpawning(Stages[_currentStage], ++_currentBar, _currentBeat = 0);

            _nextBeatTime += BeatLength;
            _nextBarTime += BarLength;
            Debug.DrawRay(Vector3.zero, Vector3.up, Color.magenta, 0.5f * (float)BeatLength);
        }

        private void StartNextBeat()
        {
            if (_currentStage == -1)
            {
                return;
            }

            TrySpawning(Stages[_currentStage], _currentBar, ++_currentBeat);

            _nextBeatTime += BeatLength;
            Debug.DrawRay(Vector3.zero, Vector3.up, Color.white, 0.5f * (float)BeatLength);
        }

        private int AngleToTarget(ShotBase.TargetType target, Vector3 position)
        {
            if (target == ShotBase.TargetType.Enemy)
            {
                Debug.LogError("The player can't have an aimed shot!");
                return 0;
            }

            return (int)Vector2.SignedAngle(Vector2.down, (_player.transform.position - position).normalized);
        }

        public bool SpawnShot(ShotMode mode, string shotName, Vector3 position, ref int angle)
        {
            if (!_shotPoolDict.TryGetValue(shotName, out ShotPool pool))
            {
                Debug.LogError($"{shotName} was not found in {nameof(_shotPoolDict)}.");
                return false;
            }

            if (mode == ShotMode.Static)
            {
                return pool.Spawn(position, angle);
            }
            
            if (mode == ShotMode.Random)
            {
                angle = UnityEngine.Random.Range(-angle, angle + 1);
            }

            angle = Wrap(angle + AngleToTarget(pool.Prefab.Target, position));
            return pool.Spawn(position, angle);
        }

        private void OnPlayerDeath()
        {
            // TODO: Trigger explosion. If zero extends left then gameover -> hiscore screen. Else respawn the player.
            _player.BombCount = 3;
            _player.PowerCount = 0;

            if (_player.ExtendCount == 0)
            {
                _player.ExtendCount = 2;
                return;
            }

            _player.ExtendCount--;
            _player.Spawn();
        }

        private void OnBombUse()
        {
            // Clears all active shots.
            foreach (ShotPool pool in _shotPoolDict.Values)
            {
                pool.Clear();
            }

            // TODO: damage enemies based on distance.
        }

        void FixedUpdate()
        {
            // Manages timeline.
            double time = AudioSettings.dspTime;
            if (time >= _nextStageTime)
            {
                StartNextStage();
            }
            else if (time >= _nextBarTime)
            {
                StartNextBar();
            }
            else if (time >= _nextBeatTime)
            {
                StartNextBeat();
            }

            // Manages enemies.
            Vector3 playerPos = _player.transform.position;
            foreach (EnemyBase enemy in _activeEnemies)
            {
                enemy.Tick(playerPos, Time.fixedDeltaTime);
            }

            // Manages shots.
            foreach (ShotPool pool in _shotPoolDict.Values)
            {
                pool.Tick(Time.fixedDeltaTime);
            }
        }

        private void OnEnable()
        {
            PlayerController.OnDeath += OnPlayerDeath;
            PlayerController.OnBombUse += OnBombUse;
        }

        private void OnDisable()
        {
            PlayerController.OnDeath -= OnPlayerDeath;
            PlayerController.OnBombUse -= OnBombUse;
        }
    }
}
