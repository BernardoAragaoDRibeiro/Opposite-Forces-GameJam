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
        // player damage here
        Debug.Log($"Projétil atingiu: {collision.gameObject.name}");
        Destroy(gameObject);
    }
}