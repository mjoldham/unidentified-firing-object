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

        public SpriteRenderer BackgroundRenderer;

        public StageSettings[] Stages;

        private Material _bgMaterial;
        private int _scrollID = Shader.PropertyToID(string.Concat("_", nameof(StageSettings.ScrollSpeed)));

        public ShotController BaseShotPrefab;

        [Serializable]
        public struct ShotPool
        {
            public int MaxCount;

            private Queue<ShotController> _inactiveShots;
            private Queue<ShotController> _activeShots;

            public ShotPool Init(ShotController baseShot, Transform parent)
            {
                _inactiveShots = new Queue<ShotController>();
                _activeShots = new Queue<ShotController>();
                for (int i = 0; i < MaxCount; i++)
                {
                    ShotController shot = Instantiate(baseShot, parent);
                    shot.Init();
                    shot.gameObject.SetActive(false);
                    _inactiveShots.Enqueue(shot);
                }

                return this;
            }

            public bool Spawn(ShotParams shotParams, Vector3 position, int angle)
            {
                if (_inactiveShots.Count == 0)
                {
                    return false;
                }

                ShotController shot = _inactiveShots.Dequeue();
                shot.Spawn(shotParams, position, angle);
                _activeShots.Enqueue(shot);

                return true;
            }

            public void Tick(float deltaTime)
            {
                int count = _activeShots.Count;
                for (int i = 0; i < count; i++)
                {
                    ShotController shot = _activeShots.Dequeue();
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
                    ShotController shot = _activeShots.Dequeue();
                    shot.gameObject.SetActive(false);
                    _inactiveShots.Enqueue(shot);
                }
            }
        }

        public ShotPool PlayerShotPool, EnemyShotPool;

        private Queue<EnemyBase> _activeEnemies = new Queue<EnemyBase>();

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
            _bgMaterial = BackgroundRenderer.material;

            PlayerShotPool.Init(BaseShotPrefab, transform);
            EnemyShotPool.Init(BaseShotPrefab, transform);

            InitTimelines();
            _nextStageTime = UnityEngine.AudioSettings.dspTime + 1.0;
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
            double startTime = UnityEngine.AudioSettings.dspTime + 1.0;
            _audio.Play(stage.MusicTrack, startTime);

            _currentBar = -1;

            _nextStageTime = CalculateNextStageTime(startTime, stage.MusicTrack);
            _nextBeatTime = _nextBarTime = startTime;

            BeatLength = 60.0 / stage.BPM;
            BarLength = BeatsPerBar * BeatLength;

            BackgroundRenderer.sprite = stage.Background;
            _bgMaterial.SetFloat(_scrollID, stage.ScrollSpeed);
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

            // TODO: implement object pools for each enemy type that behave like ShotPool.
            //       makes sense to separate by type so max count can be used for difficulty balancing.
            Vector3 pos = new Vector3(lane, ScreenHalfHeight + 1.0f, 0.0f);
            _activeEnemies.Enqueue(Instantiate(enemy, pos, Quaternion.identity));

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

        private int AngleToTarget(ShotController.TargetType target, Vector3 position)
        {
            if (target == ShotController.TargetType.Enemy)
            {
                Debug.LogError("The player can't have an aimed shot!");
                return 0;
            }

            return (int)Vector2.SignedAngle(Vector2.down, (_player.transform.position - position).normalized);
        }

        public bool SpawnShot(ShotMode mode, ShotParams shotParams, Vector3 position, ref int angle)
        {
            ShotPool pool = shotParams.Target == ShotController.TargetType.Enemy ? PlayerShotPool : EnemyShotPool;
            if (mode == ShotMode.Static)
            {
                return pool.Spawn(shotParams, position, angle);
            }
            
            if (mode == ShotMode.Random)
            {
                angle = UnityEngine.Random.Range(-angle, angle + 1);
            }

            angle = Wrap(angle + AngleToTarget(shotParams.Target, position));
            return pool.Spawn(shotParams, position, angle);
        }

        private void OnBombUse()
        {
            // Clears all active shots.
            PlayerShotPool.Clear();
            EnemyShotPool.Clear();

            // TODO: damage enemies based on distance.
        }

        private void OnGameOver()
        {
            // TODO: gameover!
        }

        void FixedUpdate()
        {
            // Manages timeline.
            double time = UnityEngine.AudioSettings.dspTime;
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

            // Updates enemy movement and firing.
            Vector3 playerPos = _player.transform.position;
            int count = _activeEnemies.Count;
            for (int i = 0; i < count; i++)
            {
                EnemyBase enemy = _activeEnemies.Dequeue();
                if (enemy == null)
                {
                    continue;
                }

                enemy.Tick(playerPos, Time.fixedDeltaTime);
                _activeEnemies.Enqueue(enemy);
            }

            // Updates player movement and firing.
            _player.Tick(Time.fixedDeltaTime);

            // Moves shots and checks for collisions.
            PlayerShotPool.Tick(Time.fixedDeltaTime);
            EnemyShotPool.Tick(Time.fixedDeltaTime);

            // TODO: check player-enemy collisions here.
        }

        private void OnEnable()
        {
            PlayerController.OnGameOver += OnGameOver;
            PlayerController.OnBombUse += OnBombUse;
        }

        private void OnDisable()
        {
            PlayerController.OnGameOver -= OnGameOver;
            PlayerController.OnBombUse -= OnBombUse;
        }
    }
}
