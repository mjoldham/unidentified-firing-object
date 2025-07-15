using UnityEngine;

namespace UFO
{
    public class EnemyShot : ShotBase
    {
        public void ResetShot(Vector3 position, float angle, float speed)
        {
            transform.position = position;
            Angle = angle;
            Speed = speed;
            gameObject.SetActive(true);
        }

        public void Tick(float deltaTime)
        {
            Vector3 move = Quaternion.Euler(0, 0, Angle) * Vector3.down;
            transform.position += move * Speed * deltaTime;
        }
    }
}
