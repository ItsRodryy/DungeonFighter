using UnityEngine;

public class MiniMapFollow : MonoBehaviour
{
    public Transform target;
    public float z = -10f;

    void LateUpdate()
    {
        // Si no tenemos objetivo salimos y evitamos nulls
        if (!target) return;

        // Cogemos la x e y del objetivo y mantenemos la z fija para la cámara del minimapa
        transform.position = new Vector3(target.position.x, target.position.y, z);
    }
}
