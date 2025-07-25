using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UFO
{
    public class EnemyController : MonoBehaviour
    {
        protected PlayerController _player;
        protected Animator _animator;

        public static Action<Vector2> OnKill;

        public float ShotSealingDistance = 1.0f; // Set this to zero for relentless enemies!
        private float _sealingDistSqr;

        public Transform HitboxParent, HurtboxParent, ShieldboxParent;

        [HideInInspector]
        public Collider2D[] Hitboxes, Hurtboxes, Shieldboxes;

        // How the enemy should move along the x-axis in normalised time (0 to 1).
        public AnimationCurve XCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        // How the enemy should move along the y-axis in normalised time (0 to 1).
        public AnimationCurve YCurve = AnimationCurve.Constant(0.0f, 1.0f, 0.0f);

        public int Health = 1;
        private int _fullHealth;

        private Queue<RouteStep> _route;
        private int _moveBeats, _fireCount;
        private bool _waitToFire;

        private bool _isMirrored;

        private ShotEmitter[] _emitters;

        Vector2 _start, _destination;
        double _destTime, _destDuration;

        private bool _isExiting;

        public void Init()
        {
            _player = PlayerController.Instance;
            _animator = GetComponent<Animator>();

            if (HitboxParent != null)
            {
                Hitboxes = HitboxParent.GetComponentsInChildren<Collider2D>();
            }
            else
            {
                Hitboxes = new Collider2D[0];
            }

            if (HurtboxParent != null)
            {
                Hurtboxes = HurtboxParent.GetComponentsInChildren<Collider2D>();
            }
            else
            {
                Hurtboxes = new Collider2D[0];
            }

            if (ShieldboxParent != null)
            {
                Shieldboxes = ShieldboxParent.GetComponentsInChildren<Collider2D>();
            }
            else
            {
                Shieldboxes = new Collider2D[0];
            }

            foreach (Collider2D hurt in Hurtboxes)
            {
                hurt.gameObject.layer = GameManager.HurtLayer;
            }

            foreach (Collider2D shield in Shieldboxes)
            {
                shield.gameObject.layer = GameManager.ShieldLayer;
            }

            _emitters = GetComponentsInChildren<ShotEmitter>();
            foreach (ShotEmitter emitter in _emitters)
            {
                emitter.Init();
            }

            _fullHealth = Health;
            _sealingDistSqr = ShotSealingDistance * ShotSealingDistance;
            gameObject.SetActive(false);
        }

        private void StartMove(Vector2 position, int beats, bool isMirrored)
        {
            if (isMirrored)
            {
                position.x = -position.x;
            }

            _moveBeats = beats;
            if (_moveBeats == 0)
            {
                transform.position = _destination = position;
                return;
            }

            _start = transform.position;
            _destination = position;

            _destDuration = beats * GameManager.BeatLength;
            _destTime = UnityEngine.AudioSettings.dspTime + _destDuration;
        }

        private void StartMove(RouteStep step)
        {
            _fireCount = step.NumBursts;
            _waitToFire = step.WaitToFire;

            if (_fireCount > 0)
            {
                foreach (ShotEmitter emitter in _emitters)
                {
                    emitter.Restart();
                    emitter.IsMirrored = _isMirrored ? !emitter.IsMirrored : emitter.IsMirrored;
                }
            }

            StartMove(step.transform.position, step.BeatsToComplete, _isMirrored);
        }

        public void Spawn(SpawnInfo spawnInfo)
        {
            _isExiting = false;
            _moveBeats = 0;
            _isMirrored = spawnInfo.IsMirrored;
            Health = _fullHealth;

            gameObject.SetActive(true);

            if (spawnInfo.RoutePrefab == null)
            {
                transform.position = _destination = new Vector2(0.0f, GameManager.ScreenHalfHeight + 1.0f);
                ExitStage();
                return;
            }

            _route = new Queue<RouteStep>(spawnInfo.RoutePrefab.GetComponentsInChildren<RouteStep>());
            if (!_route.TryDequeue(out RouteStep step))
            {
                transform.position = _destination = new Vector2(0.0f, GameManager.ScreenHalfHeight + 1.0f);
                ExitStage();
                return;
            }

            int lane = Mathf.RoundToInt(step.transform.position.x);
            lane = Mathf.Clamp(_isMirrored ? -lane : lane, -(GameManager.NumLanes - 1) / 2, (GameManager.NumLanes - 1) / 2);
            transform.position = _destination = new Vector2(lane, GameManager.ScreenHalfHeight + 1.0f);
            StartMove(step);
        }

        public bool TryDie(int damage)
        {
            if (damage == 0)
            {
                return false;
            }

            Health -= damage;
            if (Health <= 0)
            {
                OnKill?.Invoke(transform.position);
                gameObject.SetActive(false);
                return true;
            }

            return false;
        }

        public void TryDamage(PlayerController player)
        {
            if (player.IsInvincible)
            {
                return;
            }

            Vector2 position = player.transform.position;
            if (Hitboxes.Any(hitbox => hitbox.enabled && hitbox.OverlapPoint(position)))
            {
                player.TryDie();
                GameManager.OnHitHurt?.Invoke(position);
            }
        }

        // Returns false when enemy should be despawned after exiting the stage.
        public bool Tick(float deltaTime)
        {
            // Updates position based on destination and curves.
            double time = UnityEngine.AudioSettings.dspTime;
            if (time < _destTime)
            {
                float t = (float)((_destTime - time) / _destDuration);
                float tx = XCurve.Evaluate(1.0f - t);
                float ty = YCurve.Evaluate(1.0f - t);

                float x = Mathf.Lerp(_start.x, _destination.x, tx);
                float y = Mathf.Lerp(_start.y, _destination.y, ty);
                transform.position = new Vector2(x, y);

                if (_waitToFire)
                {
                    return true;
                }
            }
            else
            {
                transform.position = _destination;
                if (_isExiting)
                {
                    gameObject.SetActive(false);
                    return false;
                }
            }

            if (_fireCount == 0)
            {
                return true;
            }

            // While above the cutoff height or offscreen, keeps ticking emitters.
            if (transform.position.y < GameManager.CutoffHeight || transform.position.y > GameManager.ScreenHalfHeight
                || Mathf.Abs(transform.position.x) > GameManager.ScreenHalfWidth)
            {
                return true;
            }

            if ((_player.transform.position - transform.position).sqrMagnitude < _sealingDistSqr)
            {
                return true;
            }

            if (ShotEmitter.Tick(_emitters))
            {
                return true;
            }

            if (--_fireCount == 0)
            {
                return true;
            }

            foreach (ShotEmitter emitter in _emitters)
            {
                emitter.Restart();
            }

            return true;
        }

        private void ExitStage()
        {
            _isExiting = true;
            Vector2 dest = new Vector2(transform.position.x, -GameManager.ScreenHalfHeight - 1.0f);
            StartMove(dest, 4, false);
        }

        private void OnBeat()
        {
            if (_isExiting)
            {
                return;
            }

            // Don't start next step if still firing or moving.
            if (_fireCount > 0 || --_moveBeats > 0)
            {
                return;
            }

            // If finished moving, exit the stage.
            if (!_route.TryDequeue(out RouteStep step))
            {
                ExitStage();
                return;
            }

            StartMove(step);
        }

        private void OnPause()
        {
            _animator.speed = 0.0f;
        }

        private void OnUnpause(double lostTime)
        {
            _animator.speed = 1.0f;
            _destTime += lostTime;
        }

        private void OnEnable()
        {
            GameManager.OnBeat += OnBeat;
            GameManager.OnPause += OnPause;
            GameManager.OnUnpause += OnUnpause;
        }

        private void OnDisable()
        {
            GameManager.OnBeat -= OnBeat;
            GameManager.OnPause -= OnPause;
            GameManager.OnUnpause -= OnUnpause;
        }
    }
}
