using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoginRegistroUI : MonoBehaviour
{
    [Header("Servicios")]
    public GameSaveServicio gameSave;

    [Header("Inputs")]
    public TMP_InputField inpCorreo;
    public TMP_InputField inpPass;
    public TMP_InputField inpNombre;       // solo en modo Registro

    [Header("UI")]
    public TMP_Text txtError;
    public GameObject panelRegistro;       // contenedor del InputNombre

    bool modoRegistro = false;


    void Start()
    {
        if (txtError) txtError.text = "";
        if (panelRegistro) panelRegistro.SetActive(modoRegistro);
    }

    // Conectar al botón "Cambiar a Registro / Cambiar a Login"
    public TMP_Text btnCambiarModoTexto; // añade este campo público
    public void ToggleModo()
    {
        modoRegistro = !modoRegistro;
        if (panelRegistro) panelRegistro.SetActive(modoRegistro);
        if (txtError) txtError.text = "";
        if (btnCambiarModoTexto) btnCambiarModoTexto.text = modoRegistro ? "Ir a Login" : "Ir a Registro";
        Debug.Log("[LoginRegistroUI] Toggle => modoRegistro=" + modoRegistro);
    }


    // Conectar al botón "Aceptar"
    public async void OnClickAceptar()
    {
        void Awake()
        {
            // Si no está asignado por Inspector, lo buscamos en escena (el persistente de BootServicios)
            if (!gameSave) gameSave = FindObjectOfType<GameSaveServicio>();
        }

        // Por si acaso, reintenta enganchar antes de usarlo
        if (!gameSave) gameSave = FindObjectOfType<GameSaveServicio>();
        if (!gameSave) { if (txtError) txtError.text = "Falta GameSaveServicio"; return; }

        Debug.Log($"[LoginRegistroUI] modoRegistro={modoRegistro} correo='{inpCorreo.text}' pass='{inpPass.text}' nombre='{(inpNombre ? inpNombre.text : "")}'");

        if (!gameSave) { if (txtError) txtError.text = "Falta GameSaveServicio"; return; }

        var correo = inpCorreo ? inpCorreo.text.Trim() : "";
        var pass = inpPass ? inpPass.text.Trim() : "";
        var nombre = inpNombre ? inpNombre.text.Trim() : "";

        if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(pass))
        {
            if (txtError) txtError.text = "Rellena correo y contraseña.";
            return;
        }

        if (txtError) txtError.text = "Procesando...";

        try
        {
            if (modoRegistro)
                await gameSave.RegistroAsync(correo, pass, nombre);
            else
                await gameSave.LoginAsync(correo, pass);

            SceneManager.LoadScene("MenuPrincipal");
        }
        catch (System.Exception ex)
        {
            if (txtError) txtError.text = "Error: " + ex.Message;
        }
    }
}
