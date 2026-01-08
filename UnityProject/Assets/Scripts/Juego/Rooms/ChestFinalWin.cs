using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ChestFinalWin : MonoBehaviour
{
    public float espera = 3f;
    public string escenaWin = "Win";

    bool hecho = false;

    public void LanzarVictoria()
    {
        if (hecho) return;
        hecho = true;
        StartCoroutine(Rutina());
    }

    IEnumerator Rutina()
    {
        if (JuegoUI.Instance != null)
            JuegoUI.Instance.ShowMessage("JUEGO COMPLETADO");

        yield return new WaitForSeconds(espera);
        SceneManager.LoadScene(escenaWin);
    }
}
