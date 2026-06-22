using UnityEngine;
using StarterAssets;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EnemyHead : MonoBehaviour
{
    [Header("Dano")]
    public float MinImpactVelocity = 3f;
    public float MaxImpactVelocity = 20f;
    public float MaxDamage = 100f;

    [Header("Bounce")]
    public float BounceStrength = 8f;

    [Header("Pre-jump Detection")]
    public float ProximityRadius = 2f;
    public LayerMask PlayerLayer;

    private bool _wasSpaceHeld = false;
    private bool _bounceQueued = false;

    private void Update()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, ProximityRadius, PlayerLayer);
        bool playerNearby = hits.Length > 0;

        if (!playerNearby)
        {
            _bounceQueued = false;
            _wasSpaceHeld = false;
            return;
        }

        #if ENABLE_INPUT_SYSTEM
        bool spaceHeld = Keyboard.current != null && Keyboard.current.spaceKey.isPressed;
        #else
        bool spaceHeld = Input.GetKey(KeyCode.Space);
        #endif

        if (_wasSpaceHeld && !spaceHeld)
            _bounceQueued = true;

        _wasSpaceHeld = spaceHeld;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        var controller = other.GetComponentInParent<FirstPersonController>();
        if (controller == null) return;

        float fallSpeed = Mathf.Abs(controller.LastFallVelocity);
        if (fallSpeed < MinImpactVelocity) return;

        float damageRatio = Mathf.InverseLerp(MinImpactVelocity, MaxImpactVelocity, fallSpeed);
        GetComponentInParent<Enemy>().TakeDamage(damageRatio * MaxDamage);

        GoblinAI goblinAI = GetComponentInParent<GoblinAI>();
        if (goblinAI != null)
        {
            goblinAI.IsStomped = true;
            goblinAI.ResetAttackTimer();
        }

        float totalBounce = _bounceQueued
            ? BounceStrength + controller.ConsumeCharge() * BounceStrength
            : BounceStrength;

        _bounceQueued = false;
        controller.Bounce(totalBounce);

        LandingAoE aoe = other.GetComponentInParent<LandingAoE>();
        if (aoe != null)
            aoe.TriggerAoE(controller.LastFallVelocity);
    }
}