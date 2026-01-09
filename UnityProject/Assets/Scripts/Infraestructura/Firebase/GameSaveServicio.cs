using System.Threading.Tasks;
using UnityEngine;

// Guardado y recuperado en local los PlayerPrefs el idToken y el uid del usuario
// Hacer login/registro con FirebaseAuthCliente
// Guardar y cargar la partida con FirestoreCliente
//
// La idea es que la UI  y el juego llamen a este servicio
// sin tener que saber detalles de Firebase ni de PlayerPrefs.
public class GameSaveServicio : MonoBehaviour
{
    // Referencia al cliente de autenticación
    public FirebaseAuthCliente auth;

    // Referencia al cliente de base de datos
    public FirestoreCliente firestore;

    // Token
    public string IdToken { get; private set; }

    // UID
    public string Uid { get; private set; }

    // Claves que usamos en PlayerPrefs para guardar la sesión en el PC del jugador
    const string PREF_IDTOKEN = "DF_IdToken";
    const string PREF_UID = "DF_Uid";

    void Awake()
    {
        // Al arrancar el juego, si ya se inició anteriormente sesión, se mantiene guardada
        CargarSesionLocal();
    }

    // Guardamos en PlayerPrefs el token y el uid, para mantener la sesión guardada
    void GuardarSesionLocal()
    {
        // Solo guardamos si ambos valores existen
        if (!string.IsNullOrEmpty(IdToken) && !string.IsNullOrEmpty(Uid))
        {
            PlayerPrefs.SetString(PREF_IDTOKEN, IdToken);
            PlayerPrefs.SetString(PREF_UID, Uid);

            // Forzamos el guardado
            PlayerPrefs.Save();
        }
    }

    // Carga de PlayerPrefs el token y uid guardados
    void CargarSesionLocal()
    {
        // GetString nos devuelve el valor si existe o el default si no existe
        IdToken = PlayerPrefs.GetString(PREF_IDTOKEN, null);
        Uid = PlayerPrefs.GetString(PREF_UID, null);

        // Si falta cualquiera de los dos, no hay sesión activa
        if (string.IsNullOrEmpty(IdToken) || string.IsNullOrEmpty(Uid))
        {
            IdToken = null;
            Uid = null;
        }
    }

    // Borra la sesión local guardada en PlayerPrefs
    void BorrarSesionLocal()
    {
        PlayerPrefs.DeleteKey(PREF_IDTOKEN);
        PlayerPrefs.DeleteKey(PREF_UID);
    }

    // Hacemos el login en Firebase con email y contraseña
    // Si el login funciona, guardamos idToken y uid en memoria y en PlayerPrefs
    public async Task<bool> LoginAsync(string correo, string pass)
    {
        // Llamamos al cliente de Auth, nos devuelve el idToken y el uid
        var r = await auth.IniciarSesionAsync(correo, pass);

        IdToken = r.idToken;
        Uid = r.localId;

        GuardarSesionLocal();

        return true;
    }

    // Registro en Firebase con email y contraseña y creamos del documento usuario en Firestore
    // Además de guardar sesión local igual que en login
    public async Task<bool> RegistroAsync(string correo, string pass, string nombreUsuario, bool esAdmin)
    {
        // Creamos cuenta en Firebase Auth (esto ya genera el usuario y su uid)
        var r = await auth.RegistrarseAsync(correo, pass);

        IdToken = r.idToken;
        Uid = r.localId;

        // Guardamos la sesión
        GuardarSesionLocal();

        // Creamos el documento /usuarios/{uid} con los datos
        await firestore.UpsertUsuarioAsync(IdToken, Uid, correo, nombreUsuario, esAdmin);

        return true;
    }

    // Si no se pasa esAdmin, por defecto false
    public Task<bool> RegistroAsync(string correo, string pass, string nombreUsuario)
        => RegistroAsync(correo, pass, nombreUsuario, false);

    // Guardamos la partida en Firestore en /partidasGuardadas/{uid}.
    public Task GuardarAsync(FirestoreCliente.PartidaGuardada p)
        => firestore.GuardarPartidaAsync(IdToken, Uid, p);

    // Cargamos la partida desde /partidasGuardadas/{uid}.
    public Task<FirestoreCliente.PartidaGuardada> CargarAsync()
        => firestore.CargarPartidaAsync(IdToken, Uid);

    // Cerramos la sesión local: eliminamos el token y uid en memoria y borramos PlayerPrefs.
    public void SignOutLocal()
    {
        IdToken = null;
        Uid = null;
        BorrarSesionLocal();
    }
}
