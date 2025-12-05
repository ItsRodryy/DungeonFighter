using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Login / Registro
public class LoginRegistroUI : MonoBehaviour
{
    [Header("Servicios")]
    // Servicio para gestionar el login y el registro
    public GameSaveServicio gameSave;

    [Header("Inputs")]
    // Campos de entrada
    public TMP_InputField inpCorreo;
    public TMP_InputField inpPass;
    public TMP_InputField inpNombre;

    [Header("UI")]
    public TMP_Text txtError;
    // Panel adicional Registro
    public GameObject panelRegistro;
    // Texto del botón para cambiar de login a registro
    public TMP_Text btnCambiarModoTexto;

    [Header("Registro como Admin")]
    public Toggle chkAdmin;

    // False = Login, True = Registro
    private bool modoRegistro = false;

    private void Awake()
    {
        // Si no se ha asignado el servicio en el inspector, lo buscamos en escena
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
    }


    // Botón Cambiar a Registro o Cambiar a Login
    public void ToggleModo()
    {
        // Cambiamos el modo y actualizamos la UI
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
        var correo = inpCorreo ? inpCorreo.text.Trim() : "";
        var pass = inpPass ? inpPass.text.Trim() : "";
        var nombre = inpNombre ? inpNombre.text.Trim() : "";
        var quiereAdmin = chkAdmin && chkAdmin.isOn;

        // Validación para Login y Registro
        if (string.IsNullOrEmpty(correo) || string.IsNullOrEmpty(pass))
        {
            if (txtError) txtError.text = "Rellena el correo y la contraseña.";
            return;
        }

        if (txtError) txtError.text = "Procesando...";

        try
        {
            if (modoRegistro)
            {
                // Modo Registro, comprobamos si quiere registrarse como admin
                bool esAdmin = false;

                if (quiereAdmin)
                {
                    // Contraseña de admin para el que se quiera registrar como admin
                    if (pass == "admin123456")
                        esAdmin = true;
                    else
                    {
                        if (txtError) txtError.text = "Ingresa la contraseña de admin :(";
                        return;
                    }
                }

                // Registro de Usuario nuevo
                await gameSave.RegistroAsync(correo, pass, nombre, esAdmin);
            }
            else
            {
                // Modo Login
                await gameSave.LoginAsync(correo, pass);
            }

            // Si va bien todo, pasamos al menú principal
            SceneManager.LoadScene("MenuPrincipal");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Error en login/registro: " + ex);

            if (txtError)
                txtError.text = "Error con las credenciales...";
        }

    }
}
