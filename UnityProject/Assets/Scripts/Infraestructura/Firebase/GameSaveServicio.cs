using System.Threading.Tasks;
using UnityEngine;

// Servicio que guarda idToken/uid tras login/registro
// y expone Guardar/Cargar para que la UI o el juego lo llamen fácil.
public class GameSaveServicio : MonoBehaviour
{
    public FirebaseAuthCliente auth;     // Arrastra el componente del mismo objeto
    public FirestoreCliente firestore;   // Arrastra el componente del mismo objeto

    public string IdToken { get; private set; }
    public string Uid { get; private set; }

    // Login: guarda token y uid
    public async Task<bool> LoginAsync(string correo, string pass)
    {
        var r = await auth.IniciarSesionAsync(correo, pass);
        IdToken = r.idToken;
        Uid = r.localId;
        return true;
    }

    // Registro (NUEVO): crea usuario y su documento con el rol indicado
    public async Task<bool> RegistroAsync(string correo, string pass, string nombreUsuario, bool esAdmin)
    {
        var r = await auth.RegistrarseAsync(correo, pass);
        IdToken = r.idToken;
        Uid = r.localId;

        await firestore.UpsertUsuarioAsync(IdToken, Uid, correo, nombreUsuario, esAdmin);
        return true;
    }

    // Overload para no romper llamadas antiguas (por ej. PruebaConexion)
    public Task<bool> RegistroAsync(string correo, string pass, string nombreUsuario)
        => RegistroAsync(correo, pass, nombreUsuario, false);

    // Guarda/Carga sobre /partidasGuardadas/{uid} (un único guardado por usuario)
    public Task GuardarAsync(FirestoreCliente.PartidaGuardada p) => firestore.GuardarPartidaAsync(IdToken, Uid, p);
    public Task<FirestoreCliente.PartidaGuardada> CargarAsync() => firestore.CargarPartidaAsync(IdToken, Uid);

    // Cerrar sesión local (borra credenciales en memoria)
    public void SignOutLocal()
    {
        IdToken = null;
        Uid = null;
    }
}
