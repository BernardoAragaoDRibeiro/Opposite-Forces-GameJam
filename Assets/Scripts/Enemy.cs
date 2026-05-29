using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Hit Feedback")]
    public Color HitColor = Color.orange;
    public float HitFreezeDuration = 0.1f;

    private Renderer _renderer;
    private Color _originalColor;
    
    public float MaxHP = 100f;
    private float _currentHP;
    
    [Header("Valor de Pontuação")]
    public int ScoreValue = 10;

    private void Start()
    {
        _currentHP = MaxHP;
        _renderer = GetComponent<Renderer>();

        if (_renderer != null)
        {
            _originalColor = _renderer.material.color;
        }
    }

    public void TakeDamage(float damage)
    {
        _currentHP -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. HP: {_currentHP}");
        
        StopAllCoroutines();
        StartCoroutine(HitRoutine());

        if (_currentHP <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");
    
        WaveManager waveManager = FindFirstObjectByType<WaveManager>();
        if (waveManager != null)
            waveManager.OnEnemyDied();

        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(ScoreValue);
        
        Destroy(gameObject);
    }

    private IEnumerator HitRoutine()
    {
        // color flash
        if (_renderer != null)
        {
            _renderer.material.color = HitColor;
        }
        
        // stopping the agent
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        var ai = GetComponent<GoblinAI>();
        if (agent != null) agent.isStopped = true;
        if (ai != null) ai.enabled = false;
        
        yield return new WaitForSeconds(HitFreezeDuration);
        
        // turn the agent back on
        if (agent != null) agent.isStopped = false;
        if (ai != null) ai.enabled = true;
        
        yield return new WaitForSeconds(HitFreezeDuration);
        
        // turn back original colors
        if (_renderer != null) _renderer.material.color = _originalColor;
    }
}