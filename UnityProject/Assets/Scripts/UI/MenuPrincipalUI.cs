using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controlamos el menú principal, Usuario y Admin
public class MenuPrincipalUI : MonoBehaviour
{
    [Header("Servicios")]
    public GameSaveServicio gameSave;

    [Header("Comunes")]
    public TMP_Text txtBienvenida;

    [Header("Panel Usuario")]
    public GameObject panelUsuario;
    public Button btnNuevaPartida;
    public Button btnCargarPartida;
    public Button btnCerrarSesion;

    [Header("Panel Admin")]
    public GameObject panelAdmin;
    public Button btnListarPartidas;
    public TMP_InputField inputUid;
    public Button btnEliminarUID;
    public TMP_Text txtListado;
    public Button btnCerrarSesionAdmin;

    // Start es async porque para poder hablar con Firestore
    private async void Start()
    {
        try
        {
            // Si no se ha asignado en el inspector, lo buscamos en la escena
            if (!gameSave) gameSave = Object.FindFirstObjectByType<GameSaveServicio>();

            // Si no hay sesión, volvemos a Login/Registro
            if (gameSave == null || string.IsNullOrEmpty(gameSave.Uid) || string.IsNullOrEmpty(gameSave.IdToken))
            {
                SceneManager.LoadScene("LoginRegistro");
                return;
            }

            // Usuario
            FirestoreCliente.UsuarioPerfil perfil = null;
            try
            {
                perfil = await gameSave.firestore.GetUsuarioPerfilAsync(gameSave.IdToken, gameSave.Uid);
                if (txtBienvenida && perfil != null)
                    txtBienvenida.text = $"¡Qué dise er máquina, {perfil.nombreUsuario}!";
            }
            catch
            {
                // Si falla Firestore, no muestro el nombre
            }

            bool esAdmin = perfil != null && perfil.esAdmin;

            // Panel Admin o Usuario según el rol
            if (panelAdmin) panelAdmin.SetActive(esAdmin);
            if (panelUsuario) panelUsuario.SetActive(!esAdmin);

            // Botones Usuario

            if (btnNuevaPartida)
                btnNuevaPartida.onClick.AddListener(() =>
                    SceneManager.LoadScene("Juego")
                );

            if (btnCargarPartida)
                btnCargarPartida.onClick.AddListener(async () =>
                {
                    if (txtListado) txtListado.text = "Cargando...";
                    try
                    {
                        var p = await gameSave.CargarAsync();
                        SceneManager.LoadScene(p.datosJugador.nombreEscena);
                    }
                    catch (System.Exception ex)
                    {
                        if (txtListado) txtListado.text = "Error cargar: " + ex.Message;
                    }
                });

            if (btnCerrarSesion)
                btnCerrarSesion.onClick.AddListener(() =>
                {
                    gameSave.SignOutLocal();
                    SceneManager.LoadScene("LoginRegistro");
                });

            // Botones Admin

            // Listar Partidas (Hasta 5)
            if (btnListarPartidas)
                btnListarPartidas.onClick.AddListener(async () =>
                {
                    if (txtListado) txtListado.text = "Cargando...";
                    try
                    {
                        var lista = await gameSave.firestore.ListarPartidasAsync(gameSave.IdToken, 5);
                        if (txtListado)
                            txtListado.text = string.Join("\n", lista.Select(e =>
                                $"{e.uid} | {e.partida.nombrePartida} | {e.partida.datosJugador.nombreEscena}"
                            ));
                    }
                    catch (System.Exception ex)
                    {
                        if (txtListado) txtListado.text = "Error: " + ex.Message;
                    }
                });

            // Eliminamos partida por UID escrito ese panel
            if (btnEliminarUID)
                btnEliminarUID.onClick.AddListener(async () =>
                {
                    var uid = inputUid ? inputUid.text.Trim() : "";
                    if (string.IsNullOrEmpty(uid))
                    {
                        if (txtListado) txtListado.text = "UID Vacío";
                        return;
                    }
                    try
                    {
                        await gameSave.firestore.EliminarPartidaAsync(gameSave.IdToken, uid);
                        if (txtListado) txtListado.text = "Eliminado. Pulsa 'Listar' para actualizar.";
                    }
                    catch (System.Exception ex)
                    {
                        if (txtListado) txtListado.text = "Error Eliminando: " + ex.Message;
                    }
                });

            if (btnCerrarSesionAdmin)
                btnCerrarSesionAdmin.onClick.AddListener(() =>
                {
                    gameSave.SignOutLocal();
                    SceneManager.LoadScene("LoginRegistro");
                });
        }
        catch (System.Exception ex)
        {
            // Última red de seguridad para no partir el juego si algo falla en Start
            Debug.LogError("MenuPrincipalUI.Start fallo: " + ex);
            if (txtListado) txtListado.text = "Error en Menú: " + ex.Message;
        }
    }
}
