using UnityEngine;
using System.Collections.Generic;

namespace UFO
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        private AudioManager _audio;

        public const int BeatsPerBar = 4;
        public const int NumLanes = 5;
        public const float ScreenHalfWidth = 3.5f;
        public const float ScreenHalfHeight = 3.5f;

        public StageSettings[] Stages;

        public EnemyShot EnemyShotPrefab;
        public int EnemyShotPoolSize = 100;

        private Queue<EnemyShot> _inactiveEnemyShots = new Queue<EnemyShot>();
        private Queue<EnemyShot> _activeEnemyShots = new Queue<EnemyShot>();

        public Transform Player;
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
            for (int i = 0; i < EnemyShotPoolSize; i++)
            {
                EnemyShot shot = Instantiate(EnemyShotPrefab);
                shot.gameObject.SetActive(false);
                _inactiveEnemyShots.Enqueue(shot);
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

            Vector3 pos = new Vector3(lane, ScreenHalfHeight + 1.0f, -1.0f);
            Instantiate(enemy, pos, Quaternion.identity); // TODO: switch to an object pooling scheme per stage.

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

        public void SpawnEnemyShot(Vector3 position, int angle, float speed)
        {
            if (_inactiveEnemyShots.Count == 0) return;

            EnemyShot shot = _inactiveEnemyShots.Dequeue();
            shot.ResetShot(position, angle, speed);
            _activeEnemyShots.Enqueue(shot);
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
            Vector3 playerPos = Player.position;

            foreach (var enemy in _activeEnemies)
            {
                enemy.Tick(playerPos, Time.fixedDeltaTime);
            }

            int count = _activeEnemyShots.Count;

            for (int i = 0; i < count; i++)
            {
                EnemyShot shot = _activeEnemyShots.Dequeue();
                shot.Tick(Time.fixedDeltaTime);

                if (Mathf.Abs(shot.transform.position.x) > ScreenHalfWidth ||
                    Mathf.Abs(shot.transform.position.y) > ScreenHalfHeight)
                {
                    shot.gameObject.SetActive(false);
                    _inactiveEnemyShots.Enqueue(shot);
                }
                else
                {
                    _activeEnemyShots.Enqueue(shot);
                }
            }
        }

        public void RegisterEnemy(EnemyBase e)
        {
            _activeEnemies.Add(e);
        }


        // TODO: spawn player
    }
}
