using System;
using UnityEngine;

namespace UFO
{
    public abstract class ShotBase : MonoBehaviour
    {
        public float Speed = 5.0f;

        [HideInInspector]
        public int Angle = 0;

        public void ResetShot(Vector3 position, int angle)
        {
            transform.SetPositionAndRotation(position, Quaternion.AngleAxis(angle, Vector3.forward));
            Angle = angle;
            gameObject.SetActive(true);
        }

        public virtual void Tick(float deltaTime)
        {
            Vector3 move = Quaternion.Euler(0.0f, 0.0f, Angle) * Vector3.down;
            transform.position += Speed * deltaTime * move;
            // TODO: put out-of-bounds/collision logic for each shot type here.
        }
    }
}
