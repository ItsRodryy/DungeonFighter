using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverController : MonoBehaviour
{
    void Update()
    {
        // Cualquier tecla / click / botón mando
        if (Input.anyKeyDown)
        {
            // Vuelve al menú principal
            SceneManager.LoadScene("MenuPrincipal");
        }
    }
}
