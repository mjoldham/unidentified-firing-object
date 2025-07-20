using UnityEngine;

namespace UFO
{
    public class DummyEnemy : EnemyBase
    {
        public override void Tick(Vector3 playerPosition, float deltaTime)
        {
            transform.position += Speed * Time.fixedDeltaTime * Vector3.down;
            if (transform.position.y < -GameManager.ScreenHalfHeight - 1.0f)
            {
                Destroy(gameObject);
                return;
            }

            int frames = 1;
            ShotEmitter.Tick(_emitters, ref _isFiring, ref frames);
        }
    }
}
