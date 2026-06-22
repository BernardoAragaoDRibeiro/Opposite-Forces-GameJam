using System;
using UnityEngine;
using UnityEngine.AI;

public class GoblinAI : MonoBehaviour
{
    [Header("Combat")]
    public float AttackRange = 1.5f;
    public float AttackDamage = 10f;
    public float AttackCooldown = 1f;

    public bool IsStomped = false;

    private Transform _player;
    private NavMeshAgent _agent;
    private Enemy _enemy;
    private float _attackTimer = 0f;

    private void Start()
    {
        _agent = GetComponent<NavMeshAgent>();
        _enemy = GetComponent<Enemy>();
        _player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    private void Update()
    {
        if (IsStomped) return;
        if (_player == null) return;
        
        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);

        if (distanceToPlayer <= AttackRange)
        {
            _agent.ResetPath(); // Stop moving
            
            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                Attack();
                _attackTimer = AttackCooldown;
            }
        }
        else
        {
            _agent.SetDestination(_player.position); // hunt player down
        }
    }
    
    public void ResetAttackTimer()
    {
        _attackTimer = AttackCooldown;
    }

    public void Attack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
        if (distanceToPlayer <= AttackRange)
        {
            PlayerHealth health = _player.GetComponent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(AttackDamage);
        }
    }
}
