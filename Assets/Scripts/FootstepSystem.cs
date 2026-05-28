using UnityEngine;
using StarterAssets;

public class FootstepSystem : MonoBehaviour
{
    [System.Serializable]
    public struct SurfaceSound
    {
        public PhysicsMaterial Material;
        public AudioClip[] Clips;
    }

    [Header("Superfícies")]
    public SurfaceSound[] Surfaces;
    public AudioClip[] DefaultClips; // toca se nenhum material bater

    [Header("Configuração")]
    public float StepIntervalWalking = 0.5f;
    public float StepIntervalSprinting = 0.3f;
    public float RaycastDistance = 1.5f;
    
    private FirstPersonController _controller;
    private CharacterController _characterController;
    private float _stepTimer;

    private void Start()
    {
        _controller = GetComponent<FirstPersonController>();
        _characterController = GetComponent<CharacterController>();
        _stepTimer = StepIntervalWalking;
    }

    private void Update()
    {
        if (!_controller.Grounded) return;

        float horizontalSpeed = new Vector3(
            _characterController.velocity.x, 0f,
            _characterController.velocity.z).magnitude;

        if (horizontalSpeed < 0.1f) return;

        float interval = horizontalSpeed > 5f ? StepIntervalSprinting : StepIntervalWalking;

        _stepTimer -= Time.deltaTime;
        if (_stepTimer <= 0f)
        {
            PlayFootstep();
            _stepTimer = interval;
        }
    }

    private void PlayFootstep()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, RaycastDistance))
            return;

        PhysicsMaterial material = hit.collider.sharedMaterial;
        AudioClip[] clips = GetClipsForMaterial(material);

        if (clips == null || clips.Length == 0) return;

        AudioClip clip = clips[Random.Range(0, clips.Length)];
        AudioManager.Instance.PlaySFX(clip);
    }

    private AudioClip[] GetClipsForMaterial(PhysicsMaterial material)
    {
        if (material != null)
        {
            foreach (var surface in Surfaces)
            {
                if (surface.Material == material)
                    return surface.Clips;
            }
        }

        return DefaultClips;
    }
}