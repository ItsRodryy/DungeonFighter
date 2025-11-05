using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// Excepción específica de Firebase Auth para mensajes limpios.
public class FirebaseAuthException : System.Exception
{
    public long HttpCode { get; }
    public string FirebaseMessage { get; }
    public FirebaseAuthException(long httpCode, string firebaseMessage)
        : base($"{httpCode}: {firebaseMessage}")
    {
        HttpCode = httpCode;
        FirebaseMessage = firebaseMessage;
    }
}

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
        public string localId; // UID
    }

    public FirebaseAjustes ajustes;

    string Url(string metodo) =>
        $"https://identitytoolkit.googleapis.com/v1/accounts:{metodo}?key={ajustes.apiKey}";

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

        // IMPORTANTE: si hay error HTTP lanzamos SIEMPRE una excepción controlada (FirebaseAuthException)
        if (req.result != UnityWebRequest.Result.Success)
        {
            var detalle = req.downloadHandler != null ? req.downloadHandler.text : "";
            string fbMsg = "ERROR_AUTENTICACION";
            try
            {
                // Esperado: { "error": { "message": "INVALID_LOGIN_CREDENTIALS", ... } }
                var o = Newtonsoft.Json.Linq.JObject.Parse(detalle);
                fbMsg = (string)o["error"]?["message"] ?? fbMsg;
            }
            catch { /* si no es JSON, mantenemos fbMsg genérico */ }

            var ex = new FirebaseAuthException(req.responseCode, fbMsg);
            Debug.LogWarning($"FirebaseAuth => {ex.Message} | detalle={detalle}");
            throw ex;
        }

        return req.downloadHandler.text;
    }

    public async Task<RespuestaAuth> RegistrarseAsync(string correo, string contrasena)
    {
        var body = JsonConvert.SerializeObject(new { email = correo, password = contrasena, returnSecureToken = true });
        var json = await EnviarJsonAsync(Url("signUp"), UnityWebRequest.kHttpVerbPOST, body);
        return JsonConvert.DeserializeObject<RespuestaAuth>(json);
    }

    public async Task<RespuestaAuth> IniciarSesionAsync(string correo, string contrasena)
    {
        var body = JsonConvert.SerializeObject(new { email = correo, password = contrasena, returnSecureToken = true });
        var json = await EnviarJsonAsync(Url("signInWithPassword"), UnityWebRequest.kHttpVerbPOST, body);
        return JsonConvert.DeserializeObject<RespuestaAuth>(json);
    }
}
