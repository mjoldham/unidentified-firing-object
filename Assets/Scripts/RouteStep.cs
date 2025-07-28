using UnityEngine;

namespace UFO
{
    // Place these where you want them on the screen, don't forget to link them together!
    public class RouteStep : MonoBehaviour
    {
        [Min(0)]
        public int BeatsToComplete;
        public RouteStep NextStep;

        private void OnDrawGizmos()
        {
            Vector2 pos = new Vector2(transform.position.x, transform.localPosition.y);
            if (NextStep != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pos, new Vector2(NextStep.transform.position.x, NextStep.transform.localPosition.y));
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(pos, new Vector2(pos.x, -GameManager.ScreenHalfHeight));
            }
        }
    }
}
