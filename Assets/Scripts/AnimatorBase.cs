using UnityEngine;

namespace UFO
{
    public abstract class AnimatorBase : MonoBehaviour
    {
        protected static void GoToState(Animator animator, int stateID)
        {
            animator.CrossFade(stateID, 0.0f);
        }
    }
}
