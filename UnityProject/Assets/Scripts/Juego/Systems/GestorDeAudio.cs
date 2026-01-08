using UnityEngine;
using UnityEngine.SceneManagement;

public class GestorDeAudio : MonoBehaviour
{
    public static GestorDeAudio I;

    [Header("Fuentes (AudioSource)")]
    public AudioSource fuenteMusica; // hijo "Musica"
    public AudioSource fuentePasos;  // hijo "Pasos"
    public AudioSource fuenteSFX;    // hijo "SFX"
    public AudioSource fuenteGolpe;  // hijo "Golpe"

    [Header("Clips")]
    public AudioClip mainTheme;
    public AudioClip walking;
    public AudioClip punch;
    public AudioClip gate;
    public AudioClip dead;
    public AudioClip punchEnemy;
    public AudioClip uiClick;

    bool yaSonoDead = false;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        string nombre = escena.name.ToLower().Trim();

        // 1) GameOver: NO música, NO pasos, NO golpes, pero SFX (dead) puede seguir sonando
        if (nombre == "gameover")
        {
            if (fuenteMusica) fuenteMusica.Stop();
            if (fuentePasos) fuentePasos.Stop();
            if (fuenteGolpe) fuenteGolpe.Stop();
            return;
        }

        // 2) Resto de escenas: música en bucle SIEMPRE
        ReproducirMusica();

        // 3) Fuera de "juego": aseguro que no suenen pasos ni cosas raras
        if (nombre != "juego")
        {
            if (fuentePasos) fuentePasos.Stop();
            if (fuenteGolpe) fuenteGolpe.Stop();
        }
    }

    public void ReproducirMusica()
    {
        if (!fuenteMusica || !mainTheme) return;

        fuenteMusica.clip = mainTheme;
        fuenteMusica.loop = true;

        if (!fuenteMusica.isPlaying)
            fuenteMusica.Play();
    }

    // SOLO para la escena "juego"
    public void SetPasos(bool andando)
    {
        if (SceneManager.GetActiveScene().name.ToLower().Trim() != "juego") return;
        if (!fuentePasos || !walking) return;

        if (!andando)
        {
            if (fuentePasos.isPlaying) fuentePasos.Stop();
            return;
        }

        fuentePasos.clip = walking;
        fuentePasos.loop = true;

        if (!fuentePasos.isPlaying)
            fuentePasos.Play();
    }

    // SOLO para la escena "juego"
    public void ReproducirGolpe()
    {
        if (SceneManager.GetActiveScene().name.ToLower().Trim() != "juego") return;
        if (!fuenteGolpe || !punch) return;

        // Esto hace que el golpe nuevo "pise" al anterior (no se acumula)
        fuenteGolpe.Stop();
        fuenteGolpe.clip = punch;
        fuenteGolpe.loop = false;
        fuenteGolpe.Play();
    }

    // SOLO para la escena "juego"
    public void ReproducirPuerta()
    {
        if (SceneManager.GetActiveScene().name.ToLower().Trim() != "juego") return;
        if (!fuenteSFX || !gate) return;

        fuenteSFX.PlayOneShot(gate, 1f);
    }

    // Se puede llamar desde "juego" y debe seguir sonando aunque cargues gameover
    public void ReproducirDeadUnaVez()
    {
        if (yaSonoDead) return;
        yaSonoDead = true;

        // corto música y pasos para que destaque la muerte
        if (fuenteMusica) fuenteMusica.Stop();
        if (fuentePasos) fuentePasos.Stop();
        if (fuenteGolpe) fuenteGolpe.Stop();

        if (fuenteSFX && dead)
            fuenteSFX.PlayOneShot(dead, 1f);
    }

    // GOLPE DEL ENEMIGO AL JUGADOR (solo en Juego)
    public void ReproducirGolpeEnemigo()
    {
        if (UnityEngine.SceneManagement.SceneManager
            .GetActiveScene().name.ToLower().Trim() != "juego")
            return;

        if (!fuenteSFX || !punchEnemy) return;

        // No se acumula infinito, pero puede solaparse ligeramente
        fuenteSFX.PlayOneShot(punchEnemy, 1f);
    }

    // SONIDO DE UI (botones, ESC, etc.)
    public void ReproducirUIClick()
    {
        if (!fuenteSFX || !uiClick) return;
        fuenteSFX.PlayOneShot(uiClick, 1f);
    }


}
