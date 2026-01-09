using UnityEngine;
using UnityEngine.SceneManagement;

public class WinController : MonoBehaviour
{
    void Update()
    {
        // Detectamos cualquier tecla o botón y volvemos al menú principal
        if (Input.anyKeyDown)
            SceneManager.LoadScene("MenuPrincipal");
    }
}
