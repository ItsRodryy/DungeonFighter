using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;   // arrastra PF_Player_Swordman
    public float smooth = 8f;  // 6–10 va bien

    Vector3 vel;

    void LateUpdate()
    {
        if (!target) return;
        var goal = new Vector3(target.position.x, target.position.y, -10f);
        float smoothTime = 1f / Mathf.Max(0.0001f, smooth);
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref vel, smoothTime);
    }
}
