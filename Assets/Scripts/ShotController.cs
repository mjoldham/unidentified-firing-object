using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UFO
{
    public class ShotController : MonoBehaviour
    {
        public enum TargetType
        {
            Player,
            Enemy,
            Both
        }

        [HideInInspector]
        public TargetType Target;
        [HideInInspector]
        public float Speed = 5.0f;
        [HideInInspector]
        public int Angle = 0;

        private Animator _animator;
        private AnimatorOverrideController _overrideController;
        private AnimationClip _overrideClip;

        private Vector3 _velocity;

        public void Init()
        {
            _animator = GetComponent<Animator>();
            _overrideController = new AnimatorOverrideController(_animator.runtimeAnimatorController);
            _animator.runtimeAnimatorController = _overrideController;
            _overrideClip = _overrideController.animationClips[0];

            gameObject.SetActive(false);
        }

        public void Spawn(ShotParams shotParams, Vector2 position, int angle)
        {
            _overrideController[_overrideClip] = shotParams.Clip;
            Target = shotParams.Target;
            Speed = shotParams.Speed;

            Quaternion rot = Quaternion.Euler(0.0f, 0.0f, Angle = angle);
            transform.SetPositionAndRotation(position, rot);
            _velocity = rot * (Speed * Vector3.down);

            gameObject.SetActive(true);
        }

        public bool Tick(float deltaTime)
        {
            // TODO: test for collisions depending on target type.
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
