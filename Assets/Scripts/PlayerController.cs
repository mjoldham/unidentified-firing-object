using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UFO
{
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        public PlayerSettings Settings;

        public static Action OnSpawn, OnDeath, OnFire, OnBombUse;
        public static Action<Vector2, bool> OnMove;

        public bool IsShielded;

        [Min(0)]
        public int ExtendCount = 2;

        [Range(0, 5)]
        public int BombCount = 3;

        [Range(0, 4)]
        public int PowerCount;

        private List<ShotEmitter>[] _emitters;

        private int _shotFrame;
        private float _shotTimeFrames;

        private bool _isFiring;

        private Coroutine _dyingCoroutine;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(this);
                return;
            }

            Instance = this;
        }

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

            OnSpawn?.Invoke();
        }

        private IEnumerator Dying()
        {
            yield return new WaitForSeconds(Settings.BombSaveDuration);
            OnDeath?.Invoke();
        }

        public void Die()
        {
            StartCoroutine(Dying());
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
            if (BombCount == 0)
            {
                return;
            }

            if (!Settings.BombAction.WasPerformedThisFrame())
            {
                return;
            }

            if (_dyingCoroutine != null)
            {
                StopCoroutine(_dyingCoroutine);
            }

            BombCount--;
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
