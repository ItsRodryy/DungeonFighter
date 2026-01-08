using UnityEngine;
using UnityEngine.SceneManagement;

public class WinController : MonoBehaviour
{
    void Update()
    {
        if (Input.anyKeyDown)
            SceneManager.LoadScene("MenuPrincipal");
    }
}
