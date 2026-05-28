using StarterAssets;
using UnityEngine;

public class EnemyHead : MonoBehaviour
{
    [Tooltip("Minimal velocity to cause damage (m/s)")]
    public float MinImpactVelocity = 3f;
    [Tooltip("Velocity to max damage (m/s)")]
    public float MaxImpactVelocity = 20f;
    [Tooltip("Max damage caused")]
    public float MaxDamage = 100f;
    [Tooltip("Bounce strength hitting enemy")]
    public float BounceStrength = 8f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        
        var controller = other.GetComponent<FirstPersonController>();
        if (controller == null) return;

        float fallSpeed = Mathf.Abs(controller.LastFallVelocity);
        
        // Small fall won't count
        if (fallSpeed < MinImpactVelocity) return;
        
        // Maps fall velocity for damage
        float damageRatio = Mathf.InverseLerp(MinImpactVelocity, MaxImpactVelocity, fallSpeed);
        float damage = damageRatio * MaxDamage;
        
        GetComponentInParent<Enemy>().TakeDamage(damage);
        float charge = controller.ConsumeCharge();
        controller.Bounce(BounceStrength + charge * BounceStrength);
    }
}
