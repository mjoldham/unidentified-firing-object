using System;
using System.Linq;
using UnityEngine;

namespace UFO
{
    public class EnemyController : BaseSpawnable
    {
        protected GameManager _gm;
        protected PlayerController _player;
        protected Animator _animator;

        public static Action<Vector2> OnKill;

        private static int _damageID = Shader.PropertyToID("_Damage");

        public bool WaitToFire;
        public float LookSpeed = 0.0f;

        public float Health = 1.0f;
        private float _fullHealth;

        // How the enemy should move along the x-axis in normalised time (0 to 1).
        public AnimationCurve XCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        // How the enemy should move along the y-axis in normalised time (0 to 1).
        public AnimationCurve YCurve = AnimationCurve.EaseInOut(0.0f, 0.0f, 1.0f, 1.0f);

        public bool ShotSealingEnabled = true;

        public Transform HitboxParent, HurtboxParent, ShieldboxParent;

        [HideInInspector]
        public Collider2D[] Hitboxes, Hurtboxes, Shieldboxes;

        public int DamageFrames = 5;
        private int _damageFrames;

        private int _moveBeats;

        private ShotEmitter[] _emitters;

        Vector2 _start, _destination;
        double _destTime, _destDuration;

        private bool _isFiring, _isExiting;
        private bool _isOffscreen, _shotSealed;

        private Material[] _spriteMats;

        private void OnDrawGizmosSelected()
        {
            if (Parameters.Condition == SpawnCondition.None)
            {
                Gizmos.color = Color.green;
            }
            else if (Parameters.Condition == SpawnCondition.NoEnemies)
            {
                Gizmos.color = Color.red;
            }
            else
            {
                Gizmos.color = Color.black;
            }

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

        public void Init(GameManager gm, PlayerController player)
        {
            _gm = gm;
            _player = player;
            _animator = GetComponent<Animator>();

            SpriteRenderer[] sprites = GetComponentsInChildren<SpriteRenderer>();
            _spriteMats = new Material[sprites.Length];
            for (int i = 0; i < sprites.Length; i++)
            {
                _spriteMats[i] = sprites[i].material;
            }

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
                emitter.Init(gm);
            }

            _fullHealth = Health;
            gameObject.SetActive(false);
        }

        private void StartMove(Vector2 position, int beats)
        {
            _moveBeats = beats;
            if (_moveBeats == 0)
            {
                transform.position = _destination = position;
                StartMove();
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
            int beats = Parameters.CurrentStep.BeatsToComplete;
            Parameters.CurrentStep = Parameters.CurrentStep.NextStep;

            ReloadEmitters(pos.x > 0.0f);
            StartMove(pos, beats);
        }

        public override void Spawn(SpawnInfo spawnInfo)
        {
            _isFiring = _isExiting = _isOffscreen = _shotSealed = false;
            _moveBeats = 0;

            Parameters = spawnInfo.Parameters;
            Health = _fullHealth;
            transform.position = _destination = new Vector2(spawnInfo.Lane, GameManager.ScreenHalfHeight + 1.0f);

            ReloadEmitters(spawnInfo.Lane > 0.0f);
            gameObject.SetActive(true);
        }

        public bool TryDie(float damage)
        {
            if (damage == 0.0f)
            {
                return false;
            }

            GameManager.AddScore(damage);
            Health -= damage;
            if (Health <= 0.0f)
            {
                GameManager.AddScore(_fullHealth);
                if (GameManager.InSecondLoop)
                {
                    int i = 0;
                    for (; i < _emitters.Length; i++)
                    {
                        _gm.SpawnSuicideShot(_emitters[i].transform.position);
                    }

                    if (i == 0)
                    {
                        _gm.SpawnSuicideShot(transform.position);
                    }
                }

                OnKill?.Invoke(transform.position);
                gameObject.SetActive(false);
                return true;
            }

            _damageFrames = DamageFrames;
            foreach (Material mat in _spriteMats)
            {
                mat.SetFloat(_damageID, 1.0f);
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

        private void DamageTick()
        {
            if (--_damageFrames <= 0)
            {
                foreach (Material mat in _spriteMats)
                {
                    mat.SetFloat(_damageID, 0.0f);
                }

                return;
            }

            float t = (float)(DamageFrames - _damageFrames) / DamageFrames;
            foreach (Material mat in _spriteMats)
            {
                mat.SetFloat(_damageID, t);
            }
        }

        // Returns false when enemy should be despawned after exiting the stage.
        public bool Tick(float deltaTime)
        {
            DamageTick();

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
            _isOffscreen = transform.position.y < GameManager.CutoffHeight;
            // - offscreen.
            _isOffscreen |= transform.position.y > GameManager.ScreenHalfHeight;
            _isOffscreen |= Mathf.Abs(transform.position.x) > GameManager.ScreenHalfWidth;
            // - within sealing distance.
            _shotSealed = ShotSealingEnabled && ((Vector2)(_player.transform.position - transform.position)).sqrMagnitude < 1.0f;

            _isFiring = ShotEmitter.Tick(_emitters, _isOffscreen || _shotSealed);
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

            // Don't start next step if still moving or firing when onscreen.
            if (--_moveBeats > 0 || (_isFiring && !_isOffscreen))
            {
                return;
            }

            StartMove();
        }

        private void OnUnpause(double lostTime)
        {
            _destTime += lostTime;
        }

        private void OnEnable()
        {
            GameManager.OnBeat += OnBeat;
            GameManager.OnUnpause += OnUnpause;
        }

        private void OnDisable()
        {
            GameManager.OnBeat -= OnBeat;
            GameManager.OnUnpause -= OnUnpause;
        }
    }
}
