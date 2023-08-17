using UnityEngine;

namespace Source
{
    public class WallChecker : MonoBehaviour
    {
        [SerializeField] private LayerMask _groundLayer;
        [SerializeField] private Transform _frontWallCheckPoint;
        [SerializeField] private Transform _backWallCheckPoint;
        [SerializeField] private Vector2 _wallCheckSize = new(0.5f, 1f);
        
        public bool CheckFrontWall(bool isFacingRight)
        {
            return Physics2D.OverlapBox(_frontWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && isFacingRight;
        }

        public bool CheckBackWal(bool isFacingRight)
        {
            return Physics2D.OverlapBox(_backWallCheckPoint.position, _wallCheckSize, 0, _groundLayer) && isFacingRight == false;
        }
    }
}