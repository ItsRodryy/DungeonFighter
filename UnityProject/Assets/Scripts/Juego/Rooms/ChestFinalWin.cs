using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Lanzamos la victoria cuando abrimos el cofre final
public class ChestFinalWin : MonoBehaviour
{
    public float espera = 3f;
    public string escenaWin = "Win";

    bool hecho = false;

    public void LanzarVictoria()
    {
        // Evitamos ejecutar dos veces la rutina
        if (hecho) return;

        hecho = true;
        StartCoroutine(Rutina());
    }

    IEnumerator Rutina()
    {
        // Mostramos mensaje de victoria si tenemos UI
        if (JuegoUI.Instance != null)
            JuegoUI.Instance.ShowMessage("JUEGO COMPLETADO");

        // Esperamos un tiempo antes de cambiar de escena
        yield return new WaitForSeconds(espera);

        // Cargamos la escena de victoria
        SceneManager.LoadScene(escenaWin);
    }
}
