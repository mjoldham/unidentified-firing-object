using System;
using System.Collections.Generic;
using System.Linq;
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

        [Range(0, 5)]
        public int BombCount;

        [Range(0, 4)]
        public int PowerCount;

        private List<ShotEmitter>[] _emitters;

        private int _shotFrame;
        private float _shotTimeFrames;

        private bool _isFiring;

        private void Start()
        {
            Transform child = transform.GetChild(0);
            _emitters = new List<ShotEmitter>[child.childCount];
            for (int i = 0; i < child.childCount; i++)
            {
                _emitters[i] = child.GetChild(i).GetComponentsInChildren<ShotEmitter>().ToList();
            }
        }

        private void HandleFiring()
        {
            if (Settings.FireAction.IsPressed())
            {
                _shotTimeFrames = Settings.ShotTimeBuffer;
            }

            // TODO: could restart emitters early if all shots are despawned?

            _shotTimeFrames--;
            if (_isFiring)
            {
                _isFiring = false;
                foreach (ShotEmitter emitter in _emitters[PowerCount])
                {
                    _isFiring |= emitter.Tick();
                }

                if (_isFiring)
                {
                    return;
                }
            }

            if (_shotTimeFrames < 0)
            {
                return;
            }

            _isFiring = true;
            foreach (ShotEmitter emitter in _emitters[PowerCount])
            {
                emitter.Restart();
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

        private void HandleMovement()
        {
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
            move *= Time.fixedDeltaTime * (_isFiring ? Settings.SlowSpeed : Settings.FastSpeed);

            Vector3 oldPos = transform.position;
            transform.position += (Vector3)move;
            transform.position = new Vector2(Mathf.Clamp(transform.position.x, -GameManager.ScreenHalfWidth, GameManager.ScreenHalfWidth),
                Mathf.Clamp(transform.position.y, -GameManager.ScreenHalfHeight, GameManager.ScreenHalfHeight));

            OnMove?.Invoke(transform.position - oldPos, _isFiring);
        }

        private void HandleBombing()
        {
            if (!Settings.BombAction.WasPerformedThisFrame())
            {
                return;
            }
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

            HandleMovement();
            HandleFiring();
            HandleBombing();
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
