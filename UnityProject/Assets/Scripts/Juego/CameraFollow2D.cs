using UnityEngine;

public class CameraFollow2D : MonoBehaviour
{
    public Transform target;  // Player
    public float smooth = 8f; // mayor = más rápido

    void LateUpdate()
    {
        if (!target) return;
        Vector3 pos = transform.position;
        pos = Vector3.Lerp(pos, new Vector3(target.position.x, target.position.y, -10f), smooth * Time.deltaTime);
        transform.position = pos;
    }
}
