using System.Threading.Tasks;
using UnityEngine;

// Servicio que guarda el token/uid tras el logueo y expone guardar o cargar para que la UI o el juego lo llamen fácil
public class GameSaveServicio : MonoBehaviour
{
    public FirebaseAuthCliente auth;
    public FirestoreCliente firestore;

    public string IdToken { get; private set; }
    public string Uid { get; private set; }

    // Login, guarda token y uid
    public async Task<bool> LoginAsync(string correo, string pass)
    {
        var r = await auth.IniciarSesionAsync(correo, pass);
        IdToken = r.idToken;
        Uid = r.localId;
        return true;
    }

    // Registro, crea usuario y su documento indicando si es admin o no
    public async Task<bool> RegistroAsync(string correo, string pass, string nombreUsuario, bool esAdmin)
    {
        var r = await auth.RegistrarseAsync(correo, pass);
        IdToken = r.idToken;
        Uid = r.localId;

        await firestore.UpsertUsuarioAsync(IdToken, Uid, correo, nombreUsuario, esAdmin);
        return true;
    }

    public Task<bool> RegistroAsync(string correo, string pass, string nombreUsuario)
        => RegistroAsync(correo, pass, nombreUsuario, false);

    // Guarda/Carga sobre /partidasGuardadas/{uid} (una única partida por usuario)
    public Task GuardarAsync(FirestoreCliente.PartidaGuardada p) => firestore.GuardarPartidaAsync(IdToken, Uid, p);
    public Task<FirestoreCliente.PartidaGuardada> CargarAsync() => firestore.CargarPartidaAsync(IdToken, Uid);

    // Cerrar sesión en local
    public void SignOutLocal()
    {
        IdToken = null;
        Uid = null;
    }
}
