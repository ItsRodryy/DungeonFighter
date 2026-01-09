using System;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Controlamos el menú principal y mostramos opciones según el rol del usuario
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

    private async void Start()
    {
        try
        {
            // Si no lo asignamos en el inspector lo buscamos en la escena
            if (!gameSave)
                gameSave = UnityEngine.Object.FindFirstObjectByType<GameSaveServicio>();

            // Si no tenemos sesión válida volvemos a la escena de login y registro
            if (gameSave == null || string.IsNullOrEmpty(gameSave.Uid) || string.IsNullOrEmpty(gameSave.IdToken))
            {
                SceneManager.LoadScene("LoginRegistro");
                return;
            }

            // Cargamos el perfil para saber si es admin y para poner el texto de bienvenida
            FirestoreCliente.UsuarioPerfil perfil = null;
            try
            {
                perfil = await gameSave.firestore.GetUsuarioPerfilAsync(gameSave.IdToken, gameSave.Uid);
            }
            catch
            {
                // Si falla seguimos con el flujo normal y tratamos el usuario como no admin
            }

            // Si tenemos perfil ponemos el nombre del usuario en pantalla
            if (txtBienvenida && perfil != null)
                txtBienvenida.text = $"¡Qué dise er máquina, {perfil.nombreUsuario}!";

            // Determinamos si el usuario es admin según el perfil
            bool esAdmin = perfil != null && perfil.esAdmin;

            // Activamos el panel correspondiente según el rol
            if (panelAdmin) panelAdmin.SetActive(esAdmin);
            if (panelUsuario) panelUsuario.SetActive(!esAdmin);

            // Botones del panel de usuario normal

            if (btnNuevaPartida)
                btnNuevaPartida.onClick.AddListener(async () =>
                {
                    // Mostramos un texto para que el usuario sepa lo que estamos haciendo
                    if (txtListado) txtListado.text = "Creando nueva partida...";

                    try
                    {
                        // Borramos la partida anterior si existía para empezar de cero
                        await gameSave.firestore.EliminarPartidaAsync(gameSave.IdToken, gameSave.Uid);
                    }
                    catch
                    {
                        // Si no existe la partida anterior no pasa nada y seguimos
                    }

                    // Entramos al juego
                    SceneManager.LoadScene("Juego");
                });

            if (btnCargarPartida)
                btnCargarPartida.onClick.AddListener(async () =>
                {
                    // Avisamos que estamos cargando datos
                    if (txtListado) txtListado.text = "Cargando...";

                    try
                    {
                        // Cargamos la partida del usuario y saltamos a la escena guardada
                        var p = await gameSave.CargarAsync();
                        SceneManager.LoadScene(p.datosJugador.nombreEscena);
                    }
                    catch (Exception ex)
                    {
                        // Si falla mostramos el error en pantalla
                        if (txtListado) txtListado.text = "Error cargar: " + ex.Message;
                    }
                });

            if (btnCerrarSesion)
                btnCerrarSesion.onClick.AddListener(() =>
                {
                    // Cerramos la sesión local y volvemos al login
                    gameSave.SignOutLocal();
                    SceneManager.LoadScene("LoginRegistro");
                });

            // Botones del panel admin

            // Usamos un solo botón con doble comportamiento
            // Si el input está vacío listamos usuarios
            // Si el input tiene un uid mostramos la partida de ese uid
            if (btnListarPartidas)
                btnListarPartidas.onClick.AddListener(async () =>
                {
                    try
                    {
                        // Leemos el uid del input si existe
                        var uid = inputUid ? inputUid.text.Trim() : "";

                        // Si el texto es placeholder o está vacío lo tratamos como vacío
                        var uidLower = uid.ToLower();
                        if (string.IsNullOrWhiteSpace(uid) || uidLower.Contains("introduce") || uidLower.Contains("enter"))
                            uid = "";

                        // Si hay uid mostramos la partida de ese uid
                        if (!string.IsNullOrEmpty(uid))
                        {
                            if (txtListado) txtListado.text = "Cargando partida del UID...";

                            var p = await gameSave.firestore.CargarPartidaAsync(gameSave.IdToken, uid);

                            // Pintamos un resumen de la partida en el texto del panel
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

                        // Si no hay uid listamos usuarios
                        if (txtListado) txtListado.text = "Listando usuarios...";
                        var listaUsuarios = await gameSave.firestore.ListarUsuariosAsync(gameSave.IdToken, 200);

                        // Mostramos nombres de usuario sin repetir y sin vacíos
                        if (txtListado)
                            txtListado.text = string.Join("\n", listaUsuarios
                                .Select(u => u.perfil.nombreUsuario)
                                .Where(n => !string.IsNullOrWhiteSpace(n))
                                .Distinct()
                            );
                    }
                    catch (System.Exception ex)
                    {
                        // Si falla mostramos el error en el panel
                        if (txtListado) txtListado.text = "Error: " + ex.Message;
                    }
                });

            if (btnEliminarUID)
                btnEliminarUID.onClick.AddListener(async () =>
                {
                    // Cogemos el uid escrito en el input
                    var uid = inputUid ? inputUid.text.Trim() : "";

                    // Si viene vacío no hacemos nada y avisamos
                    if (string.IsNullOrEmpty(uid))
                    {
                        if (txtListado) txtListado.text = "UID vacío.";
                        return;
                    }

                    // Avisamos que estamos eliminando
                    if (txtListado) txtListado.text = "Eliminando partida...";

                    try
                    {
                        // Eliminamos la partida del uid indicado
                        await gameSave.firestore.EliminarPartidaAsync(gameSave.IdToken, uid);

                        // Confirmamos el borrado en pantalla
                        if (txtListado) txtListado.text = $"Partida borrada (UID: {uid})";
                    }
                    catch (Exception ex)
                    {
                        // Si falla mostramos el error en el panel
                        if (txtListado) txtListado.text = "Error borrar partida: " + ex.Message;
                    }
                });

            if (btnCerrarSesionAdmin)
                btnCerrarSesionAdmin.onClick.AddListener(() =>
                {
                    // Cerramos la sesión local y volvemos al login
                    gameSave.SignOutLocal();
                    SceneManager.LoadScene("LoginRegistro");
                });
        }
        catch (Exception ex)
        {
            // Si algo revienta al iniciar lo dejamos registrado y avisamos por pantalla si podemos
            Debug.LogError("MenuPrincipalUI.Start fallo: " + ex);
            if (txtListado) txtListado.text = "Error en Menú: " + ex.Message;
        }
    }
}
