using UnityEngine;
using System.Threading.Tasks;

public class PruebaConexion : MonoBehaviour
{
    public GameSaveServicio juego;

    async void Start()
    {
        var correo = "adrian@gmail.com";
        var pass = "adrian123456";

        // Registrar si no existe; si existe, login
        try { await juego.RegistroAsync(correo, pass, "Adri"); }
        catch { await juego.LoginAsync(correo, pass); }

        // Guardado mínimo (tu modelo)
        var p = new FirestoreCliente.PartidaGuardada
        {
            nombrePartida = "Mazmorra Nivel 1",
            datosJugador = new FirestoreCliente.DatosJugador { vida = 3, vidaMaxima = 3, posX = 15f, posY = -5f, nombreEscena = "Nivel_Mazmorra_1" },
            datosInventario = new FirestoreCliente.DatosInventario { monedas = 100, llaves = 10, pociones = 2 },
            estadoMundo = new FirestoreCliente.EstadoMundo
            {
                enemigosEliminados = new System.Collections.Generic.List<string> { "id_enemigo_01" },
                cofresAbiertos = new System.Collections.Generic.List<string> { "id_cofre_01" }
            }
        };
        await juego.GuardarAsync(p);

        // Cargar para verificar
        var c = await juego.CargarAsync();
        Debug.Log($"✅ Guardado y cargado OK: {c.nombrePartida} / {c.datosJugador.nombreEscena} ({c.datosJugador.posX},{c.datosJugador.posY})");
    }
}
