using UnityEngine;

namespace UFO
{
    public class KamikazeEnemy : EnemyBase
    {
        public float TurnSpeed = 180f;

        private Vector3 _direction = Vector3.down;

        public override void Tick(Vector3 playerPosition, float deltaTime)
        {
            Vector3 toPlayer = (playerPosition - transform.position).normalized;
            float angle = Vector3.SignedAngle(_direction, toPlayer, Vector3.forward);

            float maxTurn = TurnSpeed * deltaTime;
            angle = Mathf.Clamp(angle, -maxTurn, maxTurn);

            _direction = Quaternion.Euler(0, 0, angle) * _direction;
            transform.position += _direction.normalized * Speed * deltaTime;
            transform.up = _direction;
        }
    }
}
