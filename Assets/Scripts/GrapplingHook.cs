using UnityEngine;
using StarterAssets;

public class GrapplingHook : MonoBehaviour
{
    [Header("Configuração")]
    public LayerMask PullableLayers;
    public float MaxDistance = 30f;
    public float PullSpeed = 20f;
    public float ExtendSpeed = 50f;
    public float StopDistance = 2f;
    public float Cooldown = 2f;

    [Header("Visual")]
    public LineRenderer Line;
    public Transform FirePoint;

    private enum State { Idle, Extending, Pulling }
    private State _state = State.Idle;

    private Vector3 _targetPoint;
    private Vector3 _lineEnd;
    private float _cooldownTimer = 0f;

    private FirstPersonController _controller;
    private StarterAssetsInputs _input;
    private Camera _camera;

    private void Start()
    {
        _controller = GetComponent<FirstPersonController>();
        _input = GetComponent<StarterAssetsInputs>();
        _camera = Camera.main;
        Line.enabled = false;
    }

    private void Update()
    {
        if (_input.grappleHook && _state == State.Idle && _cooldownTimer <= 0f)
        {
            TryFire();
            _input.grappleHook = false;
        }
        
        if (_cooldownTimer > 0f)
            _cooldownTimer -= Time.deltaTime;
        
        if (_input.cancelGrapple && _state != State.Idle)
        {
            EndHook();
            _input.cancelGrapple = false;
        }

        switch (_state)
        {
            case State.Extending: UpdateExtending(); break;
            case State.Pulling:   UpdatePulling();   break;
        }
    }

    private void TryFire()
    {
        if (_state != State.Idle || _cooldownTimer > 0f) return;

        Ray ray = _camera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f));

        if (Physics.Raycast(ray, out RaycastHit hit, MaxDistance, PullableLayers))
        {
            _targetPoint = hit.point;
            _lineEnd = FirePoint.position;
            Line.SetPosition(0, FirePoint.position);
            Line.SetPosition(1, FirePoint.position);
            Line.enabled = true;
            _state = State.Extending;
        }
        else
        {
            // não acertou nada puxável — desaparece imediatamente
            _cooldownTimer = Cooldown;
        }
    }

    private void UpdateExtending()
    {
        _lineEnd = Vector3.MoveTowards(_lineEnd, _targetPoint, ExtendSpeed * Time.deltaTime);
        Line.SetPosition(0, FirePoint.position);
        Line.SetPosition(1, _lineEnd);

        if (Vector3.Distance(_lineEnd, _targetPoint) < 0.1f)
            _state = State.Pulling;
    }

    private void UpdatePulling()
    {
        _controller.IsGrappling = true;
        
        Line.SetPosition(0, FirePoint.position);
        Line.SetPosition(1, _targetPoint);

        Vector3 direction = (_targetPoint - transform.position).normalized;
        _controller.SetGrappleVelocity(direction * PullSpeed);

        if (Vector3.Distance(transform.position, _targetPoint) <= StopDistance)
            EndHook();
    }

    private void EndHook()
    {
        _controller.IsGrappling = false;
        _controller.ClearGrappleVelocity();
        Line.enabled = false;
        _state = State.Idle;
        _cooldownTimer = Cooldown;
    }
}