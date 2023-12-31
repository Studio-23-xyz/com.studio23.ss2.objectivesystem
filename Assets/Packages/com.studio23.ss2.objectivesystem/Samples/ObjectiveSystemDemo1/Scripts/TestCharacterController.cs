using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Studio23.SS2.ObjectiveSystem.Samples.ObjectiveSystemDemo1
{
	[RequireComponent(typeof(CharacterController))]
	[RequireComponent(typeof(PlayerInput))]
	public class TestCharacterController : MonoBehaviour
	{
		[FormerlySerializedAs("moveSpeed")]
		[FormerlySerializedAs("MoveSpeed")]
		[Header("Player")]
		[Tooltip("Move speed of the character in m/s")]
		public float _moveSpeed = 4.0f;
		[FormerlySerializedAs("sprintSpeed")] [FormerlySerializedAs("SprintSpeed")] [Tooltip("Sprint speed of the character in m/s")]
		public float _sprintSpeed = 6.0f;
		[FormerlySerializedAs("rotationSpeed")] [FormerlySerializedAs("RotationSpeed")] [Tooltip("Rotation speed of the character")]
		public float _rotationSpeed = 1.0f;
		[FormerlySerializedAs("speedChangeRate")] [FormerlySerializedAs("SpeedChangeRate")] [Tooltip("Acceleration and deceleration")]
		public float _speedChangeRate = 10.0f;

		[FormerlySerializedAs("jumpHeight")]
		[FormerlySerializedAs("JumpHeight")]
		[Space(10)]
		[Tooltip("The height the player can jump")]
		public float _jumpHeight = 1.2f;
		[FormerlySerializedAs("gravity")]
		[FormerlySerializedAs("Gravity")] 
		[Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
		public float _gravity = -15.0f;

		[FormerlySerializedAs("jumpTimeout")]
		[FormerlySerializedAs("JumpTimeout")]
		[Space(10)]
		[Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
		public float _jumpTimeout = 0.1f;
		[FormerlySerializedAs("fallTimeout")] [FormerlySerializedAs("FallTimeout")] [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
		public float _fallTimeout = 0.15f;

		[FormerlySerializedAs("grounded")]
		[FormerlySerializedAs("Grounded")]
		[Header("Player Grounded")]
		[Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
		public bool _grounded = true;
		[FormerlySerializedAs("groundedOffset")] [FormerlySerializedAs("GroundedOffset")] [Tooltip("Useful for rough ground")]
		public float _groundedOffset = -0.14f;
		[FormerlySerializedAs("groundedRadius")] [FormerlySerializedAs("GroundedRadius")] [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
		public float _groundedRadius = 0.5f;
		[FormerlySerializedAs("groundLayers")] [FormerlySerializedAs("GroundLayers")] [Tooltip("What layers the character uses as ground")]
		public LayerMask _groundLayers;

		[FormerlySerializedAs("cinemachineCameraTarget")]
		[FormerlySerializedAs("CinemachineCameraTarget")]
		[Header("Cinemachine")]
		[Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
		public GameObject _cinemachineCameraTarget;
		[FormerlySerializedAs("TopClamp")] [Tooltip("How far in degrees can you move the camera up")]
		public float _topClamp = 90.0f;
		[FormerlySerializedAs("BottomClamp")] [Tooltip("How far in degrees can you move the camera down")]
		public float _bottomClamp = -90.0f;

		// cinemachine
		private float _cinemachineTargetPitch;

		// player
		private float _speed;
		private float _rotationVelocity;
		private float _verticalVelocity;
		private float _terminalVelocity = 53.0f;

		// timeout deltatime
		private float _jumpTimeoutDelta;
		private float _fallTimeoutDelta;
	
		private PlayerInput _playerInput;
		private CharacterController _controller;
		private TestCharacterInputs _input;
		private GameObject _mainCamera;
		
		private const float Threshold = 0.01f;

		private bool IsCurrentDeviceMouse
		{
			get
			{
				return _playerInput.currentControlScheme == "KeyboardMouse";
			}
		}

		public void Toggle(bool isControllerEnabled)
		{
			enabled = isControllerEnabled;
		}

		private void Awake()
		{
			// get a reference to our main camera
			if (_mainCamera == null)
			{
				_mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
			}
		}

		private void Start()
		{
			_controller = GetComponent<CharacterController>();
			_input = GetComponent<TestCharacterInputs>();
			_playerInput = GetComponent<PlayerInput>();

			// reset our timeouts on start
			_jumpTimeoutDelta = _jumpTimeout;
			_fallTimeoutDelta = _fallTimeout;
		}

		private void Update()
		{
			JumpAndGravity();
			GroundedCheck();
			Move();
		}

		private void LateUpdate()
		{
			CameraRotation();
		}

		private void GroundedCheck()
		{
			// set sphere position, with offset
			Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z);
			_grounded = Physics.CheckSphere(spherePosition, _groundedRadius, _groundLayers, QueryTriggerInteraction.Ignore);
		}

		private void CameraRotation()
		{
			// if there is an input
			if (_input._look.sqrMagnitude >= Threshold)
			{
				//Don't multiply mouse input by Time.deltaTime
				float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
				
				_cinemachineTargetPitch += _input._look.y * _rotationSpeed * deltaTimeMultiplier;
				_rotationVelocity = _input._look.x * _rotationSpeed * deltaTimeMultiplier;

				// clamp our pitch rotation
				_cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, _bottomClamp, _topClamp);

				// Update Cinemachine camera target pitch
				_cinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

				// rotate the player left and right
				transform.Rotate(Vector3.up * _rotationVelocity);
			}
		}

		private void Move()
		{
			// set target speed based on move speed, sprint speed and if sprint is pressed
			float targetSpeed = _input._sprint ? _sprintSpeed : _moveSpeed;

			// a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

			// note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is no input, set the target speed to 0
			if (_input._move == Vector2.zero) targetSpeed = 0.0f;

			// a reference to the players current horizontal velocity
			float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

			float speedOffset = 0.1f;
			float inputMagnitude = _input._analogMovement ? _input._move.magnitude : 1f;

			// accelerate or decelerate to target speed
			if (currentHorizontalSpeed < targetSpeed - speedOffset || currentHorizontalSpeed > targetSpeed + speedOffset)
			{
				// creates curved result rather than a linear one giving a more organic speed change
				// note T in Lerp is clamped, so we don't need to clamp our speed
				_speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude, Time.deltaTime * _speedChangeRate);

				// round speed to 3 decimal places
				_speed = Mathf.Round(_speed * 1000f) / 1000f;
			}
			else
			{
				_speed = targetSpeed;
			}

			// normalise input direction
			Vector3 inputDirection = new Vector3(_input._move.x, 0.0f, _input._move.y).normalized;

			// note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
			// if there is a move input rotate player when the player is moving
			if (_input._move != Vector2.zero)
			{
				// move
				inputDirection = transform.right * _input._move.x + transform.forward * _input._move.y;
			}

			// move the player
			_controller.Move(inputDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
		}

		private void JumpAndGravity()
		{
			if (_grounded)
			{
				// reset the fall timeout timer
				_fallTimeoutDelta = _fallTimeout;

				// stop our velocity dropping infinitely when grounded
				if (_verticalVelocity < 0.0f)
				{
					_verticalVelocity = -2f;
				}

				// Jump
				if (_input._jump && _jumpTimeoutDelta <= 0.0f)
				{
					// the square root of H * -2 * G = how much velocity needed to reach desired height
					_verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
				}

				// jump timeout
				if (_jumpTimeoutDelta >= 0.0f)
				{
					_jumpTimeoutDelta -= Time.deltaTime;
				}
			}
			else
			{
				// reset the jump timeout timer
				_jumpTimeoutDelta = _jumpTimeout;

				// fall timeout
				if (_fallTimeoutDelta >= 0.0f)
				{
					_fallTimeoutDelta -= Time.deltaTime;
				}

				// if we are not grounded, do not jump
				_input._jump = false;
			}

			// apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
			if (_verticalVelocity < _terminalVelocity)
			{
				_verticalVelocity += _gravity * Time.deltaTime;
			}
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

			if (_grounded) Gizmos.color = transparentGreen;
			else Gizmos.color = transparentRed;

			// when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
			Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - _groundedOffset, transform.position.z), _groundedRadius);
		}
	}
}
