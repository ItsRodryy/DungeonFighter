using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuPrincipalUI : MonoBehaviour
{
    [Header("Servicios")]
    public GameSaveServicio gameSave;

    [Header("Comunes")]
    public TMP_Text txtBienvenida; // opcional

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

    async void Start()
    {
        if (!gameSave)
            gameSave = FindObjectOfType<GameSaveServicio>();

        if (gameSave == null || string.IsNullOrEmpty(gameSave.Uid) || string.IsNullOrEmpty(gameSave.IdToken))
        {
            Debug.LogWarning("No hay sesión cargada. Volviendo a LoginRegistro.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("LoginRegistro");
            return;
        }

        if (!gameSave)
        {
            Debug.LogError("MenuPrincipalUI: falta referencia a GameSaveServicio.");
            return;
        }

        // 1) Perfil y saludo
        FirestoreCliente.UsuarioPerfil perfil = null;
        try
        {
            perfil = await gameSave.firestore.GetUsuarioPerfilAsync(gameSave.IdToken, gameSave.Uid);
            if (txtBienvenida && perfil != null)
                txtBienvenida.text = $"¡Bienvenido, {perfil.nombreUsuario}!";
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("No se pudo cargar el perfil: " + e.Message);
        }

        bool esAdmin = perfil != null && perfil.esAdmin;

        // 2) Mostrar panel que toque
        if (panelAdmin) panelAdmin.SetActive(esAdmin);
        if (panelUsuario) panelUsuario.SetActive(!esAdmin);

        // 3) Botones - Usuario
        if (btnNuevaPartida)
            btnNuevaPartida.onClick.AddListener(() => SceneManager.LoadScene("Juego"));

        if (btnCargarPartida)
            btnCargarPartida.onClick.AddListener(async () =>
            {
                try
                {
                    var p = await gameSave.CargarAsync();
                    // TODO: aplica 'p' a tu GameManager si hace falta
                    SceneManager.LoadScene(p.datosJugador.nombreEscena);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning("No se pudo cargar la partida: " + ex.Message);
                }
            });

        if (btnCerrarSesion)
            btnCerrarSesion.onClick.AddListener(() =>
            {
                gameSave.SignOutLocal();
                SceneManager.LoadScene("LoginRegistro");
            });

        // 4) Botones - Admin
        if (btnListarPartidas)
            btnListarPartidas.onClick.AddListener(async () =>
            {
                if (txtListado) txtListado.text = "Cargando...";
                try
                {
                    var lista = await gameSave.firestore.ListarPartidasAsync(gameSave.IdToken, 100);
                    if (txtListado)
                        txtListado.text = string.Join("\n", lista.Select(e =>
                            $"{e.uid} | {e.partida.nombrePartida} | {e.partida.datosJugador.nombreEscena}"));
                }
                catch (System.Exception ex)
                {
                    if (txtListado) txtListado.text = "Error: " + ex.Message;
                }
            });

        if (btnEliminarUID)
            btnEliminarUID.onClick.AddListener(async () =>
            {
                var uid = inputUid ? inputUid.text.Trim() : "";
                if (string.IsNullOrEmpty(uid)) { if (txtListado) txtListado.text = "UID vacío"; return; }

                try
                {
                    await gameSave.firestore.EliminarPartidaAsync(gameSave.IdToken, uid);
                    if (txtListado) txtListado.text = "Eliminado. Pulsa 'Listar' para refrescar.";
                }
                catch (System.Exception ex)
                {
                    if (txtListado) txtListado.text = "Error eliminando: " + ex.Message;
                }
            });

        if (btnCerrarSesionAdmin)
            btnCerrarSesionAdmin.onClick.AddListener(() =>
            {
                gameSave.SignOutLocal();
                SceneManager.LoadScene("LoginRegistro");
            });
    }
}
