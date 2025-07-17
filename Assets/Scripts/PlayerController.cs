using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UFO
{
    public class PlayerController : MonoBehaviour
    {
        public PlayerSettings Settings;

        public static Action OnSpawn, OnDeath, OnFire, OnBombUse;
        public static Action<Vector2, bool> OnMove;

        public bool IsShielded;
        public int BombCount;
        public int PowerCount;

        private Queue<PlayerShot> _inactiveShots = new Queue<PlayerShot>();
        private Queue<PlayerShot> _activeShots = new Queue<PlayerShot>();
        private int _shotFrame;

        private void Start()
        {
            for (int i = 0; i < Settings.ShotLimit; i++)
            {
                PlayerShot shot = Instantiate(Settings.ShotPrefab);
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

                Vector3 move = Quaternion.Euler(0, 0, shot.Angle) * Vector3.down;
                shot.transform.position += shot.Speed * Time.fixedDeltaTime * move;

                if (shot.transform.position.y > GameManager.ScreenHalfHeight + 1.0f)
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
            // Makes it so the player has hyper-firerate when up close.
            if (_activeShots.Count > 0)
            {
                if (_shotFrame++ < Settings.ShotFrameDelay || _activeShots.Count >= Settings.ShotLimit)
                {
                    return; // TODO: change shot delay based on active shot count.
                }
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
            // TODO: clear all projectiles.
            // TODO: damage enemies based on distance.

            OnBombUse?.Invoke();
        }

        private void FixedUpdate()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Application.Quit();
            }

            UpdateShots();

            Vector2 move = Vector2.zero;
            if (Settings.LeftAction.IsPressed())
            {
                move.x -= 1.0f;
            }

            if (Settings.RightAction.IsPressed())
            {
                move.x += 1.0f;
            }

            if (Settings.UpAction.IsPressed())
            {
                move.y += 1.0f;
            }

            if (Settings.DownAction.IsPressed())
            {
                move.y -= 1.0f;
            }

            move.Normalize();
            move *= Time.fixedDeltaTime;

            bool isFiring = Settings.FireAction.IsPressed();
            if (isFiring)
            {
                Fire();
                move *= Settings.SlowSpeed;
            }
            else
            {
                move *= Settings.FastSpeed;
            }

            Vector3 oldPos = transform.position;
            transform.position += (Vector3)move;
            transform.position = new Vector2(Mathf.Clamp(transform.position.x, -GameManager.ScreenHalfWidth, GameManager.ScreenHalfWidth),
                Mathf.Clamp(transform.position.y, -GameManager.ScreenHalfHeight, GameManager.ScreenHalfHeight));

            OnMove?.Invoke(transform.position - oldPos, isFiring);

            if (Settings.BombAction.WasPerformedThisFrame())
            {
                Bomb();
            }
        }

        private void OnEnable()
        {
            Settings.LeftAction.Enable();
            Settings.RightAction.Enable();
            Settings.UpAction.Enable();
            Settings.DownAction.Enable();
            Settings.FireAction.Enable();
            Settings.BombAction.Enable();
        }

        private void OnDisable()
        {
            Settings.LeftAction.Disable();
            Settings.RightAction.Disable();
            Settings.UpAction.Disable();
            Settings.DownAction.Disable();
            Settings.FireAction.Disable();
            Settings.BombAction.Disable();
        }
    }
}
