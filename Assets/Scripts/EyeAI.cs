using UnityEngine;

public class EyeAI : MonoBehaviour
{
    [Header("Movimento")]
    public float PreferredDistance  = 8f;
    public float ToleranceRange     = 1.5f;
    public float MoveSpeed          = 3f;
    public float RetreatingSpeed    = 2f;
    public float MinHeight          = 3f;

    [Header("Separação entre inimigos")]
    public float SeparationRadius   = 3f;
    public float SeparationForce    = 4f;

    [Header("Combate")]
    public float ProjectileSpeed    = 10f;
    public float AttackCooldown     = 2f;
    public float MaxShootDistance   = 15f;
    public GameObject ProjectilePrefab;
    public Transform FirePoint;
    public AudioClip[] ShootClips;

    private Transform _player;
    private Transform _playerCamera;
    private Rigidbody _rb;
    private float _attackTimer = 0f;

    private void Start()
    {
        _player       = GameObject.FindGameObjectWithTag("Player").transform;
        _playerCamera = Camera.main.transform;

        // Ensure a Rigidbody exists — add one at runtime if the prefab lacks it
        _rb = GetComponent<Rigidbody>();
        if (_rb == null)
        {
            _rb             = gameObject.AddComponent<Rigidbody>();
            _rb.useGravity  = false;
            _rb.isKinematic = false;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
        }

        // Ensure a collider exists for physics separation
        if (GetComponent<Collider>() == null)
        {
            var col    = gameObject.AddComponent<SphereCollider>();
            col.radius = 0.6f;
        }
    }

    private void FixedUpdate()
    {
        if (_player == null) return;

        HandleMovement();
    }

    private void Update()
    {
        if (_player == null) return;

        HandleAttack();
        transform.LookAt(_playerCamera);
    }

    private void HandleMovement()
    {
        float   distance          = Vector3.Distance(transform.position, _player.position);
        Vector3 directionToPlayer = (_player.position - transform.position).normalized;

        // Chase / retreat along player axis
        Vector3 moveDir = Vector3.zero;
        if (distance > PreferredDistance + ToleranceRange)
            moveDir = directionToPlayer * MoveSpeed;
        else if (distance < PreferredDistance - ToleranceRange)
            moveDir = -directionToPlayer * RetreatingSpeed;

        // Peer-separation: push away from nearby EyeAI instances
        foreach (var other in FindObjectsByType<EyeAI>(FindObjectsSortMode.None))
        {
            if (other == this) continue;
            Vector3 away = transform.position - other.transform.position;
            float   dist = away.magnitude;
            if (dist < SeparationRadius && dist > 0.001f)
                moveDir += (away / dist) * SeparationForce * (1f - dist / SeparationRadius);
        }

        // Compute target position and clamp MinHeight
        Vector3 targetPos   = _rb.position + moveDir * Time.fixedDeltaTime;
        targetPos.y         = Mathf.Max(targetPos.y, MinHeight);

        _rb.MovePosition(targetPos);
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
        Rigidbody rb    = proj.GetComponent<Rigidbody>();
        if (rb != null)
            rb.linearVelocity = direction * ProjectileSpeed;
    }
}
