using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenTilt : MonoBehaviour
{
    [Header("Alvo")]
    public Transform target;

    [Header("Intensidade")]
    public float tiltAmount  = 3f;
    public float smoothSpeed = 5f;

    private Quaternion baseRotation;

    private void Awake()
    {
        if (target == null) target = transform;
        baseRotation = target.localRotation;
    }

    private void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        // Clamp para garantir que o mouse fora da tela não extrapola
        float mouseX = Mathf.Clamp((mousePos.x / Screen.width  - 0.5f) * 2f, -1f, 1f);
        float mouseY = Mathf.Clamp((mousePos.y / Screen.height - 0.5f) * 2f, -1f, 1f);

        Quaternion targetRotation = baseRotation * Quaternion.Euler(
            -mouseY * tiltAmount,
             mouseX * tiltAmount,
             0f
        );

        target.localRotation = Quaternion.Slerp(
            target.localRotation,
            targetRotation,
            Time.deltaTime * smoothSpeed
        );
    }
}