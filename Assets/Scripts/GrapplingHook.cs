using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets;

public class GrapplingHook : MonoBehaviour
{
    [Header("Configuração")]
    public LayerMask PullableLayers;
    public float MaxDistance      = 30f;
    public float ExtendSpeed      = 50f;
    public float Cooldown         = 0.5f;

    [Header("Swing")]
    public float SwingGravity     = 25f;
    [Range(0f, 1f)]
    public float SwingDamping     = 0.02f;  // 0 = sem amortecimento, 1 = para instantâneo
    public float AirControl       = 4f;
    public float PullSpeed        = 6f;
    public float StopDistance     = 2f;

    [Header("Pulo ao Soltar")]
    public float ReleaseJumpForce  = 8f;
    public float ReleaseJumpWindow = 0.3f;

    [Header("Visual")]
    public LineRenderer Line;
    public Transform    FirePoint;

    private enum State { Idle, Extending, Swinging }
    private State _state = State.Idle;

    private Vector3 _anchorPoint;
    private Vector3 _lineEnd;
    private float   _ropeLength;
    private float   _cooldownTimer    = 0f;
    private float   _releaseJumpTimer = 0f;
    private Vector3 _swingVelocity    = Vector3.zero;

    // Controla se o botão estava pressionado no frame anterior
    private bool _wasHeld = false;

    private FirstPersonController _controller;
    private StarterAssetsInputs   _input;
    private CharacterController   _charController;
    private Camera                _camera;

    private void Start()
    {
        _controller     = GetComponent<FirstPersonController>();
        _input          = GetComponent<StarterAssetsInputs>();
        _charController = GetComponent<CharacterController>();
        _camera         = Camera.main;
        Line.enabled    = false;
    }

    private void Update()
    {
        if (_cooldownTimer > 0f)    _cooldownTimer    -= Time.deltaTime;
        if (_releaseJumpTimer > 0f) _releaseJumpTimer -= Time.deltaTime;

        bool held = _input.grappleHook;

        // Pulo na janela após soltar
        bool jumpPressed = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
        if (_releaseJumpTimer > 0f && jumpPressed)
        {
            _controller.Bounce(ReleaseJumpForce);
            _releaseJumpTimer = 0f;
        }

        // Pressionou agora (borda de subida)
        if (held && !_wasHeld && _state == State.Idle && _cooldownTimer <= 0f)
            TryFire();

        // Soltou agora (borda de descida)
        if (!held && _wasHeld && _state != State.Idle)
            ReleaseHook();

        _wasHeld = held;

        switch (_state)
        {
            case State.Extending: UpdateExtending(); break;
            case State.Swinging:  UpdateSwinging();  break;
        }

        if (_state != State.Idle)
        {
            Line.SetPosition(0, FirePoint.position);
            Line.SetPosition(1, _state == State.Extending ? _lineEnd : _anchorPoint);
        }
    }

    private void TryFire()
    {
        Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, MaxDistance, PullableLayers))
        {
            _anchorPoint = hit.point;
            _lineEnd     = FirePoint.position;
            Line.enabled = true;
            _state       = State.Extending;
            Debug.Log($"[GrapplingHook] Ancorou em {_anchorPoint}");
        }
        else
        {
            _cooldownTimer = Cooldown;
            Debug.Log("[GrapplingHook] Nada encontrado.");
        }
    }

    private void UpdateExtending()
    {
        _lineEnd = Vector3.MoveTowards(_lineEnd, _anchorPoint, ExtendSpeed * Time.deltaTime);

        if (Vector3.Distance(_lineEnd, _anchorPoint) < 0.1f)
        {
            _ropeLength             = Vector3.Distance(transform.position, _anchorPoint);
            _swingVelocity          = _charController.velocity;
            _state                  = State.Swinging;
            _controller.IsSwinging  = true;
            Debug.Log($"[GrapplingHook] Swing iniciado. Rope length: {_ropeLength}");
        }
    }

    private void UpdateSwinging()
    {
        Vector3 toAnchor = _anchorPoint - transform.position;
        Vector3 ropeDir  = toAnchor.normalized;

        // Gravity
        _swingVelocity += Vector3.down * SwingGravity * Time.deltaTime;

        // Pendulum constraint: keep velocity tangential (perpendicular to rope)
        _swingVelocity -= Vector3.Dot(_swingVelocity, ropeDir) * ropeDir;

        // Air control applied after projection, input itself projected onto tangent plane
        Vector3 inputDir     = transform.right * _input.move.x + transform.forward * _input.move.y;
        Vector3 tangentInput = inputDir - Vector3.Dot(inputDir, ropeDir) * ropeDir;
        _swingVelocity += tangentInput * AirControl * Time.deltaTime;

        // Shorten rope to pull player toward anchor, never closer than StopDistance
        _ropeLength = Mathf.MoveTowards(_ropeLength, StopDistance, PullSpeed * Time.deltaTime);

        // Frame-rate independent damping
        _swingVelocity *= Mathf.Pow(1f - SwingDamping, Time.deltaTime);

        _charController.Move(_swingVelocity * Time.deltaTime);

        // Position correction: enforce rope length after movement
        toAnchor         = _anchorPoint - transform.position;
        float currentDist = toAnchor.magnitude;
        if (currentDist > _ropeLength)
            _charController.Move(toAnchor.normalized * (currentDist - _ropeLength));
    }

    private void ReleaseHook()
    {
        Line.enabled             = false;
        _state                   = State.Idle;
        _controller.IsSwinging   = false;
        _cooldownTimer           = Cooldown;
        _releaseJumpTimer        = ReleaseJumpWindow;

        if (_swingVelocity.magnitude > 0.5f)
            _controller.SetGrappleVelocity(_swingVelocity);

        _swingVelocity = Vector3.zero;
        Debug.Log("[GrapplingHook] Hook solto.");
    }
}