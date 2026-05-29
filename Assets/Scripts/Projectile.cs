using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float Damage = 10f;
    public float Lifetime = 5f;

    private void Start()
    {
        Destroy(gameObject, Lifetime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            PlayerHealth health = collision.gameObject.GetComponentInParent<PlayerHealth>();
            if (health != null)
                health.TakeDamage(Damage);
            else
                Debug.Log("PlayerHealth não encontrado no objeto atingido");
        }
        Destroy(gameObject);
    }
}