using UnityEngine;

namespace UFO
{
    public class PlayerAnimator : AnimatorBase
    {
        public Animator BodyAnimator;
        public Transform ThrusterLeft, ThrusterRight;

        private Transform _thrusters;

        public enum AnimState
        {
            Neutral = 0,
            BankLeft,
            BankRight
        }

        private static int[] _stateIDs = new int[] { Animator.StringToHash("ufo_neutral"),
                                                     Animator.StringToHash("ufo_bankleft"),
                                                     Animator.StringToHash("ufo_bankright") };

        private Vector3 _thrusterLeftPos, _thrusterRightPos;
        private const float _pixelSize = 1.0f / 64.0f;

        private float _firingScale;

        private void Start()
        {
            _thrusters = ThrusterLeft.parent;
            _thrusterLeftPos = (Vector2)ThrusterLeft.localPosition;
            _thrusterRightPos = (Vector2)ThrusterRight.localPosition;
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
                    GoToState(BodyAnimator, _stateIDs[(int)AnimState.BankLeft]);
                    ThrusterLeft.localPosition = _thrusterLeftPos + new Vector3(-2 * _pixelSize, 0.0f, 0.0f);
                    ThrusterRight.localPosition = _thrusterRightPos + new Vector3(-2 * _pixelSize, 0.0f, 0.0f);

                    ThrusterLeft.rotation *= Quaternion.Euler(0.0f, 0.0f, -30.0f);
                }
                else
                {
                    GoToState(BodyAnimator, _stateIDs[(int)AnimState.BankRight]);
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

            GoToState(BodyAnimator, _stateIDs[(int)AnimState.Neutral]);
            ThrusterLeft.localPosition = _thrusterLeftPos;
            ThrusterRight.localPosition = _thrusterRightPos;
        }

        private void OnFireStart()
        {
            _firingScale = 3.0f;
        }

        private void OnFireEnd()
        {
            _firingScale = 0.0f;
        }

        private void FixedUpdate()
        {
            Vector3 offset = _firingScale * _pixelSize * Mathf.Sin(2.0f * Mathf.PI * 10.0f * Time.time) * Vector3.up;
            ThrusterLeft.localPosition = _thrusterLeftPos + offset;
            ThrusterRight.localPosition = _thrusterRightPos + offset;
        }

        private void OnEnable()
        {
            PlayerController.OnMove += OnMove;
            PlayerController.OnFireStart += OnFireStart;
            PlayerController.OnFireEnd += OnFireEnd;
        }

        private void OnDisable()
        {
            PlayerController.OnMove -= OnMove;
            PlayerController.OnFireStart -= OnFireStart;
            PlayerController.OnFireEnd -= OnFireEnd;
        }
    }
}
