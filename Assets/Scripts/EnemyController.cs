using System;
using System.Linq;
using UnityEngine;

namespace UFO
{
    public class EnemyController : MonoBehaviour
    {
        protected PlayerController _player;
        protected Animator _animator;

        public static Action<Vector2> OnKill;

        public EnemyParams Parameters;

        public bool WaitToFire;
        public float LookSpeed = 0.0f;

        public int Health = 1;
        private int _fullHealth;

        // How the enemy should move along the x-axis in normalised time (0 to 1).
        public AnimationCurve XCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        // How the enemy should move along the y-axis in normalised time (0 to 1).
        public AnimationCurve YCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        public float ShotSealingDistance = 1.0f; // Set this to zero for relentless enemies!
        private float _sealingDistSqr;

        public Transform HitboxParent, HurtboxParent, ShieldboxParent;

        [HideInInspector]
        public Collider2D[] Hitboxes, Hurtboxes, Shieldboxes;

        private int _moveBeats;

        private ShotEmitter[] _emitters;

        Vector2 _start, _destination;
        double _destTime, _destDuration;

        private bool _isFiring, _isExiting;

        private void OnDrawGizmos()
        {
            Gizmos.color = Parameters.CheckForEnemies ? Color.red : Color.green;
            if (Parameters.ExemptFromCheck)
            {
                Gizmos.color += Color.blue;
            }

            Gizmos.DrawWireSphere(transform.position, 1.0f);

            Gizmos.color = Color.red;
            Vector2 start = new Vector2(transform.position.x, GameManager.ScreenHalfHeight + 1.0f);
            Vector2 end = new Vector2(transform.position.x, -GameManager.ScreenHalfHeight - 1.0f);
            if (Parameters.CurrentStep != null)
            {
                Gizmos.color = Color.green;
                end = new Vector2(Parameters.CurrentStep.transform.position.x, Parameters.CurrentStep.transform.localPosition.y);
            }

            Gizmos.DrawLine(start, end);
        }

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

        private void StartMove(Vector2 position, int beats)
        {
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

        private void ReloadEmitters(bool isMirrored)
        {
            foreach (ShotEmitter emitter in _emitters)
            {
                emitter.Restart();
                emitter.IsMirrored = isMirrored;
            }
        }

        private void StartMove()
        {
            // If finished moving, exit the stage.
            if (Parameters.CurrentStep == null)
            {
                ExitStage();
                return;
            }

            _isFiring = true;

            Vector2 pos = new Vector2(Parameters.CurrentStep.transform.position.x, Parameters.CurrentStep.transform.localPosition.y);
            ReloadEmitters(pos.x > 0.0f);

            StartMove(pos, Parameters.CurrentStep.BeatsToComplete);
            Parameters.CurrentStep = Parameters.CurrentStep.NextStep;
        }

        public void Spawn(SpawnInfo spawnInfo)
        {
            _isFiring = _isExiting = false;
            _moveBeats = 0;

            Parameters = spawnInfo.Parameters;
            Health = _fullHealth;
            transform.position = _destination = new Vector2(spawnInfo.Lane, GameManager.ScreenHalfHeight + 1.0f);

            ReloadEmitters(spawnInfo.Lane > 0.0f);
            gameObject.SetActive(true);
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
            }
        }

        // Returns false when enemy should be despawned after exiting the stage.
        public bool Tick(float deltaTime)
        {
            if (LookSpeed > 0.0f)
            {
                float angle = 0.0f;
                if (!_isExiting)
                {
                    angle = GameManager.AngleToTarget(transform.position, _player.transform.position);
                }

                angle = Mathf.LerpAngle(transform.rotation.eulerAngles.z, angle, LookSpeed * deltaTime);
                transform.rotation = Quaternion.Euler(0.0f, 0.0f, angle);
            }

            // Updates position based on destination and curves.
            double time = UnityEngine.AudioSettings.dspTime;
            if (time < _destTime)
            {
                float t = (float)((_destTime - time) / _destDuration);
                float tx = XCurve.Evaluate(1.0f - t);
                float ty = YCurve.Evaluate(1.0f - t);

                float x = Mathf.LerpUnclamped(_start.x, _destination.x, tx);
                float y = Mathf.LerpUnclamped(_start.y, _destination.y, ty);
                transform.position = new Vector2(x, y);

                if (WaitToFire)
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

            // Keeps emitters ticking but suppresses fire if:
            // - below the cutoff height.
            bool overrideFire = transform.position.y < GameManager.CutoffHeight;
            // - offscreen.
            overrideFire |= transform.position.y > GameManager.ScreenHalfHeight;
            overrideFire |= Mathf.Abs(transform.position.x) > GameManager.ScreenHalfWidth;
            // - within sealing distance.
            overrideFire |= (_player.transform.position - transform.position).sqrMagnitude < _sealingDistSqr;

            _isFiring = ShotEmitter.Tick(_emitters, overrideFire);
            return true;
        }

        private void ExitStage()
        {
            _isExiting = true;
            Vector2 dest = new Vector2(transform.position.x, -GameManager.ScreenHalfHeight - 1.0f);
            StartMove(dest, 4);
        }

        private void OnBeat()
        {
            if (_isExiting)
            {
                return;
            }

            // Don't start next step if still firing or moving.
            if (_isFiring || --_moveBeats > 0)
            {
                return;
            }

            StartMove();
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
