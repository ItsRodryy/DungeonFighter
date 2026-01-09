using UnityEngine;
using UnityEngine.UI;

public class VolumenUI : MonoBehaviour
{
    [Header("UI")]
    public Slider sliderVolumen;

    [Header("Ajustes")]
    [Range(0f, 1f)] public float volumenPorDefecto = 0.8f;
    const string KEY = "VOLUMEN_MASTER";

    void Start()
    {
        // Cargamos el volumen guardado o usamos el valor por defecto si no existe
        float v = PlayerPrefs.GetFloat(KEY, volumenPorDefecto);

        // Aplicamos el volumen al AudioListener para que afecte a todo el juego
        SetVolumen(v);

        // Sincronizamos el slider con el valor cargado sin disparar el evento de cambio
        if (sliderVolumen)
        {
            sliderVolumen.SetValueWithoutNotify(v);

            // Nos suscribimos al evento para que al mover el slider actualicemos el volumen
            sliderVolumen.onValueChanged.AddListener(SetVolumen);
        }
    }

    public void SetVolumen(float v)
    {
        // Limitamos el valor entre 0 y 1 para evitar valores fuera de rango
        v = Mathf.Clamp01(v);

        // Cambiamos el volumen global de la aplicación
        AudioListener.volume = v;

        // Guardamos el volumen para mantenerlo entre sesiones
        PlayerPrefs.SetFloat(KEY, v);
        PlayerPrefs.Save();
    }
}
