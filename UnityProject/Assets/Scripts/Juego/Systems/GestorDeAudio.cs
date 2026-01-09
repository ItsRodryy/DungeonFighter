using UnityEngine;
using UnityEngine.SceneManagement;

// Gestionamos música pasos y efectos y mantenemos el audio entre escenas
public class GestorDeAudio : MonoBehaviour
{
    public static GestorDeAudio I;

    [Header("Fuentes (AudioSource)")]
    public AudioSource fuenteMusica; // hijo Musica
    public AudioSource fuentePasos;  // hijo Pasos
    public AudioSource fuenteSFX;    // hijo SFX
    public AudioSource fuenteGolpe;  // hijo Golpe

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
        // Nos aseguramos de que solo exista una instancia
        if (I != null)
        {
            Destroy(gameObject);
            return;
        }

        I = this;

        // Marcamos el objeto para que no se destruya al cambiar de escena
        DontDestroyOnLoad(gameObject);

        // Nos suscribimos al evento de escena cargada para ajustar audio según escena
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    void OnDestroy()
    {
        // Nos desuscribimos para evitar referencias colgando
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        string nombre = escena.name.ToLower().Trim();

        // Si entramos en GameOver paramos música pasos y golpes para dejarlo limpio
        if (nombre == "gameover")
        {
            if (fuenteMusica) fuenteMusica.Stop();
            if (fuentePasos) fuentePasos.Stop();
            if (fuenteGolpe) fuenteGolpe.Stop();
            return;
        }

        // En el resto de escenas ponemos música en bucle
        ReproducirMusica();

        // Fuera de Juego paramos pasos y golpes para que no suenen cosas raras
        if (nombre != "juego")
        {
            if (fuentePasos) fuentePasos.Stop();
            if (fuenteGolpe) fuenteGolpe.Stop();
        }
    }

    public void ReproducirMusica()
    {
        // Si no tenemos fuente o clip no hacemos nada
        if (!fuenteMusica || !mainTheme) return;

        // Configuramos clip y bucle
        fuenteMusica.clip = mainTheme;
        fuenteMusica.loop = true;

        // Solo le damos play si no estaba sonando ya
        if (!fuenteMusica.isPlaying)
            fuenteMusica.Play();
    }

    public void SetPasos(bool andando)
    {
        // Solo gestionamos pasos dentro de la escena Juego
        if (SceneManager.GetActiveScene().name.ToLower().Trim() != "juego") return;
        if (!fuentePasos || !walking) return;

        // Si paramos dejamos de reproducir la fuente
        if (!andando)
        {
            if (fuentePasos.isPlaying) fuentePasos.Stop();
            return;
        }

        // Configuramos clip y bucle para pasos
        fuentePasos.clip = walking;
        fuentePasos.loop = true;

        // Solo iniciamos si no está sonando
        if (!fuentePasos.isPlaying)
            fuentePasos.Play();
    }

    public void ReproducirGolpe()
    {
        // Solo reproducimos golpe dentro de la escena Juego
        if (SceneManager.GetActiveScene().name.ToLower().Trim() != "juego") return;
        if (!fuenteGolpe || !punch) return;

        // Paramos para que el golpe nuevo pise al anterior
        fuenteGolpe.Stop();
        fuenteGolpe.clip = punch;
        fuenteGolpe.loop = false;
        fuenteGolpe.Play();
    }

    public void ReproducirPuerta()
    {
        // Solo reproducimos puerta dentro de la escena Juego
        if (SceneManager.GetActiveScene().name.ToLower().Trim() != "juego") return;
        if (!fuenteSFX || !gate) return;

        // Usamos PlayOneShot para no cambiar el clip base de la fuente
        fuenteSFX.PlayOneShot(gate, 1f);
    }

    public void ReproducirDeadUnaVez()
    {
        // Evitamos repetir el sonido de muerte
        if (yaSonoDead) return;
        yaSonoDead = true;

        // Paramos música pasos y golpes para que destaque la muerte
        if (fuenteMusica) fuenteMusica.Stop();
        if (fuentePasos) fuentePasos.Stop();
        if (fuenteGolpe) fuenteGolpe.Stop();

        // Lanzamos el sonido de muerte por SFX para que pueda seguir aunque cambie la escena
        if (fuenteSFX && dead)
            fuenteSFX.PlayOneShot(dead, 1f);
    }

    public void ReproducirGolpeEnemigo()
    {
        // Solo reproducimos golpe enemigo dentro de la escena Juego
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name.ToLower().Trim() != "juego")
            return;

        if (!fuenteSFX || !punchEnemy) return;

        // Permitimos un poco de solape usando PlayOneShot
        fuenteSFX.PlayOneShot(punchEnemy, 1f);
    }

    public void ReproducirUIClick()
    {
        // Reproducimos sonido de UI para botones y acciones de menú
        if (!fuenteSFX || !uiClick) return;
        fuenteSFX.PlayOneShot(uiClick, 1f);
    }
}
