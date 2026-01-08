using System;
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
    public Button btnListarPartidas;   // lo estás usando como "Listar Usuarios/Partida"
    public TMP_InputField inputUid;
    public Button btnEliminarUID;
    public TMP_Text txtListado;
    public Button btnCerrarSesionAdmin;

    private async void Start()
    {
        try
        {
            // Si no se ha asignado en el inspector, lo buscamos en la escena
            if (!gameSave) gameSave = UnityEngine.Object.FindFirstObjectByType<GameSaveServicio>();

            // Si no hay sesión, volvemos a Login/Registro
            if (gameSave == null || string.IsNullOrEmpty(gameSave.Uid) || string.IsNullOrEmpty(gameSave.IdToken))
            {
                SceneManager.LoadScene("LoginRegistro");
                return;
            }

            // Cargar perfil para saber si es admin y poner bienvenida
            FirestoreCliente.UsuarioPerfil perfil = null;
            try
            {
                perfil = await gameSave.firestore.GetUsuarioPerfilAsync(gameSave.IdToken, gameSave.Uid);
            }
            catch { /* si falla, seguimos */ }

            if (txtBienvenida && perfil != null)
                txtBienvenida.text = $"¡Qué dise er máquina, {perfil.nombreUsuario}!";

            bool esAdmin = perfil != null && perfil.esAdmin;

            // Panel Admin o Usuario según rol
            if (panelAdmin) panelAdmin.SetActive(esAdmin);
            if (panelUsuario) panelUsuario.SetActive(!esAdmin);

            // ===== BOTONES USUARIO =====

            if (btnNuevaPartida)
                btnNuevaPartida.onClick.AddListener(async () =>
                {
                    if (txtListado) txtListado.text = "Creando nueva partida...";

                    try
                    {
                        // 1) BORRAR partida anterior (si existe)
                        await gameSave.firestore.EliminarPartidaAsync(gameSave.IdToken, gameSave.Uid);
                    }
                    catch
                    {
                        // Si no existe, da igual (404)
                    }

                    // 2) Entrar al juego
                    SceneManager.LoadScene("Juego");
                });


            if (btnCargarPartida)
                btnCargarPartida.onClick.AddListener(async () =>
                {
                    if (txtListado) txtListado.text = "Cargando...";
                    try
                    {
                        var p = await gameSave.CargarAsync();
                        SceneManager.LoadScene(p.datosJugador.nombreEscena);
                    }
                    catch (Exception ex)
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

            // ===== BOTONES ADMIN =====

            // UN SOLO BOTÓN:
            // - Si inputUid está vacío => lista usuarios (UID + email + nombre)
            // - Si inputUid tiene UID => muestra la partida de ese UID
            if (btnListarPartidas)
                btnListarPartidas.onClick.AddListener(async () =>
                {
                    try
                    {
                        // 1) Leer UID
                        var uid = inputUid ? inputUid.text.Trim() : "";

                        // 2) Si el texto es el placeholder o está vacío -> lo tratamos como vacío
                        var uidLower = uid.ToLower();
                        if (string.IsNullOrWhiteSpace(uid) || uidLower.Contains("introduce") || uidLower.Contains("enter"))
                            uid = "";

                        // 3) Si hay UID -> mostrar partida de ese UID
                        if (!string.IsNullOrEmpty(uid))
                        {
                            if (txtListado) txtListado.text = "Cargando partida del UID...";

                            var p = await gameSave.firestore.CargarPartidaAsync(gameSave.IdToken, uid);

                            if (txtListado)
                            {
                                txtListado.text =
                                    $"UID: {uid}\n" +
                                    $"NombrePartida: {p.nombrePartida}\n" +
                                    $"Escena: {p.datosJugador.nombreEscena}\n" +
                                    $"Pos: ({p.datosJugador.posX:0.00}, {p.datosJugador.posY:0.00})\n" +
                                    $"Vida: {p.datosJugador.vida}/{p.datosJugador.vidaMaxima}\n" +
                                    $"Inv -> Monedas: {p.datosInventario.monedas}, Llaves: {p.datosInventario.llaves}, Pociones: {p.datosInventario.pociones}";
                            }
                            return;
                        }

                        // 4) Si NO hay UID -> listar usuarios
                        if (txtListado) txtListado.text = "Listando usuarios...";
                        var listaUsuarios = await gameSave.firestore.ListarUsuariosAsync(gameSave.IdToken, 200);

                        if (txtListado)
                            txtListado.text = string.Join("\n", listaUsuarios
                                .Select(u => u.perfil.nombreUsuario)
                                .Where(n => !string.IsNullOrWhiteSpace(n))
                                .Distinct()
                            );


                    }
                    catch (System.Exception ex)
                    {
                        if (txtListado) txtListado.text = "Error: " + ex.Message;
                    }
                });


            // Eliminar partida por UID escrito en el input
            if (btnEliminarUID)
                btnEliminarUID.onClick.AddListener(async () =>
                {
                    var uid = inputUid ? inputUid.text.Trim() : "";
                    if (string.IsNullOrEmpty(uid))
                    {
                        if (txtListado) txtListado.text = "UID vacío.";
                        return;
                    }

                    if (txtListado) txtListado.text = "Eliminando partida...";
                    try
                    {
                        await gameSave.firestore.EliminarPartidaAsync(gameSave.IdToken, uid);
                        if (txtListado) txtListado.text = $"Partida borrada ✅ (UID: {uid})";
                    }
                    catch (Exception ex)
                    {
                        if (txtListado) txtListado.text = "Error borrar partida: " + ex.Message;
                    }
                });

            if (btnCerrarSesionAdmin)
                btnCerrarSesionAdmin.onClick.AddListener(() =>
                {
                    gameSave.SignOutLocal();
                    SceneManager.LoadScene("LoginRegistro");
                });
        }
        catch (Exception ex)
        {
            Debug.LogError("MenuPrincipalUI.Start fallo: " + ex);
            if (txtListado) txtListado.text = "Error en Menú: " + ex.Message;
        }
    }
}
