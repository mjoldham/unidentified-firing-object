using System;
using UnityEngine;

namespace UFO
{
    public abstract class ShotBase : MonoBehaviour
    {
        public enum TargetType
        {
            Player,
            Enemy,
            Both
        }

        public TargetType Target;
        public float Speed = 5.0f;

        [HideInInspector]
        public int Angle = 0;

        private Vector3 _velocity;

        public void Spawn(Vector3 position, int angle)
        {
            Quaternion rot = Quaternion.Euler(0.0f, 0.0f, Angle = angle);
            transform.SetPositionAndRotation(position, rot);
            _velocity = rot * (Speed * Vector3.down);

            gameObject.SetActive(true);
        }

        public virtual bool Tick(float deltaTime)
        {
            // TODO: Test for collisions.
            transform.position += deltaTime * _velocity;
            if (Mathf.Abs(transform.position.x) > GameManager.ScreenHalfWidth + 1.0f ||
                Mathf.Abs(transform.position.y) > GameManager.ScreenHalfHeight + 1.0f)
            {
                gameObject.SetActive(false);
                return false;
            }

            return true;
        }
    }
}
