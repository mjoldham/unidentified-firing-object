using UnityEngine;
using System.Collections.Generic;
using System;
using static UFO.ShotEmitter;
using UnityEngine.InputSystem;
using static UnityEngine.Timeline.DirectorControlPlayable;

namespace UFO
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        private AudioManager _audio;
        private PlayerController _player;

        public GameObject _mainMenuUI;

        public bool _playing = false;

        public const int BeatsPerBar = 4;
        public const int NumLanes = 5;
        public const float ScreenHalfWidth = 3.5f;
        public const float ScreenHalfHeight = 3.5f;
        public const float CutoffHeight = -ScreenHalfHeight + 1.0f;

        public static Action OnBeat, OnPause;
        public static Action<double> OnUnpause;

        public SpriteRenderer BackgroundRenderer;

        public StageSettings[] Stages;
        private int _spawnIndex;

        private Material _bgMaterial;
        private int _scrollID = Shader.PropertyToID(string.Concat("_", nameof(StageSettings.ScrollSpeed)));

        public struct ShotPool
        {
            public int MaxCount { get; private set; }

            private Queue<ShotController> _inactiveShots;
            private Queue<ShotController> _activeShots;

            public ShotPool(int maxCount, ShotController baseShot, Transform parent)
            {
                MaxCount = maxCount;
                _inactiveShots = new Queue<ShotController>();
                _activeShots = new Queue<ShotController>();
                for (int i = 0; i < MaxCount; i++)
                {
                    ShotController shot = Instantiate(baseShot, parent);
                    shot.Init();
                    _inactiveShots.Enqueue(shot);
                }
            }

            public bool Spawn(ShotParams shotParams, Vector2 position, int angle)
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

        public ShotController BaseShotPrefab;
        public int PlayerShotMaxCount = 32, EnemyShotMaxCount = 256;
        private ShotPool _playerShotPool, _enemyShotPool;

        [Serializable]
        public class EnemyPool
        {
            public int MaxCount;
            public EnemyController EnemyPrefab;

            private Queue<EnemyController> _inactiveEnemies;
            private Queue<EnemyController> _activeEnemies;

            public int Count { get => _activeEnemies.Count; }

            public void Init(Transform parent)
            {
                _inactiveEnemies = new Queue<EnemyController>();
                _activeEnemies = new Queue<EnemyController>();
                for (int i = 0; i < MaxCount; i++)
                {
                    EnemyController enemy = Instantiate(EnemyPrefab, parent);
                    enemy.Init();
                    _inactiveEnemies.Enqueue(enemy);
                }
            }

            public bool Spawn(SpawnInfo spawnInfo)
            {
                if (_inactiveEnemies.Count == 0)
                {
                    return false;
                }

                EnemyController enemy = _inactiveEnemies.Dequeue();
                enemy.Spawn(spawnInfo);
                _activeEnemies.Enqueue(enemy);

                return true;
            }

            public void Tick(float deltaTime)
            {
                int count = _activeEnemies.Count;
                for (int i = 0; i < count; i++)
                {
                    EnemyController enemy = _activeEnemies.Dequeue();
                    if (enemy.Tick(deltaTime))
                    {
                        _activeEnemies.Enqueue(enemy);
                    }
                    else
                    {
                        _inactiveEnemies.Enqueue(enemy);
                    }
                }
            }

            public void Clear()
            {
                int count = _activeEnemies.Count;
                for (int i = 0; i < count; i++)
                {
                    EnemyController enemy = _activeEnemies.Dequeue();
                    enemy.gameObject.SetActive(false);
                    _inactiveEnemies.Enqueue(enemy);
                }
            }
        }

        public EnemyPool[] EnemyPools;
        private Dictionary<string, EnemyPool> _enemyPoolDict = new Dictionary<string, EnemyPool>();

        private int _currentStage = -1, _currentBar, _currentBeat;

        public double BeatLength { get; private set; }
        public double BarLength { get; private set; }
        private double _nextStageTime, _nextBarTime, _nextBeatTime;

        private int _currentLoop = 1;
        private bool _isPaused;
        private double _pauseStart;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

        private void VerifySpawnsAndEnemyPools()
        {
            foreach (StageSettings stage in Stages)
            {
                int bar = 0, beat = 0;
                for (int i = 0; i < stage.Spawns.Length; i++)
                {
                    SpawnInfo spawnInfo = stage.Spawns[i];
                    if (spawnInfo.Bar < bar || (spawnInfo.Bar == bar && spawnInfo.Beat < beat))
                    {
                        Debug.LogError($"{stage.name}'s Spawns[{i}] is out of order, needs to be placed earlier in list.", stage);
                        return;
                    }
                    
                    if (!_enemyPoolDict.ContainsKey(spawnInfo.EnemyPrefab.gameObject.name))
                    {
                        Debug.LogError($"{spawnInfo.EnemyPrefab.gameObject.name} has not been included in the GameManagers list of EnemyPools.");
                        return;
                    }

                    bar = spawnInfo.Bar;
                    beat = spawnInfo.Beat;
                }
            }
        }

        void Start()
        {
            _audio = GetComponent<AudioManager>();
            _player = PlayerController.Instance;
            _bgMaterial = BackgroundRenderer.material;

            _playerShotPool = new ShotPool(PlayerShotMaxCount, BaseShotPrefab, transform);
            _enemyShotPool = new ShotPool(EnemyShotMaxCount, BaseShotPrefab, transform);

            foreach (EnemyPool pool in EnemyPools)
            {
                pool.Init(transform);
                _enemyPoolDict[pool.EnemyPrefab.gameObject.name] = pool;
            }

            VerifySpawnsAndEnemyPools();
            //_nextStageTime = UnityEngine.AudioSettings.dspTime + 1.0;
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

            _spawnIndex = 0;
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

        private void ClearScreen()
        {
            _playerShotPool.Clear();
            _enemyShotPool.Clear();
            foreach (EnemyPool pool in EnemyPools)
            {
                pool.Clear();
            }
        }

        public void RestartStage()
        {
            ClearScreen();
            _currentStage--;
            StartNextStage();
        }

        private void TrySpawningEnemies(SpawnInfo[] spawns)
        {
            Debug.Log($"Bar: {_currentBar},\tBeat: {_currentBeat}");
            if (_spawnIndex == spawns.Length)
            {
                return;
            }

            SpawnInfo spawnInfo = spawns[_spawnIndex];
            while (spawnInfo.Bar == _currentBar && spawnInfo.Beat == _currentBeat)
            {
                EnemyPool pool = _enemyPoolDict[spawnInfo.EnemyPrefab.gameObject.name];
                if (!spawnInfo.CheckForEnemies || pool.Count == 0)
                {
                    if (!pool.Spawn(spawnInfo))
                    {
                        Debug.Log($"Too many {spawnInfo.EnemyPrefab.gameObject.name} active to spawn more!");
                    }
                }

                if (++_spawnIndex == spawns.Length)
                {
                    break;
                }

                spawnInfo = spawns[_spawnIndex];
            }
        }

        private void StartNextBar()
        {
            if (_currentStage == -1)
            {
                return;
            }

            _currentBar++;
            _currentBeat = -1;
            _nextBarTime += BarLength;

            StartNextBeat();
        }

        private void StartNextBeat()
        {
            if (_currentStage == -1)
            {
                return;
            }

            _currentBeat++;
            _nextBeatTime += BeatLength;
            TrySpawningEnemies(Stages[_currentStage].Spawns);

            OnBeat?.Invoke();
        }

        private int AngleToTarget(ShotController.TargetType target, Vector2 position)
        {
            if (target == ShotController.TargetType.Enemy)
            {
                Debug.LogError("The player can't have an aimed shot!");
                return 0;
            }

            return (int)Vector2.SignedAngle(Vector2.down, ((Vector2)_player.transform.position - position).normalized);
        }

        public bool SpawnShot(ShotMode mode, ShotParams shotParams, Vector2 position, ref int angle)
        {
            ShotPool pool = shotParams.Target == ShotController.TargetType.Enemy ? _playerShotPool : _enemyShotPool;
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
            ClearScreen();

            // TODO: damage enemies based on distance.
        }

        private void OnGameOver()
        {
            // TODO: gameover!
            RestartStage();
        }

        private void Unpause()
        {
            _isPaused = false;

            double lostTime = UnityEngine.AudioSettings.dspTime - _pauseStart;
            _pauseStart = 0.0;

            _nextStageTime += lostTime;
            _nextBarTime += lostTime;
            _nextBeatTime += lostTime;

            OnUnpause?.Invoke(lostTime);
        }

        void FixedUpdate()
        {
            if (!_playing)
                return;


            if (_player.CheckRestart())
            {
                Unpause();
                RestartStage();
                return;
            }

            _isPaused = _isPaused ? !_player.CheckPause() : _player.CheckPause();
            if (_isPaused)
            {
                if (_pauseStart == 0.0)
                {
                    _pauseStart = UnityEngine.AudioSettings.dspTime;
                    OnPause?.Invoke();
                }

                return;
            }

            if (_pauseStart > 0.0)
            {
                Unpause();
            }

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
            foreach (EnemyPool pool in EnemyPools)
            {
                pool.Tick(Time.fixedDeltaTime);
            }

            // Updates player movement and firing.
            _player.Tick(Time.fixedDeltaTime);

            // Moves shots and checks for collisions.
            _playerShotPool.Tick(Time.fixedDeltaTime);
            _enemyShotPool.Tick(Time.fixedDeltaTime);

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

        public void StartGame()
        {
            if (_currentStage == -1)
            {
                if (_playing) return;
                _playing = true;

                double startTime = UnityEngine.AudioSettings.dspTime + 1.0;
                _nextStageTime = startTime;
            }
        }
    }
}
