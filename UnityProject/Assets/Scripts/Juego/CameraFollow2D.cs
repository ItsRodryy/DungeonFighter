using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;
    public float smooth = 8f;   // cuanto mayor, más rápido
    Vector3 vel;                // velocidad interna SmoothDamp

    void LateUpdate()
    {
        if (!target) return;
        var goal = new Vector3(target.position.x, target.position.y, -10f);
        float smoothTime = 1f / Mathf.Max(0.0001f, smooth); // 8 -> ~0.125s
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref vel, smoothTime);
    }
}
