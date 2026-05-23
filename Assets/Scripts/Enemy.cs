using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float MaxHP = 100f;
    private float _currentHP;

    private void Start()
    {
        _currentHP = MaxHP;
    }

    public void TakeDamage(float damage)
    {
        _currentHP -= damage;
        Debug.Log($"{gameObject.name} took {damage} damage. HP: {_currentHP}");

        if (_currentHP <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} died.");
        Destroy(gameObject);
    }
}