using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Login / Registro
public class LoginRegistroUI : MonoBehaviour
{
    [Header("Servicios")]
    public GameSaveServicio gameSave;

    [Header("Inputs")]
    public TMP_InputField inpCorreo;
    public TMP_InputField inpPass;
    public TMP_InputField inpNombre;

    [Header("UI")]
    public TMP_Text txtError;
    public GameObject panelRegistro;
    public TMP_Text btnCambiarModoTexto;

    [Header("Registro como Admin")]
    public Toggle chkAdmin;

    // False = Login, True = Registro
    private bool modoRegistro = false;

    private void Awake()
    {
        if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
    }

    private void Start()
    {
        if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();

        // Si ya hay sesión guardada, saltamos el login
        if (gameSave != null &&
            !string.IsNullOrEmpty(gameSave.Uid) &&
            !string.IsNullOrEmpty(gameSave.IdToken))
        {
            SceneManager.LoadScene("MenuPrincipal");
            return;
        }

        if (txtError) txtError.text = "";
        if (panelRegistro) panelRegistro.SetActive(modoRegistro);
        if (chkAdmin) chkAdmin.gameObject.SetActive(modoRegistro);

        if (btnCambiarModoTexto)
            btnCambiarModoTexto.text = modoRegistro ? "Ir a Login" : "Ir a Registro";
    }

    // Botón Cambiar a Registro o Cambiar a Login
    public void ToggleModo()
    {
        modoRegistro = !modoRegistro;

        if (panelRegistro) panelRegistro.SetActive(modoRegistro);
        if (chkAdmin) chkAdmin.gameObject.SetActive(modoRegistro);
        if (txtError) txtError.text = "";

        if (btnCambiarModoTexto)
            btnCambiarModoTexto.text = modoRegistro ? "Ir a Login" : "Ir a Registro";
    }

    // Botón Aceptar
    public async void OnClickAceptar()
    {
        // Aseguramos el servicio disponible
        if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
        if (!gameSave)
        {
            if (txtError) txtError.text = "Falta GameSaveServicio";
            return;
        }

        // Leemos los campos de la UI
        string correo = inpCorreo ? inpCorreo.text.Trim() : "";
        string pass = inpPass ? inpPass.text : "";           // NO Trim aquí por si hay espacios intencionados
        string nombre = inpNombre ? inpNombre.text.Trim() : "";
        bool quiereAdmin = chkAdmin && chkAdmin.isOn;

        // ✅ Validaciones ANTES de llamar a Firebase
        if (string.IsNullOrWhiteSpace(correo))
        {
            if (txtError) txtError.text = "Escribe el correo.";
            return;
        }

        if (!EsCorreoValido(correo))
        {
            if (txtError) txtError.text = "Correo inválido (formato).";
            return;
        }

        if (string.IsNullOrWhiteSpace(pass))
        {
            if (txtError) txtError.text = "Escribe la contraseña.";
            return;
        }

        if (modoRegistro && string.IsNullOrWhiteSpace(nombre))
        {
            if (txtError) txtError.text = "Escribe un nombre de usuario.";
            return;
        }

        if (txtError) txtError.text = "Procesando...";

        try
        {
            if (modoRegistro)
            {
                // Registro (Admin opcional)
                bool esAdmin = false;

                if (quiereAdmin)
                {
                    // Tu lógica actual: usando pass como "clave admin"
                    if (pass == "admin123456")
                        esAdmin = true;
                    else
                    {
                        if (txtError) txtError.text = "Ingresa la contraseña de admin :(";
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
            Debug.LogError("Error en login/registro: " + ex);

            if (txtError)
                txtError.text = TraducirErrorFirebase(ex.Message);
        }
    }

    // Validación simple de formato de correo
    bool EsCorreoValido(string correo)
    {
        return Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    // Traduce errores típicos de Firebase a mensajes útiles
    string TraducirErrorFirebase(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return "Error con las credenciales.";

        string m = msg.ToUpperInvariant();

        if (m.Contains("INVALID_EMAIL")) return "Correo inválido.";
        if (m.Contains("MISSING_PASSWORD")) return "Falta la contraseña.";
        if (m.Contains("WEAK_PASSWORD")) return "Contraseña demasiado débil.";
        if (m.Contains("EMAIL_NOT_FOUND")) return "Ese usuario no existe.";
        if (m.Contains("INVALID_PASSWORD") || m.Contains("INVALID_LOGIN_CREDENTIALS")) return "Contraseña incorrecta.";
        if (m.Contains("EMAIL_EXISTS")) return "Ese correo ya está registrado.";

        return "Error con las credenciales.";
    }
}
