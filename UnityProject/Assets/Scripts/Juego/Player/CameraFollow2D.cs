using UnityEngine;

// Seguimos al objetivo con suavizado usando SmoothDamp
public class CameraFollow2D : MonoBehaviour
{
    public Transform target;

    public float smooth = 8f;

    Vector3 vel;

    void LateUpdate()
    {
        // Si no hay objetivo no hacemos nada
        if (!target) return;

        // Construimos posición objetivo manteniendo z fija
        var goal = new Vector3(target.position.x, target.position.y, -10f);

        // Convertimos smooth a un smoothTime estable
        float smoothTime = 1f / Mathf.Max(0.0001f, smooth);

        // Movemos la cámara suavemente hacia el objetivo
        transform.position = Vector3.SmoothDamp(
            transform.position,
            goal,
            ref vel,
            smoothTime
        );
    }
}
