using UnityEngine;

public class EyeAI : MonoBehaviour
{
    [Header("Movimento")]
    public float PreferredDistance = 8f;
    public float ToleranceRange = 1.5f;
    public float MoveSpeed = 3f;
    public float RetreatingSpeed = 2f;
    public float MinHeight = 3f;

    [Header("Combate")]
    public float ProjectileSpeed = 10f;
    public float AttackCooldown = 2f;
    public float MaxShootDistance = 15f;
    public GameObject ProjectilePrefab;
    public Transform FirePoint;
    public AudioClip[] ShootClips;

    private Transform _player;
    private Transform _playerCamera;
    private float _attackTimer = 0f;

    private void Start()
    {
        _player = GameObject.FindGameObjectWithTag("Player").transform;
        _playerCamera = Camera.main.transform;
    }

    private void Update()
    {
        if (_player == null) return;

        HandleMovement();
        HandleAttack();
    }

    private void HandleMovement()
    {
        float distance = Vector3.Distance(transform.position, _player.position);
        Vector3 directionToPlayer = (_player.position - transform.position).normalized;

        if (distance > PreferredDistance + ToleranceRange)
            transform.position += directionToPlayer * (MoveSpeed * Time.deltaTime);
        else if (distance < PreferredDistance - ToleranceRange)
            transform.position -= directionToPlayer * (RetreatingSpeed * Time.deltaTime);

        Vector3 pos = transform.position;
        pos.y = Mathf.Max(pos.y, MinHeight);
        transform.position = pos;

        transform.LookAt(_playerCamera);
    }

    private void HandleAttack()
    {
        float distance = Vector3.Distance(transform.position, _player.position);

        if (distance <= MaxShootDistance)
        {
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                Shoot();
                _attackTimer = AttackCooldown;
            }
        }
        else
        {
            _attackTimer = AttackCooldown;
        }
    }

    private void Shoot()
    {
        if (ProjectilePrefab == null || FirePoint == null) return;

        Vector3 direction = (_playerCamera.position - FirePoint.position).normalized;
        if (ShootClips.Length > 0 && AudioManager.Instance != null)
            AudioManager.Instance.PlaySFX(ShootClips[Random.Range(0, ShootClips.Length)]);
        GameObject proj = Instantiate(ProjectilePrefab, FirePoint.position, Quaternion.LookRotation(direction));
        Rigidbody rb = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = direction * ProjectileSpeed;
    }
}