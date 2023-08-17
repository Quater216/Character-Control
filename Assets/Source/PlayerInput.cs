using UnityEngine;

namespace Source
{
    public class PlayerInput : MonoBehaviour
    {
        [SerializeField] private PlayerMover _mover;

        private void Update()
        {
            _mover.MoveInput.x = Input.GetAxisRaw("Horizontal");
            _mover.MoveInput.y = Input.GetAxisRaw("Vertical");

            if (_mover.MoveInput.x == 0 == false)
                _mover.CheckDirectionToFace(_mover.MoveInput.x > 0);

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.J))
            {
                _mover.OnJumpInput();
            }

            if (Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.C) || Input.GetKeyUp(KeyCode.J))
            {
                _mover.OnJumpUpInput();
            }

            if (Input.GetKeyDown(KeyCode.X) || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.K))
            {
                _mover.OnDashInput();
            }
        }
    }
}