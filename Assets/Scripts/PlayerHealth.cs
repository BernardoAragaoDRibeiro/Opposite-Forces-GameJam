using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float MaxHP = 100f;
    [field: SerializeField]
    public float CurrentHP { get; private set; }

    private void Start()
    {
        CurrentHP = MaxHP;
    }

    public void TakeDamage(float damage)
    {
        CurrentHP = Mathf.Max(CurrentHP - damage, 0f);

        if (CurrentHP <= 0f)
            Die();
    }

    private void Die()
    {
        GameManager.Instance.GoToGameOver();
    }
}