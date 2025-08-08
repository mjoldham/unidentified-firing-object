using UnityEngine;
using UnityEngine.UIElements;

namespace UFO
{
    public class PowerupController : BaseSpawnable
    {
        private PlayerController _player;

        public CircleCollider2D ShieldCollider, ExtendCollider, PowerCollider, BombCollider;
        private CircleCollider2D _shieldExtend;

        public float FallDuration = 10.0f, TripleDuration = 2.5f;
        public float TurnSpeedStart = 180.0f, TurnSpeedEnd = 720.0f;

        // Normalised position of the powerup over normalised time
        public AnimationCurve FallCurve = AnimationCurve.Linear(0.0f, 0.0f, 1.0f, 1.0f);

        public static bool SpawnExtend;
        private bool _spawnedExtend;

        private float _fallTimer, _tripleTimer, _outerRadiusSqr;

        public void Init(PlayerController player)
        {
            _player = player;

            _outerRadiusSqr = (ShieldCollider.ClosestPoint(transform.position) - (Vector2)transform.position).magnitude;
            _outerRadiusSqr += 2.0f * ShieldCollider.radius;
            _outerRadiusSqr *= _outerRadiusSqr;

            ExtendCollider.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        public override void Spawn(SpawnInfo spawnInfo)
        {
            transform.position = new Vector2(spawnInfo.Lane, GameManager.ScreenHalfHeight + 1.0f);
            if (SpawnExtend && _player.IsShielded)
            {
                SpawnExtend = false;
                _spawnedExtend = true;
                ShieldCollider.gameObject.SetActive(false);
                ExtendCollider.gameObject.SetActive(true);
                _shieldExtend = ExtendCollider;
            }
            else
            {
                _spawnedExtend = false;
                ShieldCollider.gameObject.SetActive(true);
                ExtendCollider.gameObject.SetActive(false);
                _shieldExtend = ShieldCollider;
            }

            _fallTimer = FallDuration;
            _tripleTimer = TripleDuration;

            gameObject.SetActive(true);
        }

        // Returns false when despawned.
        public bool Tick(PlayerController player, float deltaTime)
        {
            _fallTimer -= deltaTime;
            if (_fallTimer <= 0.0f)
            {
                gameObject.SetActive(false);
                return false;
            }

            // Updates position.
            float y = Mathf.Lerp(-GameManager.ScreenHalfHeight - 1.0f, GameManager.ScreenHalfHeight + 1.0f, FallCurve.Evaluate(_fallTimer / FallDuration));
            transform.position = new Vector2(transform.position.x, y);

            // Updates rotation.
            float delta = Mathf.Lerp(TurnSpeedEnd, TurnSpeedStart, _tripleTimer / TripleDuration) * deltaTime;
            transform.rotation *= Quaternion.Euler(0.0f, 0.0f, delta);
            _shieldExtend.transform.rotation = PowerCollider.transform.rotation = BombCollider.transform.rotation = Quaternion.identity;

            // If player is inside then check triple timer.
            Vector2 playerPos = player.transform.position;
            if ((playerPos - (Vector2)transform.position).sqrMagnitude < _outerRadiusSqr)
            {
                // Decrements the triple timer. When it ends the player gets all three powerups.
                _tripleTimer -= deltaTime;
                if (_tripleTimer <= 0.0f)
                {
                    if (_spawnedExtend)
                    {
                        player.GetExtend(_shieldExtend.transform.position);
                    }
                    else
                    {
                        player.GetShield(_shieldExtend.transform.position);
                    }

                    player.GetPower(PowerCollider.transform.position);
                    player.GetBomb(BombCollider.transform.position);

                    gameObject.SetActive(false);
                    return false;
                }
            }
            // Otherwise resets triple timer.
            else
            {
                _tripleTimer = TripleDuration;
            }

            // Checks collision with individual powerups.
            bool despawn = false;
            if (_shieldExtend.OverlapPoint(playerPos))
            {
                despawn = true;
                if (_spawnedExtend)
                {
                    player.GetExtend(_shieldExtend.transform.position);
                }
                else
                {
                    player.GetShield(_shieldExtend.transform.position);
                }
            }
            else if (PowerCollider.OverlapPoint(playerPos))
            {
                despawn = true;
                player.GetPower(PowerCollider.transform.position);
            }
            else if (BombCollider.OverlapPoint(playerPos))
            {
                despawn = true;
                player.GetBomb(BombCollider.transform.position);
            }

            if (despawn)
            {
                gameObject.SetActive(false);
                return false;
            }

            return true;
        }
    }
}
