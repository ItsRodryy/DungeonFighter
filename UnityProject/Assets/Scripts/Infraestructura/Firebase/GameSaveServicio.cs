using System.Threading.Tasks;
using UnityEngine;

// Servicio que guarda token/uid y llama a FirebaseAuth + Firestore
public class GameSaveServicio : MonoBehaviour
{
    public FirebaseAuthCliente auth;
    public FirestoreCliente firestore;

    public string IdToken { get; private set; }
    public string Uid { get; private set; }

    // Claves para PlayerPrefs
    const string PREF_IDTOKEN = "DF_IdToken";
    const string PREF_UID = "DF_Uid";

    void Awake()
    {
        // Al arrancar, intentamos recuperar sesión guardada
        CargarSesionLocal();
    }

    void GuardarSesionLocal()
    {
        if (!string.IsNullOrEmpty(IdToken) && !string.IsNullOrEmpty(Uid))
        {
            PlayerPrefs.SetString(PREF_IDTOKEN, IdToken);
            PlayerPrefs.SetString(PREF_UID, Uid);
            PlayerPrefs.Save();
        }
    }

    void CargarSesionLocal()
    {
        IdToken = PlayerPrefs.GetString(PREF_IDTOKEN, null);
        Uid = PlayerPrefs.GetString(PREF_UID, null);

        // Si falta algo, lo dejamos a null
        if (string.IsNullOrEmpty(IdToken) || string.IsNullOrEmpty(Uid))
        {
            IdToken = null;
            Uid = null;
        }
    }

    void BorrarSesionLocal()
    {
        PlayerPrefs.DeleteKey(PREF_IDTOKEN);
        PlayerPrefs.DeleteKey(PREF_UID);
    }

    // Login, guarda token y uid
    public async Task<bool> LoginAsync(string correo, string pass)
    {
        var r = await auth.IniciarSesionAsync(correo, pass);
        IdToken = r.idToken;
        Uid = r.localId;

        GuardarSesionLocal();
        return true;
    }

    // Registro, crea usuario y su documento indicando si es admin o no
    public async Task<bool> RegistroAsync(string correo, string pass, string nombreUsuario, bool esAdmin)
    {
        var r = await auth.RegistrarseAsync(correo, pass);
        IdToken = r.idToken;
        Uid = r.localId;

        GuardarSesionLocal();

        await firestore.UpsertUsuarioAsync(IdToken, Uid, correo, nombreUsuario, esAdmin);
        return true;
    }

    public Task<bool> RegistroAsync(string correo, string pass, string nombreUsuario)
        => RegistroAsync(correo, pass, nombreUsuario, false);

    // Guarda/Carga sobre /partidasGuardadas/{uid} (UNA partida por usuario)
    public Task GuardarAsync(FirestoreCliente.PartidaGuardada p)
        => firestore.GuardarPartidaAsync(IdToken, Uid, p);

    public Task<FirestoreCliente.PartidaGuardada> CargarAsync()
        => firestore.CargarPartidaAsync(IdToken, Uid);

    // Cerrar sesión en local
    public void SignOutLocal()
    {
        IdToken = null;
        Uid = null;
        BorrarSesionLocal();
    }
}
