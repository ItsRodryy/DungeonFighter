using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginRegistroUI : MonoBehaviour
{
    [Header("Servicios")]
    public GameSaveServicio gameSave;

    [Header("Inputs")]
    public TMP_InputField inpCorreo;
    public TMP_InputField inpPass;
    public TMP_InputField inpNombre;   // solo en modo Registro

    [Header("UI")]
    public TMP_Text txtError;
    public GameObject panelRegistro;   // contenedor del InputNombre
    public TMP_Text btnCambiarModoTexto;

    [Header("Registro como Admin")]
    public Toggle chkAdmin;            // casilla “Crear admin”

    private bool modoRegistro = false;

    private void Awake()
    {
        if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
    }

    private void Start()
    {
        if (txtError) txtError.text = "";
        if (panelRegistro) panelRegistro.SetActive(modoRegistro);
        if (chkAdmin) chkAdmin.gameObject.SetActive(modoRegistro); // mostrar solo en registro (opcional)
    }

    // Botón "Cambiar a Registro / Cambiar a Login"
    public void ToggleModo()
    {
        modoRegistro = !modoRegistro;
        if (panelRegistro) panelRegistro.SetActive(modoRegistro);
        if (chkAdmin) chkAdmin.gameObject.SetActive(modoRegistro);
        if (txtError) txtError.text = "";
        if (btnCambiarModoTexto) btnCambiarModoTexto.text = modoRegistro ? "Ir a Login" : "Ir a Registro";
    }

    // Botón "Aceptar"
    public async void OnClickAceptar()
    {
        if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
        if (!gameSave) { if (txtError) txtError.text = "Falta GameSaveServicio"; return; }

        var correo = inpCorreo ? inpCorreo.text.Trim() : "";
        var pass = inpPass ? inpPass.text.Trim() : "";
        var nombre = inpNombre ? inpNombre.text.Trim() : "";
        var quiereAdmin = chkAdmin && chkAdmin.isOn;

        if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(pass))
        {
            if (txtError) txtError.text = "Rellena correo y contraseña.";
            return;
        }

        if (txtError) txtError.text = "Procesando...";

        try
        {
            if (modoRegistro)
            {
                bool esAdmin = false;
                if (quiereAdmin)
                {
                    if (pass == "admin123456")
                        esAdmin = true;
                    else
                    {
                        if (txtError) txtError.text = "Para crear un ADMIN usa la contraseña exacta: admin123456";
                        return;
                    }
                }

                await gameSave.RegistroAsync(correo, pass, nombre, esAdmin);
            }
            else
            {
                await gameSave.LoginAsync(correo, pass);
            }

            SceneManager.LoadScene("MenuPrincipal");
        }
        catch (System.Exception ex)
        {
            // Mensaje para el usuario
            string msg = ex is FirebaseAuthException fae
                ? (fae.FirebaseMessage == "INVALID_LOGIN_CREDENTIALS"
                       ? "Correo o contraseña incorrectos."
                       : $"Error de autenticación: {fae.FirebaseMessage}")
                : ("Error: " + ex.Message);

            if (txtError) txtError.text = msg;
            // NO cambiamos de escena, no salimos del juego. Solo mostramos el error.
        }

    }
}
