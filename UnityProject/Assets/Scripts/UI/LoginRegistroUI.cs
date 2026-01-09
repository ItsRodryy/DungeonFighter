using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controlamos la pantalla de Login y Registro
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

    // Usamos false para Login y true para Registro
    private bool modoRegistro = false;

    private void Awake()
    {
        // Si no lo hemos asignado en el inspector lo buscamos en la escena
        if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
    }

    private void Start()
    {
        // Repetimos búsqueda por seguridad si entramos con referencias sin asignar
        if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();

        // Si ya tenemos sesión guardada saltamos esta escena y vamos directo al menú
        if (gameSave != null &&
            !string.IsNullOrEmpty(gameSave.Uid) &&
            !string.IsNullOrEmpty(gameSave.IdToken))
        {
            SceneManager.LoadScene("MenuPrincipal");
            return;
        }

        // Dejamos la UI limpia y configuramos el modo actual
        if (txtError) txtError.text = "";
        if (panelRegistro) panelRegistro.SetActive(modoRegistro);
        if (chkAdmin) chkAdmin.gameObject.SetActive(modoRegistro);

        // Ajustamos el texto del botón para alternar entre Login y Registro
        if (btnCambiarModoTexto)
            btnCambiarModoTexto.text = modoRegistro ? "Ir a Login" : "Ir a Registro";
    }

    // Alternamos el modo de la pantalla entre Login y Registro
    public void ToggleModo()
    {
        // Cambiamos el booleano para alternar el modo
        modoRegistro = !modoRegistro;

        // Mostramos u ocultamos el panel extra de registro
        if (panelRegistro) panelRegistro.SetActive(modoRegistro);

        // Mostramos u ocultamos el toggle de admin según el modo
        if (chkAdmin) chkAdmin.gameObject.SetActive(modoRegistro);

        // Limpiamos mensajes de error al cambiar de modo
        if (txtError) txtError.text = "";

        // Cambiamos el texto del botón de alternar
        if (btnCambiarModoTexto)
            btnCambiarModoTexto.text = modoRegistro ? "Ir a Login" : "Ir a Registro";
    }

    // Ejecutamos Login o Registro al pulsar el botón Aceptar
    public async void OnClickAceptar()
    {
        // Aseguramos que el servicio existe antes de seguir
        if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();
        if (!gameSave)
        {
            if (txtError) txtError.text = "Falta GameSaveServicio";
            return;
        }

        // Leemos campos de la UI y los normalizamos
        string correo = inpCorreo ? inpCorreo.text.Trim() : "";
        string pass = inpPass ? inpPass.text : "";           // No recortamos espacios por si el usuario los mete a propósito
        string nombre = inpNombre ? inpNombre.text.Trim() : "";
        bool quiereAdmin = chkAdmin && chkAdmin.isOn;

        // Validamos antes de llamar a Firebase para evitar peticiones innecesarias
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

        // En modo registro exigimos nombre de usuario
        if (modoRegistro && string.IsNullOrWhiteSpace(nombre))
        {
            if (txtError) txtError.text = "Escribe un nombre de usuario.";
            return;
        }

        // Mostramos un estado mientras procesamos
        if (txtError) txtError.text = "Procesando...";

        try
        {
            if (modoRegistro)
            {
                // Registramos usuario y marcamos rol admin si se cumple la condición
                bool esAdmin = false;

                if (quiereAdmin)
                {
                    // Aplicamos la lógica actual para activar admin usando la contraseña como clave
                    if (pass == "admin123456")
                        esAdmin = true;
                    else
                    {
                        if (txtError) txtError.text = "Ingresa la contraseña de admin :(";
                        return;
                    }
                }

                // Creamos cuenta y guardamos perfil en Firestore con el rol
                await gameSave.RegistroAsync(correo, pass, nombre, esAdmin);
            }
            else
            {
                // Iniciamos sesión y guardamos token y uid en local
                await gameSave.LoginAsync(correo, pass);
            }

            // Si todo va bien entramos al menú principal
            SceneManager.LoadScene("MenuPrincipal");
        }
        catch (System.Exception ex)
        {
            // Registramos el error en consola para depurar
            Debug.LogError("Error en login/registro: " + ex);

            // Mostramos un mensaje entendible al usuario
            if (txtError)
                txtError.text = TraducirErrorFirebase(ex.Message);
        }
    }

    // Validamos formato básico de correo con una expresión regular simple
    bool EsCorreoValido(string correo)
    {
        return Regex.IsMatch(correo, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    // Traducimos errores típicos de Firebase a mensajes más claros
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
