using UnityEngine;

namespace Source
{
    public class GroundChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _groundCheckPoint;
        [SerializeField] private Vector2 _groundCheckSize = new(0.49f, 0.03f);

        public bool CheckGround()
        {
            if (Physics2D.OverlapBox(_groundCheckPoint.position, _groundCheckSize, 0,
                    _groundLayer))
            {
                return true;
            }

            return false;
        }
    }
}