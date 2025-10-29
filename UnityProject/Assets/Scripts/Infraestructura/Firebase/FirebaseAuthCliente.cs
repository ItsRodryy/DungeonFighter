using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

// Cliente para registrar e iniciar sesión en FirebaseAuth (email/contraseña).
// Devuelve idToken (para autorizar en Firestore) y localId (UID).
public class FirebaseAuthCliente : MonoBehaviour
{
    [System.Serializable]
    public class RespuestaAuth
    {
        public string idToken;
        public string email;
        public string refreshToken;
        public string expiresIn;
        // UID del usuario
        public string localId;
    }

    public FirebaseAjustes ajustes;

    // Construye la URL del método de Auth con la apiKey
    string Url(string metodo) =>
        $"https://identitytoolkit.googleapis.com/v1/accounts:{metodo}?key={ajustes.apiKey}";

    // Envia un JSON con UnityWebRequest y espera con Task
    async Task<string> EnviarJsonAsync(string url, string metodo, string json)
    {
        var req = new UnityWebRequest(url, metodo);
        if (json != null)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.SetRequestHeader("Content-Type", "application/json");
        }
        req.downloadHandler = new DownloadHandlerBuffer();

        var tcs = new TaskCompletionSource<bool>();
        req.SendWebRequest().completed += _ => tcs.SetResult(true);
        await tcs.Task;

        if (req.result != UnityWebRequest.Result.Success)
            throw new System.Exception($"{metodo} {url} -> {req.responseCode}: {req.error} {req.downloadHandler.text}");

        return req.downloadHandler.text;
    }

    // Registro de usuario
    public async Task<RespuestaAuth> RegistrarseAsync(string correo, string contrasena)
    {
        var body = JsonConvert.SerializeObject(new { email = correo, password = contrasena, returnSecureToken = true });
        var json = await EnviarJsonAsync(Url("signUp"), UnityWebRequest.kHttpVerbPOST, body);
        return JsonConvert.DeserializeObject<RespuestaAuth>(json);
    }

    // Login de usuario
    public async Task<RespuestaAuth> IniciarSesionAsync(string correo, string contrasena)
    {
        var body = JsonConvert.SerializeObject(new { email = correo, password = contrasena, returnSecureToken = true });
        var json = await EnviarJsonAsync(Url("signInWithPassword"), UnityWebRequest.kHttpVerbPOST, body);
        return JsonConvert.DeserializeObject<RespuestaAuth>(json);
    }
}
