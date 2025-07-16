using UnityEngine;

namespace UFO
{
    public class PlayerAnimator : AnimatorBase
    {
        public Animator BodyAnimator;
        public Transform ThrusterLeft, ThrusterRight;

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

        private void Start()
        {
            _thrusterLeftPos = (Vector2)ThrusterLeft.localPosition;
            _thrusterRightPos = (Vector2)ThrusterRight.localPosition;
        }

        public static void SwitchToState(Animator animator, AnimState state)
        {
            animator.CrossFade(_stateIDs[(int)state], 0.0f);
        }

        private void OnMove(Vector2 move, bool isFiring)
        {
            if (move.x < 0.0f)
            {
                GoToState(BodyAnimator, _stateIDs[(int)AnimState.BankLeft]);
                ThrusterLeft.localPosition = _thrusterLeftPos + new Vector3(-2 * _pixelSize, 0.0f, 0.1f);
                ThrusterRight.localPosition = _thrusterRightPos + new Vector3(-2 * _pixelSize, 0.0f, -0.1f);
            }
            else if (move.x > 0.0f)
            {
                GoToState(BodyAnimator, _stateIDs[(int)AnimState.BankRight]);
                ThrusterLeft.localPosition = _thrusterLeftPos + new Vector3(2 * _pixelSize, 0.0f, -0.1f);
                ThrusterRight.localPosition = _thrusterRightPos + new Vector3(2 * _pixelSize, 0.0f, 0.1f);
            }
            else
            {
                GoToState(BodyAnimator, _stateIDs[(int)AnimState.Neutral]);
                ThrusterLeft.localPosition = _thrusterLeftPos + new Vector3(0.0f, 0.0f, -0.1f);
                ThrusterRight.localPosition = _thrusterRightPos + new Vector3(0.0f, 0.0f, -0.1f);
            }
        }

        private void OnEnable()
        {
            PlayerController.OnMove += OnMove;
        }

        private void OnDisable()
        {
            PlayerController.OnMove -= OnMove;
        }
    }
}
