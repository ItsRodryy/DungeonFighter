using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
    public Transform target;
    public float z = -10f;

    void LateUpdate()
    {
        if (!target) return;
        transform.position = new Vector3(target.position.x, target.position.y, z);
    }
}
