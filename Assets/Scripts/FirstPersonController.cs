using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	[RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
	[RequireComponent(typeof(PlayerInput))]
#endif
	public class FirstPersonController : MonoBehaviour
	{
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float MoveSpeed = 4.0f;
		[Tooltip("Sprint speed of the character in m/s")]
		public float SprintSpeed = 6.0f;
		[Tooltip("Rotation speed of the character")]
		public float RotationSpeed = 1.0f;
		[Tooltip("Acceleration and deceleration")]
		public float SpeedChangeRate = 10.0f;

		[Space(10)]
		[Tooltip("Altura mínima do pulo (0% de carga)")]
		public float MinJumpHeight = 0.5f;
		[Tooltip("Altura máxima do pulo (100% de carga)")]
		public float MaxJumpHeight = 3.0f;
		[Tooltip("Tempo em segundos para carregar 100%")]
		public float MaxChargeTime = 1.0f;
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float Gravity = -15.0f;
		[Tooltip("Jump Audio")]
		public AudioClip JumpClip;
		[Range(0f, 1f)] public float JumpVolume = 1f;

		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float JumpTimeout = 0.1f;
		[Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float FallTimeout = 0.15f;

		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool Grounded = true;
		[Tooltip("Useful for rough ground")]
		public float GroundedOffset = -0.14f;
		[Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float GroundedRadius = 0.5f;
		[Tooltip("What layers the character uses as ground")]
		public LayerMask GroundLayers;

		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject CinemachineCameraTarget;
		[Tooltip("How far in degrees can you move the camera up")]
		public float TopClamp = 90.0f;
		[Tooltip("How far in degrees can you move the camera down")]
		public float BottomClamp = -90.0f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;
		public float LastFallVelocity { get; private set; }
		public bool IsSwinging = false;
		private Vector3 _grappleVelocity;
		private float _originalSpeedChangeRate;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;

		// charge jump
		private float _jumpCharge = 0f;
		private bool _isCharging = false;
		public float JumpCharge => _jumpCharge;

#if ENABLE_INPUT_SYSTEM
		private PlayerInput _playerInput;
#endif
		private CharacterController _controller;
		private StarterAssetsInputs _input;
		private GameObject _mainCamera;

		private const float _threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
				#if ENABLE_INPUT_SYSTEM
				return _playerInput.currentControlScheme == "KeyboardMouse";
				#else
				return false;
				#endif
			}
		}

		private void Awake()
		{
			if (_mainCamera == null)
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<StarterAssetsInputs>();
			#if ENABLE_INPUT_SYSTEM
			_playerInput = GetComponent<PlayerInput>();
			#else
			Debug.LogError("Starter Assets package is missing dependencies.");
			#endif

			_jumpTimeoutDelta = JumpTimeout;
			_fallTimeoutDelta = FallTimeout;
			_originalSpeedChangeRate = SpeedChangeRate;
			RotationSpeed = PlayerPrefs.GetFloat("MouseSensitivity", 1f);

			if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Game")
			{
				Cursor.lockState = CursorLockMode.Locked;
				Cursor.visible   = false;
			}
		}

		private void Update()
		{
			if (!IsSwinging)
				JumpAndGravity();
			GroundedCheck();
			if (!IsSwinging)
				Move();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void GroundedCheck()
		{
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
			Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			if (PauseManager.Instance != null && PauseManager.Instance.IsPaused) return;
			if (_input.look.sqrMagnitude >= _threshold)
			{
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

				_cinemachineTargetPitch += _input.look.y * RotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input.look.x * RotationSpeed * deltaTimeMultiplier;

				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);
				CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move()
		{
			float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed;
			if (_input.move == Vector2.zero) targetSpeed = 0.0f;

			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
			float speedOffset = 0.1f;
			float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * SpeedChangeRate);
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;
			if (_input.move != Vector2.zero)
				inputDirection = transform.right * _input.move.x + transform.forward * _input.move.y;

			// Decai o impulso do grapple
			if (_grappleVelocity.magnitude > 2f)
			{
				_grappleVelocity = Vector3.MoveTowards(_grappleVelocity, Vector3.zero, 60f * Time.deltaTime);
			}
			else
			{
				_grappleVelocity = Vector3.zero;
				// Restaura SpeedChangeRate quando impulso acabar
				if (SpeedChangeRate != _originalSpeedChangeRate)
					SpeedChangeRate = _originalSpeedChangeRate;
			}

			// Movimento normal sempre funciona — grapple é somado por cima
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime)
							 + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime
							 + _grappleVelocity * Time.deltaTime);
		}

		private void JumpAndGravity()
		{
			if (_verticalVelocity < 0f)
				LastFallVelocity = _verticalVelocity;

			#if ENABLE_INPUT_SYSTEM
			bool jumpHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
			#else
			bool jumpHeld = Input.GetKey(KeyCode.Space);
			#endif

			if (jumpHeld)
			{
				_isCharging = true;
				_jumpCharge = Mathf.Clamp01(_jumpCharge + Time.deltaTime / MaxChargeTime);
			}

			if (Grounded)
			{
				_fallTimeoutDelta = FallTimeout;

				if (_verticalVelocity < 0.0f)
					_verticalVelocity = -2f;

				if (_jumpTimeoutDelta <= 0.0f && _isCharging && !jumpHeld)
				{
					float jumpHeight = Mathf.Lerp(MinJumpHeight, MaxJumpHeight, _jumpCharge);
					_verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Gravity);
					_jumpCharge = 0f;
					_isCharging = false;

					if (JumpClip != null && AudioManager.Instance != null)
						AudioManager.Instance.PlaySFX(JumpClip, JumpVolume);
				}

				if (_jumpTimeoutDelta >= 0.0f)
					_jumpTimeoutDelta -= Time.deltaTime;
			}
			else
			{
				_jumpTimeoutDelta = JumpTimeout;

				if (_fallTimeoutDelta >= 0.0f)
					_fallTimeoutDelta -= Time.deltaTime;

				_input.jump = false;
			}

			if (_verticalVelocity < _terminalVelocity)
				_verticalVelocity += Gravity * Time.deltaTime;
		}

		public float ConsumeCharge()
		{
			float charge = _jumpCharge;
			_jumpCharge = 0f;
			_isCharging = false;
			return charge;
		}

		public void Bounce(float force)
		{
			_verticalVelocity = force;
		}

		public void SetGrappleVelocity(Vector3 velocity)
		{
			_grappleVelocity = velocity;
			SpeedChangeRate = 50f; // resposta imediata durante o impulso
		}

		private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
		{
			if (lfAngle < -360f) lfAngle += 360f;
			if (lfAngle > 360f) lfAngle -= 360f;
			return Mathf.Clamp(lfAngle, lfMin, lfMax);
		}

		private void OnDrawGizmosSelected()
		{
			Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
			Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

			if (Grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
		}
	}
}