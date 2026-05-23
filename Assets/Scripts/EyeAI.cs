using UnityEngine;

public class EyeAI : MonoBehaviour
{
    [Header("Movimento")]
    public float PreferredDistance = 8f;
    public float ToleranceRange = 1.5f;
    public float MoveSpeed = 3f;
    public float RetreatingSpeed = 2f;

    [Header("Combate")]
    public float ProjectileSpeed = 10f;
    public float AttackCooldown = 2f;
    public GameObject ProjectilePrefab;
    public Transform FirePoint;

    private Transform _playerCamera;
    private Transform _player;
    private float _attackTimer = 0f;

    private void Start()
    {
        _playerCamera = Camera.main.transform;
        _player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (_player == null) return;

        HandleMovement();

        _attackTimer -= Time.deltaTime;
        if (_attackTimer <= 0f)
        {
            Shoot();
            _attackTimer = AttackCooldown;
        }
    }

    private void HandleMovement()
    {
        float distance = Vector3.Distance(transform.position, _player.position);
        Vector3 directionToPlayer = (_player.position - transform.position).normalized;

        if (distance > PreferredDistance + ToleranceRange)
        {
            // too far away: get closer
            transform.position += directionToPlayer * (MoveSpeed * Time.deltaTime);
        }
        else if (distance < PreferredDistance - ToleranceRange)
        {
            // too close: go away
            transform.position -= directionToPlayer * (RetreatingSpeed * Time.deltaTime);
        }

        // always looking at the player
        transform.LookAt(_playerCamera);
    }

    private void Shoot()
    {
        if (ProjectilePrefab == null || FirePoint == null) return;

        Vector3 direction = (_playerCamera.position - FirePoint.position).normalized;

        GameObject proj = Instantiate(ProjectilePrefab, FirePoint.position, Quaternion.LookRotation(direction));
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = direction * ProjectileSpeed;
    }
}