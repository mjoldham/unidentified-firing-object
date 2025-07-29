using UnityEngine;
using static TMPro.SpriteAssetUtilities.TexturePacker_JsonArray;

namespace UFO
{
    public class PlayerAnimator : AnimatorBase
    {
        public Animator BodyAnimator;
        public Transform ThrusterLeft, ThrusterRight;
        public MeshRenderer[] MuzzleFlashes;

        public MeshRenderer DeathFlash;
        public int DeathFrames = 25;
        private int _deathFrames;

        public enum AnimState
        {
            PlayerNeutral = 0,
            PlayerBankLeft,
            PlayerBankRight
        }

        private static int[] _stateIDs = new int[] { Animator.StringToHash(nameof(AnimState.PlayerNeutral)),
                                                     Animator.StringToHash(nameof(AnimState.PlayerBankLeft)),
                                                     Animator.StringToHash(nameof(AnimState.PlayerBankRight)) };

        private Vector3 _thrusterLeftPos, _thrusterRightPos;
        private const float _pixelSize = 1.0f / 64.0f;

        private float _firingScale;

        private void Start()
        {
            _thrusterLeftPos = (Vector2)ThrusterLeft.localPosition;
            _thrusterRightPos = (Vector2)ThrusterRight.localPosition;

            for (int i = 0; i < MuzzleFlashes.Length; i++)
            {
                MuzzleFlashes[i].gameObject.SetActive(false);
            }

            DeathFlash.transform.SetParent(null);
            DeathFlash.gameObject.SetActive(false);
        }

        public static void SwitchToState(Animator animator, AnimState state)
        {
            animator.CrossFade(_stateIDs[(int)state], 0.0f);
        }

        private void OnMove(Vector2 move, bool isFiring)
        {
            if (move.x != 0.0f)
            {
                Quaternion rot = Quaternion.Euler(0.0f, 0.0f, 0.2f * Vector2.SignedAngle(Vector2.up, move));
                ThrusterLeft.rotation = ThrusterRight.rotation = rot;

                if (move.x < 0.0f)
                {
                    GoToState(BodyAnimator, _stateIDs[(int)AnimState.PlayerBankLeft]);
                    ThrusterLeft.localPosition = _thrusterLeftPos + new Vector3(-2 * _pixelSize, 0.0f, 0.0f);
                    ThrusterRight.localPosition = _thrusterRightPos + new Vector3(-2 * _pixelSize, 0.0f, 0.0f);

                    ThrusterLeft.rotation *= Quaternion.Euler(0.0f, 0.0f, -30.0f);
                }
                else
                {
                    GoToState(BodyAnimator, _stateIDs[(int)AnimState.PlayerBankRight]);
                    ThrusterLeft.localPosition = _thrusterLeftPos + new Vector3(2 * _pixelSize, 0.0f, 0.0f);
                    ThrusterRight.localPosition = _thrusterRightPos + new Vector3(2 * _pixelSize, 0.0f, 0.0f);

                    ThrusterRight.rotation *= Quaternion.Euler(0.0f, 0.0f, 30.0f);
                }

                return;
            }

            if (move.y < 0.0f)
            {
                ThrusterLeft.rotation = Quaternion.Euler(0.0f, 0.0f, 10.0f);
                ThrusterRight.rotation = Quaternion.Euler(0.0f, 0.0f, -10.0f);
            }
            else if (move.y > 0.0f)
            {
                ThrusterLeft.rotation = Quaternion.Euler(0.0f, 0.0f, -10.0f);
                ThrusterRight.rotation = Quaternion.Euler(0.0f, 0.0f, 10.0f);
            }
            else
            {
                ThrusterLeft.rotation = ThrusterRight.rotation = Quaternion.identity;
            }

            GoToState(BodyAnimator, _stateIDs[(int)AnimState.PlayerNeutral]);
            ThrusterLeft.localPosition = _thrusterLeftPos;
            ThrusterRight.localPosition = _thrusterRightPos;
        }

        private void OnFireStart()
        {
            _firingScale = 3.0f;
            foreach (MeshRenderer flash in MuzzleFlashes)
            {
                flash.material.SetFloat(GameManager.NormTimeID, 0.0f);
                flash.gameObject.SetActive(true);
            }
        }

        private void OnFireEnd()
        {
            _firingScale = 0.0f;
            foreach (MeshRenderer flash in MuzzleFlashes)
            {
                flash.gameObject.SetActive(false);
            }
        }

        private void OnDeath()
        {
            _deathFrames = DeathFrames;
            DeathFlash.material.SetFloat(GameManager.NormTimeID, 0.0f);
            DeathFlash.transform.position = transform.position;
            DeathFlash.gameObject.SetActive(true);
        }

        private void Tick()
        {
            float fireFreq = 10.0f;
            Vector3 offset = _firingScale * _pixelSize * Mathf.Sin(2.0f * Mathf.PI * fireFreq * Time.time) * Vector3.up;
            ThrusterLeft.localPosition = _thrusterLeftPos + offset;
            ThrusterRight.localPosition = _thrusterRightPos + offset;

            if (_firingScale > 0.0f)
            {
                for (int i = 0; i < MuzzleFlashes.Length; i++)
                {
                    MuzzleFlashes[i].material.SetFloat(GameManager.NormTimeID, 3.0f * fireFreq * Time.time);
                }
            }

            if (_deathFrames == 0)
            {
                return;
            }

            if (--_deathFrames > 0)
            {
                DeathFlash.material.SetFloat(GameManager.NormTimeID, (float)(DeathFrames - _deathFrames) / DeathFrames);
            }
            else
            {
                DeathFlash.gameObject.SetActive(false);
            }
        }

        private void OnEnable()
        {
            PlayerController.OnTick += Tick;
            PlayerController.OnMove += OnMove;
            PlayerController.OnFireStart += OnFireStart;
            PlayerController.OnFireEnd += OnFireEnd;
            PlayerController.OnDeath += OnDeath;
        }

        private void OnDisable()
        {
            PlayerController.OnTick -= Tick;
            PlayerController.OnMove -= OnMove;
            PlayerController.OnFireStart -= OnFireStart;
            PlayerController.OnFireEnd -= OnFireEnd;
            PlayerController.OnDeath -= OnDeath;
        }
    }
}
