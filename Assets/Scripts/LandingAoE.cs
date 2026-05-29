using UnityEngine;
using StarterAssets;

public class LandingAoE : MonoBehaviour
{
    [Header("Dano em Área")]
    public float MaxDamage = 50f;
    public float MaxRadius = 5f;
    public float MinFallVelocity = 5f;
    public LayerMask AffectedLayers;

    private FirstPersonController _controller;
    private bool _wasGrounded;

    private void Start()
    {
        _controller = GetComponent<FirstPersonController>();
        _wasGrounded = _controller.Grounded;
    }

    private void Update()
    {
        bool isGrounded = _controller.Grounded;

        if (!_wasGrounded && isGrounded)
            OnLanded();

        _wasGrounded = isGrounded;
    }

    public void TriggerAoE(float fallVelocity)
    {
        float fallSpeed = Mathf.Abs(fallVelocity);
        if (fallSpeed < MinFallVelocity) return;

        float ratio = Mathf.Clamp01(fallSpeed / 20f);
        float damage = ratio * MaxDamage;
        float radius = ratio * MaxRadius;

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, AffectedLayers);
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
                enemy.TakeDamage(damage);
        }
    }

    private void OnLanded()
    {
        TriggerAoE(_controller.LastFallVelocity);
    }
}