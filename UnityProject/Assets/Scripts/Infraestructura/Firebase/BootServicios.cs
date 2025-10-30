using UnityEngine;

public class BootServicios : MonoBehaviour
{
    private static BootServicios instancia;

    void Awake()
    {
        if (instancia != null) { Destroy(gameObject); return; }
        instancia = this;
        DontDestroyOnLoad(gameObject);
    }
}
