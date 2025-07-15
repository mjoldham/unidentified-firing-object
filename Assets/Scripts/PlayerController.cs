using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UFO
{
    public class PlayerController : MonoBehaviour
    {
        public InputAction LeftAction, RightAction, UpAction, DownAction, FireAction, BombAction;

        public PlayerShot ShotPrefab;

        public float SlowSpeed = 4.0f; // TODO: put params in ScriptableObject.
        public float FastSpeed = 6.0f;

        public int ShotLimit = 10;
        public int ShotFrameDelay = 4;

        public int BombLimit = 5;
        public int PowerLimit = 5;

        public float ScreenHalfWidth = 3.5f;
        public float ScreenHalfHeight = 3.5f;

        public static Action OnSpawn, OnDeath, OnFire, OnBomb;

        public bool IsShielded;
        public int BombCount;
        public int PowerCount;

        private Queue<PlayerShot> _inactiveShots = new Queue<PlayerShot>();
        private Queue<PlayerShot> _activeShots = new Queue<PlayerShot>();
        private int _shotFrame;

        private void Start()
        {
            for (int i = 0; i < ShotLimit; i++)
            {
                PlayerShot shot = Instantiate(ShotPrefab);
                shot.gameObject.SetActive(false);
                _inactiveShots.Enqueue(shot);
            }
        }

        private void UpdateShots()
        {
            // TODO: Move shots and test for collisions. Despawn on collision or leaving screen.
            int count = _activeShots.Count;
            for (int i = 0; i < count; i++)
            {
                PlayerShot shot = _activeShots.Dequeue();

                Vector3 move = Quaternion.AngleAxis(shot.Angle, Vector3.back) * Vector3.down;
                shot.transform.position += shot.Speed * Time.fixedDeltaTime * move;

                if (shot.transform.position.y > ScreenHalfHeight)
                {
                    shot.gameObject.SetActive(false);
                    _inactiveShots.Enqueue(shot);
                    continue;
                }

                // TODO: test collision (premove & postmove).
                //if (hit)
                //{
                //    // Apply damage and score.

                //    shot.gameObject.SetActive(false);
                //    _inactiveShots.Enqueue(shot);
                //    continue;
                //}

                _activeShots.Enqueue(shot);
            }
        }

        public void Spawn()
        {
            Debug.Log("Spawn");
            // TODO: Player takes ~4s to drift from bottom offscreen to top of no-fire zone.

            OnSpawn?.Invoke();
        }

        public void Die()
        {
            Debug.Log("Die");
            // TODO: Trigger explosion, reset any multipliers/powerups.

            OnDeath?.Invoke();
        }

        private void Fire()
        {
            Debug.Log("Fire!");

            if (_shotFrame++ < ShotFrameDelay || _activeShots.Count >= ShotLimit)
            {
                return;
            }

            _shotFrame = 0;
            PlayerShot shot = _inactiveShots.Dequeue();
            shot.transform.position = transform.position;
            shot.gameObject.SetActive(true);

            _activeShots.Enqueue(shot);

            // TODO: change pattern based on power level.

            OnFire?.Invoke();
        }

        private void Bomb()
        {
            Debug.Log("Bomb!");
            // TODO: clear all projectiles.
            // TODO: damage enemies based on distance.

            OnBomb?.Invoke();
        }

        private void FixedUpdate()
        {
            UpdateShots();

            Vector2 move = Vector2.zero;
            if (LeftAction.IsPressed())
            {
                move.x -= 1.0f;
            }

            if (RightAction.IsPressed())
            {
                move.x += 1.0f;
            }

            if (UpAction.IsPressed())
            {
                move.y += 1.0f;
            }

            if (DownAction.IsPressed())
            {
                move.y -= 1.0f;
            }

            move.Normalize();
            move *= Time.fixedDeltaTime;

            if (FireAction.IsPressed())
            {
                Fire();
                move *= SlowSpeed;
            }
            else
            {
                move *= FastSpeed;
            }

            transform.position += (Vector3)move;
            transform.position = new Vector2(Mathf.Clamp(transform.position.x, -ScreenHalfWidth, ScreenHalfWidth),
                Mathf.Clamp(transform.position.y, -ScreenHalfHeight, ScreenHalfHeight));

            if (BombAction.WasPerformedThisFrame())
            {
                Bomb();
            }
        }

        private void OnEnable()
        {
            LeftAction.Enable();
            RightAction.Enable();
            UpAction.Enable();
            DownAction.Enable();
            FireAction.Enable();
            BombAction.Enable();
        }

        private void OnDisable()
        {
            LeftAction.Disable();
            RightAction.Disable();
            UpAction.Disable();
            DownAction.Disable();
            FireAction.Disable();
            BombAction.Disable();
        }
    }
}
