using UnityEngine;

// Creamos un objeto persistente para mantener servicios entre escenas
// Así evitamos duplicados y no perdemos referencias al cambiar de escena
public class BootServicios : MonoBehaviour
{
    // Guardamos una referencia estática para asegurarnos de que solo exista una instancia
    private static BootServicios instancia;

    void Awake()
    {
        // Si ya existe otra instancia destruimos esta para no duplicar servicios
        if (instancia != null)
        {
            Destroy(gameObject);
            return;
        }

        // Si no existe la guardamos como instancia principal
        instancia = this;

        // Marcamos este objeto para que no se destruya al cambiar de escena
        DontDestroyOnLoad(gameObject);
    }
}
