using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    // Transform del objetivo (normalmente el jugador).
    public Transform target;

    // Factor de suavizado del seguimiento (cuanto más grande, más suave).
    public float smooth = 8f;

    // Velocidad interna usada por SmoothDamp (se modifica dentro de la función).
    Vector3 vel;

    void LateUpdate()
    {
        // Si no hay objetivo, no hacemos nada.
        if (!target) return;

        // Posición objetivo de la cámara (X e Y del jugador, Z fija para 2D).
        var goal = new Vector3(target.position.x, target.position.y, -10f);

        // Suavizado en segundos (evita divisiones por cero).
        float smoothTime = 1f / Mathf.Max(0.0001f, smooth);

        // Mueve la cámara suavemente hacia la posición objetivo.
        transform.position = Vector3.SmoothDamp(
            transform.position,
            goal,
            ref vel,
            smoothTime
        );
    }
}
