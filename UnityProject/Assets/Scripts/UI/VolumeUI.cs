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
        // Cargar volumen guardado
        float v = PlayerPrefs.GetFloat(KEY, volumenPorDefecto);
        SetVolumen(v);

        // Pintar slider sin disparar eventos raros
        if (sliderVolumen)
        {
            sliderVolumen.SetValueWithoutNotify(v);
            sliderVolumen.onValueChanged.AddListener(SetVolumen);
        }
    }

    public void SetVolumen(float v)
    {
        v = Mathf.Clamp01(v);
        AudioListener.volume = v;
        PlayerPrefs.SetFloat(KEY, v);
        PlayerPrefs.Save();
    }
}
