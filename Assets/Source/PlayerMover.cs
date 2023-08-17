using System.Collections;
using UnityEngine;

namespace Source
{
	public class PlayerMover : MonoBehaviour
	{
		public Rigidbody2D Rigidbody2D { get; private set; }
		public PlayerAnimator PlayerAnimator { get; private set; }
        
		public bool IsFacingRight { get; private set; }
		public bool IsJumping { get; private set; }
		public bool IsWallJumping { get; private set; }
		public bool IsDashing { get; private set; }
		public bool IsSliding { get; private set; }
        
		public float LastOnGroundTime { get; private set; }
		public float LastOnWallTime { get; private set; }
		public float LastOnWallRightTime { get; private set; }
		public float LastOnWallLeftTime { get; private set; }
		public float LastPressedJumpTime { get; private set; }
		public float LastPressedDashTime { get; private set; }
        
		public PlayerMovementSettings _movementSettings;
		
		[HideInInspector] public Vector2 MoveInput;
		
		[SerializeField] private WallChecker _wallChecker;
		[SerializeField] private GroundChecker _groundChecker;
		
		private bool _isJumpCut;
		private bool _isJumpFalling;
        
		private float _wallJumpStartTime;
		private int _lastWallJumpDir;
        
		private int _dashesLeft;
		private bool _dashRefilling;
		private Vector2 _lastDashDir;
		private bool _isDashAttacking;

		private void Awake()
		{
			Rigidbody2D = GetComponent<Rigidbody2D>();
			PlayerAnimator = GetComponent<PlayerAnimator>();
		}

		private void Start()
		{
			SetGravityScale(_movementSettings.gravityScale);
			IsFacingRight = true;
		}

		private void Update()
		{
			UpdateTimers();
			UpdateCollisions();
			CalculateJumping();
			CalculateDash();
			CalculateSlide();
			CalculateGravity();
		}

		private void CalculateGravity()
		{
			if (_isDashAttacking == false)
			{
				if (IsSliding)
				{
					SetGravityScale(0);
				}
				else if (Rigidbody2D.velocity.y < 0 && MoveInput.y < 0)
				{
					SetGravityScale(_movementSettings.gravityScale * _movementSettings.fastFallGravityMult);
					Rigidbody2D.velocity = new Vector2(Rigidbody2D.velocity.x,
						Mathf.Max(Rigidbody2D.velocity.y, -_movementSettings.maxFastFallSpeed));
				}
				else if (_isJumpCut)
				{
					SetGravityScale(_movementSettings.gravityScale * _movementSettings.jumpCutGravityMult);
					Rigidbody2D.velocity = new Vector2(Rigidbody2D.velocity.x,
						Mathf.Max(Rigidbody2D.velocity.y, -_movementSettings.maxFallSpeed));
				}
				else if ((IsJumping || IsWallJumping || _isJumpFalling) &&
				         Mathf.Abs(Rigidbody2D.velocity.y) < _movementSettings.jumpHangTimeThreshold)
				{
					SetGravityScale(_movementSettings.gravityScale * _movementSettings.jumpHangGravityMult);
				}
				else if (Rigidbody2D.velocity.y < 0)
				{
					SetGravityScale(_movementSettings.gravityScale * _movementSettings.fallGravityMult);
					Rigidbody2D.velocity = new Vector2(Rigidbody2D.velocity.x,
						Mathf.Max(Rigidbody2D.velocity.y, -_movementSettings.maxFallSpeed));
				}
				else
				{
					SetGravityScale(_movementSettings.gravityScale);
				}
			}
			else
			{
				SetGravityScale(0);
			}
		}

		private void CalculateSlide()
		{
			if (CanSlide() && ((LastOnWallLeftTime > 0 && MoveInput.x < 0) || (LastOnWallRightTime > 0 && MoveInput.x > 0)))
			{
				IsSliding = true;
			}
			else
				IsSliding = false;
		}

		private void CalculateDash()
		{
			if (CanDash() && LastPressedDashTime > 0)
			{
				Sleep(_movementSettings.dashSleepTime);
                
				if (MoveInput == Vector2.zero == false)
					_lastDashDir = MoveInput;
				else
					_lastDashDir = IsFacingRight ? Vector2.right : Vector2.left;


				IsDashing = true;
				IsJumping = false;
				IsWallJumping = false;
				_isJumpCut = false;

				StartCoroutine(nameof(StartDash), _lastDashDir);
			}
		}

		private void CalculateJumping()
		{
			if (IsJumping && Rigidbody2D.velocity.y < 0)
			{
				IsJumping = false;

				_isJumpFalling = true;
			}

			if (IsWallJumping && Time.time - _wallJumpStartTime > _movementSettings.wallJumpTime)
			{
				IsWallJumping = false;
			}

			if (LastOnGroundTime > 0 && IsJumping == false && IsWallJumping == false)
			{
				_isJumpCut = false;

				_isJumpFalling = false;
			}

			if (IsDashing == false)
			{
				if (CanJump() && LastPressedJumpTime > 0)
				{
					IsJumping = true;
					IsWallJumping = false;
					_isJumpCut = false;
					_isJumpFalling = false;
					Jump();

					PlayerAnimator.IsStartedJumping = true;
				}
				else if (CanWallJump() && LastPressedJumpTime > 0)
				{
					IsWallJumping = true;
					IsJumping = false;
					_isJumpCut = false;
					_isJumpFalling = false;

					_wallJumpStartTime = Time.time;
					_lastWallJumpDir = (LastOnWallRightTime > 0) ? -1 : 1;

					WallJump(_lastWallJumpDir);
				}
			}
		}

		private void UpdateCollisions()
		{
			if (IsDashing == false && IsJumping == false)
			{
				if (_groundChecker.CheckGround())
				{
					if (LastOnGroundTime < -0.1f)
					{
						PlayerAnimator.IsJustLanded = true;
					}

					LastOnGroundTime = _movementSettings.coyoteTime;
				}
                
				if ((_wallChecker.CheckFrontWall(IsFacingRight) || _wallChecker.CheckBackWal(IsFacingRight) && IsWallJumping == false))
					LastOnWallRightTime = _movementSettings.coyoteTime;
                
				if ((_wallChecker.CheckFrontWall(IsFacingRight == false) || _wallChecker.CheckBackWal(IsFacingRight)) && IsWallJumping)
					LastOnWallLeftTime = _movementSettings.coyoteTime;
                
				LastOnWallTime = Mathf.Max(LastOnWallLeftTime, LastOnWallRightTime);
			}
		}

		private void UpdateTimers()
		{
			LastOnGroundTime -= Time.deltaTime;
			LastOnWallTime -= Time.deltaTime;
			LastOnWallRightTime -= Time.deltaTime;
			LastOnWallLeftTime -= Time.deltaTime;

			LastPressedJumpTime -= Time.deltaTime;
			LastPressedDashTime -= Time.deltaTime;
		}

		private void FixedUpdate()
		{
			if (IsDashing == false)
			{
				if (IsWallJumping)
					Run(_movementSettings.wallJumpRunLerp);
				else
					Run(1);
			}
			else if (_isDashAttacking)
			{
				Run(_movementSettings.dashEndRunLerp);
			}
            
			if (IsSliding)
				Slide();
		}

		public void OnJumpInput()
		{
			LastPressedJumpTime = _movementSettings.jumpInputBufferTime;
		}

		public void OnJumpUpInput()
		{
			if (CanJumpCut() || CanWallJumpCut())
				_isJumpCut = true;
		}

		public void OnDashInput()
		{
			LastPressedDashTime = _movementSettings.dashInputBufferTime;
		}

		private void SetGravityScale(float scale)
		{
			Rigidbody2D.gravityScale = scale;
		}

		private void Sleep(float duration)
		{
			StartCoroutine(nameof(PerformSleep), duration);
		}

		private IEnumerator PerformSleep(float duration)
		{
			Time.timeScale = 0;
			yield return new WaitForSecondsRealtime(duration); 
			Time.timeScale = 1;
		}
        
		private void Run(float lerpAmount)
		{
			var targetSpeed = MoveInput.x * _movementSettings.runMaxSpeed;
			targetSpeed = Mathf.Lerp(Rigidbody2D.velocity.x, targetSpeed, lerpAmount);
            
			float accelRate;
            
			if (LastOnGroundTime > 0)
				accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? _movementSettings.runAccelAmount : _movementSettings.runDeccelAmount;
			else
				accelRate = (Mathf.Abs(targetSpeed) > 0.01f) ? _movementSettings.runAccelAmount * _movementSettings.accelInAir : _movementSettings.runDeccelAmount * _movementSettings.deccelInAir;
            
			if ((IsJumping || IsWallJumping || _isJumpFalling) && Mathf.Abs(Rigidbody2D.velocity.y) < _movementSettings.jumpHangTimeThreshold)
			{
				accelRate *= _movementSettings.jumpHangAccelerationMult;
				targetSpeed *= _movementSettings.jumpHangMaxSpeedMult;
			}
            
			if (_movementSettings.doConserveMomentum && Mathf.Abs(Rigidbody2D.velocity.x) > Mathf.Abs(targetSpeed) && Mathf.Sign(Rigidbody2D.velocity.x) == Mathf.Sign(targetSpeed) && Mathf.Abs(targetSpeed) > 0.01f && LastOnGroundTime < 0)
			{
				accelRate = 0; 
			}

			var speedDif = targetSpeed - Rigidbody2D.velocity.x;
			var movement = speedDif * accelRate;
            
			Rigidbody2D.AddForce(movement * Vector2.right, ForceMode2D.Force);
		}

		private void Turn()
		{
			var scale = transform.localScale; 
			scale.x *= -1;
			transform.localScale = scale;

			IsFacingRight = IsFacingRight == false;
		}
        
		private void Jump()
		{
			LastPressedJumpTime = 0;
			LastOnGroundTime = 0;
            
			var force = _movementSettings.jumpForce;
			
			if (Rigidbody2D.velocity.y < 0)
				force -= Rigidbody2D.velocity.y;

			Rigidbody2D.AddForce(Vector2.up * force, ForceMode2D.Impulse);
		}

		private void WallJump(int dir)
		{
			LastPressedJumpTime = 0;
			LastOnGroundTime = 0;
			LastOnWallRightTime = 0;
			LastOnWallLeftTime = 0;
            
			var force = new Vector2(_movementSettings.wallJumpForce.x, _movementSettings.wallJumpForce.y);
			force.x *= dir;

			if (Mathf.Sign(Rigidbody2D.velocity.x) != Mathf.Sign(force.x))
				force.x -= Rigidbody2D.velocity.x;

			if (Rigidbody2D.velocity.y < 0)
				force.y -= Rigidbody2D.velocity.y;
            
			Rigidbody2D.AddForce(force, ForceMode2D.Impulse);
		}
        
		private IEnumerator StartDash(Vector2 dir)
		{
			LastOnGroundTime = 0;
			LastPressedDashTime = 0;

			var startTime = Time.time;

			_dashesLeft--;
			_isDashAttacking = true;

			SetGravityScale(0);
            
			while (Time.time - startTime <= _movementSettings.dashAttackTime)
			{
				Rigidbody2D.velocity = dir.normalized * _movementSettings.dashSpeed;
				yield return null;
			}

			startTime = Time.time;

			_isDashAttacking = false;
            
			SetGravityScale(_movementSettings.gravityScale);
			Rigidbody2D.velocity = _movementSettings.dashEndSpeed * dir.normalized;

			while (Time.time - startTime <= _movementSettings.dashEndTime)
			{
				yield return null;
			}
            
			IsDashing = false;
		}
        
		private IEnumerator RefillDash(int amount)
		{
			_dashRefilling = true;
			yield return new WaitForSeconds(_movementSettings.dashRefillTime);
			_dashRefilling = false;
			_dashesLeft = Mathf.Min(_movementSettings.dashAmount, _dashesLeft + 1);
		}
        
		private void Slide()
		{
			if (Rigidbody2D.velocity.y > 0)
			{
				Rigidbody2D.AddForce(-Rigidbody2D.velocity.y * Vector2.up, ForceMode2D.Impulse);
			}
            
			var speedDifference = _movementSettings.slideSpeed - Rigidbody2D.velocity.y;	
			var movement = speedDifference * _movementSettings.slideAccel;
			movement = Mathf.Clamp(movement, -Mathf.Abs(speedDifference)  * (1 / Time.fixedDeltaTime), Mathf.Abs(speedDifference) * (1 / Time.fixedDeltaTime));

			Rigidbody2D.AddForce(movement * Vector2.up);
		}


		public void CheckDirectionToFace(bool isMovingRight)
		{
			if (isMovingRight == IsFacingRight == false)
				Turn();
		}

		private bool CanJump()
		{
			return LastOnGroundTime > 0 && !IsJumping;
		}

		private bool CanWallJump()
		{
			return LastPressedJumpTime > 0 && LastOnWallTime > 0 && LastOnGroundTime <= 0 && (!IsWallJumping ||
				(LastOnWallRightTime > 0 && _lastWallJumpDir == 1) || (LastOnWallLeftTime > 0 && _lastWallJumpDir == -1));
		}

		private bool CanJumpCut()
		{
			return IsJumping && Rigidbody2D.velocity.y > 0;
		}

		private bool CanWallJumpCut()
		{
			return IsWallJumping && Rigidbody2D.velocity.y > 0;
		}

		private bool CanDash()
		{
			if (!IsDashing && _dashesLeft < _movementSettings.dashAmount && LastOnGroundTime > 0 && !_dashRefilling)
			{
				StartCoroutine(nameof(RefillDash), 1);
			}

			return _dashesLeft > 0;
		}

		private bool CanSlide()
		{
			return LastOnWallTime > 0 && !IsJumping && !IsWallJumping && !IsDashing && LastOnGroundTime <= 0;
		}
	}
}