using UnityEngine;
using UnityEngine.Tilemaps;

namespace Source
{
    [RequireComponent(typeof(PlayerMover))]
    public class PlayerAnimator : MonoBehaviour
    {
        public bool IsStartedJumping {  private get; set; }
        public bool IsJustLanded { private get; set; }

        [Header("Movement Tilt")]
        [SerializeField] private float _maxTilt;
        [SerializeField] [Range(0, 1)] private float _tiltSpeed;

        [Header("Particle FX")]
        [SerializeField] private ParticleSystem _jumpParticles;
        [SerializeField] private ParticleSystem _landParticles;
        
        [Header("Components")]
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _renderer;
        
        private PlayerMover _mover;
        private Tilemap _tilemap;
        
        private static readonly int Property = Animator.StringToHash("Vel Y");
        private static readonly int Land = Animator.StringToHash("Land");
        private static readonly int Jump = Animator.StringToHash("Jump");

        public void Start()
        {
            _mover = GetComponent<PlayerMover>();
        }

        private void LateUpdate()
        {
            CalculateTilt();
            UpdateAnimationState();
            UpdateParticlesColor();
        }

        private void CalculateTilt()
        {
            float tiltProgress;
            var multiplier = -1;

            if (_mover.IsSliding)
            {
                tiltProgress = 0.25f;
            }
            else
            {
                tiltProgress = Mathf.InverseLerp(-_mover._movementSettings.runMaxSpeed, _mover._movementSettings.runMaxSpeed,
                    _mover.Rigidbody2D.velocity.x);
                multiplier = (_mover.IsFacingRight) ? 1 : -1;
            }

            var newRotation = ((tiltProgress * _maxTilt * 2) - _maxTilt);
            var rotation = Mathf.LerpAngle(_renderer.transform.localRotation.eulerAngles.z * multiplier, newRotation, _tiltSpeed);
            _renderer.transform.localRotation = Quaternion.Euler(0, 0, rotation * multiplier);
        }

        private void UpdateParticlesColor()
        {
            var colors = _tilemap.GetSprite(new Vector3Int((int)transform.position.x, (int)transform.position.y - 1)).texture
                .GetPixels();

            var color = colors[colors.Length / 2];

            var jumpParticlesSettings = _jumpParticles.main;
            jumpParticlesSettings.startColor = new ParticleSystem.MinMaxGradient(color);
            var landParticlesSettings = _landParticles.main;
            landParticlesSettings.startColor = new ParticleSystem.MinMaxGradient(color);
        }

        private void UpdateAnimationState()
        {
            if (IsStartedJumping)
            {
                _animator.SetTrigger(Jump);
                var obj = Instantiate(_jumpParticles.gameObject, transform.position - (Vector3.up * transform.localScale.y / 2), Quaternion.Euler(-90, 0, 0));
                Destroy(obj, 1);
                IsStartedJumping = false;
                return;
            }

            if (IsJustLanded)
            {
                _animator.SetTrigger(Land);
                var obj = Instantiate(_landParticles.gameObject, transform.position - (Vector3.up * transform.localScale.y / 1.5f), Quaternion.Euler(-90, 0, 0));
                Destroy(obj, 1);
                IsJustLanded = false;
                return;
            }

            _animator.SetFloat(Property, _mover.Rigidbody2D.velocity.y);
        }
    }
}
