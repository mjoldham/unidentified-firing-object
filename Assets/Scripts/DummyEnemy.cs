using UnityEngine;

namespace UFO
{
    public class DummyEnemy : EnemyBase
    {
        public override void Tick(Vector3 playerPosition, float deltaTime)
        {

        }

        private void FixedUpdate()
        {
            transform.position -= 2.0f * Time.fixedDeltaTime * Vector3.up;
            if (transform.position.y < -GameManager.ScreenHalfHeight - 1.0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
