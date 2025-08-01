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
        public float Damage = 1.0f;

        [HideInInspector]
        public int Angle = 0;
        [HideInInspector]
        public Collider2D Hitbox;

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
            Hitbox = GetComponentInChildren<Collider2D>();

            gameObject.SetActive(false);
        }

        public void Spawn(ShotParams shotParams, float damage, Vector2 position, int angle)
        {
            _overrideController[_overrideClip] = shotParams.Clip;
            Target = shotParams.Target;
            Speed = shotParams.Speed;
            Damage = damage;

            if (Target != TargetType.Player)
            {
                gameObject.layer = GameManager.HitLayer;
            }

            Quaternion rot = Quaternion.Euler(0.0f, 0.0f, Angle = angle);
            transform.SetPositionAndRotation(position, rot);
            _velocity = rot * (Speed * Vector3.down);

            gameObject.SetActive(true);
        }

        public bool TryDamage(EnemyController enemy, ref float damage)
        {
            Collider2D[] results = new Collider2D[16];
            ContactFilter2D filter = new ContactFilter2D();

            // TODO: rework this so we're not doing all this redundant work. guess we have to find each enemy as we kill it and dequeue?
            filter.SetLayerMask(GameManager.ShieldMask);
            int count = Physics2D.OverlapCollider(Hitbox, filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i].GetComponentInParent<EnemyController>() != enemy)
                {
                    continue;
                }

                gameObject.SetActive(false);
                GameManager.OnHitShield?.Invoke(transform.position);
                return true;
            }

            filter.SetLayerMask(GameManager.HurtMask);
            count = Physics2D.OverlapCollider(Hitbox, filter, results);
            for (int i = 0; i < count; i++)
            {
                if (results[i].GetComponentInParent<EnemyController>() != enemy)
                {
                    continue;
                }

                damage += Damage;
                gameObject.SetActive(false);

                GameManager.OnHitHurt?.Invoke(transform.position);
                return true;
            }

            return false;
        }

        public bool TryDamage(PlayerController player)
        {
            if (Hitbox.OverlapPoint(player.transform.position))
            {
                gameObject.SetActive(false);
                return true;
            }

            return false;
        }

        public bool Tick(float deltaTime)
        {
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
