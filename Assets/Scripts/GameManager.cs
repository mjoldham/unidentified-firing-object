using UnityEngine;
using System.Collections.Generic;

namespace UFO
{
    public class GameManager : MonoBehaviour
    {
        public EnemyShot EnemyShotPrefab;
        public int EnemyShotPoolSize = 100;

        private Queue<EnemyShot> _inactiveEnemyShots = new Queue<EnemyShot>();
        private Queue<EnemyShot> _activeEnemyShots = new Queue<EnemyShot>();

        public Transform Player;
        private List<EnemyBase> _activeEnemies = new List<EnemyBase>();

        public float ScreenHalfWidth = 3.5f;
        public float ScreenHalfHeight = 3.5f;

        void Start()
        {
            for (int i = 0; i < EnemyShotPoolSize; i++)
            {
                EnemyShot shot = Instantiate(EnemyShotPrefab);
                shot.gameObject.SetActive(false);
                _inactiveEnemyShots.Enqueue(shot);
            }
        }

        public void SpawnEnemyShot(Vector3 position, float angle, float speed)
        {
            if (_inactiveEnemyShots.Count == 0) return;

            EnemyShot shot = _inactiveEnemyShots.Dequeue();
            shot.ResetShot(position, angle, speed);
            _activeEnemyShots.Enqueue(shot);
        }

        void FixedUpdate()
        {
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
        // TODO: set track and params
        // TODO: beat system
        // TODO: spawn enemies
    }
}
