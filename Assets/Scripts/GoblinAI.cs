using System;
using UnityEngine;
using UnityEngine.AI;

public class GoblinAI : MonoBehaviour
{
    [Header("Combat")]
    public float AttackRange = 1.5f;
    public float AttackDamage = 10f;
    public float AttackCooldown = 1f;

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

    // attach animation?
    public void Attack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, _player.position);
        if (distanceToPlayer <= AttackRange)
        {
            Debug.Log("Goblin attacked the player!");
            // deal damage logic
            // PlayerHealth.TakeDamage(AttackDamage)
        }
    }
}
