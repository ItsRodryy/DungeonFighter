using UnityEngine;
using UnityEngine.SceneManagement;

// Controlamos la pantalla de GameOver y volvemos al menú al detectar cualquier input
public class GameOverController : MonoBehaviour
{
    void Update()
    {
        // Detectamos cualquier tecla click o botón de mando
        if (Input.anyKeyDown)
        {
            // Volvemos al menú principal
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}
