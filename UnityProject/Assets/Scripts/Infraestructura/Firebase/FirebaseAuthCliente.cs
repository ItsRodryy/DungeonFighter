using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

// Excepción propia para controlar errores de autenticación y poder mostrar mensajes más claros
public class FirebaseAuthException : System.Exception
{
    // Guardamos el código HTTP que devuelve Firebase
    public long HttpCode { get; }

    // Guardamos el mensaje concreto que manda Firebase
    public string FirebaseMessage { get; }

    // Construimos la excepción con el código y el mensaje para tenerlo todo localizado
    public FirebaseAuthException(long httpCode, string firebaseMessage)
        : base($"{httpCode}: {firebaseMessage}")
    {
        HttpCode = httpCode;
        FirebaseMessage = firebaseMessage;
    }
}

// Cliente REST para registrar e iniciar sesión en FirebaseAuth con email y contraseña
// Al autenticarnos obtenemos idToken para autorizar llamadas a Firestore y localId como UID del usuario
public class FirebaseAuthCliente : MonoBehaviour
{
    [System.Serializable]
    public class RespuestaAuth
    {
        // Token de sesión que luego usamos como Bearer en Firestore
        public string idToken;

        // Email asociado a la cuenta
        public string email;

        // Token de refresco que Firebase también devuelve
        public string refreshToken;

        // Tiempo de expiración del idToken en segundos como string
        public string expiresIn;

        // UID del usuario dentro de Firebase
        public string localId;
    }

    public FirebaseAjustes ajustes;

    // Montamos la URL de Identity Toolkit a partir del método y la apiKey del proyecto
    string Url(string metodo) =>
        $"https://identitytoolkit.googleapis.com/v1/accounts:{metodo}?key={ajustes.apiKey}";

    // Enviamos JSON con UnityWebRequest y devolvemos la respuesta en texto
    async Task<string> EnviarJsonAsync(string url, string metodo, string json)
    {
        var req = new UnityWebRequest(url, metodo);

        // Si tenemos body lo convertimos a bytes y lo enviamos como JSON
        if (json != null)
        {
            var bytes = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(bytes);
            req.SetRequestHeader("Content-Type", "application/json");
        }

        // Preparamos un buffer para leer la respuesta
        req.downloadHandler = new DownloadHandlerBuffer();

        // Esperamos a que termine la petición sin bloquear el hilo principal
        var tcs = new TaskCompletionSource<bool>();
        req.SendWebRequest().completed += _ => tcs.SetResult(true);
        await tcs.Task;

        // Si falla la petición construimos una excepción propia con el detalle de Firebase
        if (req.result != UnityWebRequest.Result.Success)
        {
            // Cogemos el texto de respuesta para poder parsear el error
            var detalle = req.downloadHandler != null ? req.downloadHandler.text : "";

            // Ponemos un mensaje por defecto por si no conseguimos parsear nada
            string fbMsg = "ERROR_AUTENTICACION";

            try
            {
                // Intentamos extraer error.message que es lo típico en Firebase
                // Por ejemplo INVALID_PASSWORD EMAIL_NOT_FOUND etc
                var o = Newtonsoft.Json.Linq.JObject.Parse(detalle);
                fbMsg = (string)o["error"]?["message"] ?? fbMsg;
            }
            catch
            {
                // Si el parseo falla seguimos con el mensaje por defecto
            }

            // Creamos la excepción con código HTTP y mensaje Firebase
            var ex = new FirebaseAuthException(req.responseCode, fbMsg);

            // Logueamos un aviso con el detalle para depurar sin reventar el flujo en silencio
            Debug.LogWarning($"FirebaseAuth => {ex.Message} | detalle={detalle}");

            // Lanzamos la excepción para que el que llama pueda capturarla y mostrar UI
            throw ex;
        }

        // Si todo va bien devolvemos el JSON en texto
        return req.downloadHandler.text;
    }

    // Registramos un usuario con email y contraseña y devolvemos los tokens y el uid
    public async Task<RespuestaAuth> RegistrarseAsync(string correo, string contrasena)
    {
        // Construimos el body que exige Firebase Auth REST
        var body = JsonConvert.SerializeObject(new
        {
            email = correo,
            password = contrasena,
            returnSecureToken = true
        });

        // Hacemos POST a signUp y parseamos la respuesta
        var json = await EnviarJsonAsync(Url("signUp"), UnityWebRequest.kHttpVerbPOST, body);
        return JsonConvert.DeserializeObject<RespuestaAuth>(json);
    }

    // Iniciamos sesión con email y contraseña y devolvemos idToken y uid para usar en Firestore
    public async Task<RespuestaAuth> IniciarSesionAsync(string correo, string contrasena)
    {
        // Construimos el body igual que en registro pero usando el endpoint de login
        var body = JsonConvert.SerializeObject(new
        {
            email = correo,
            password = contrasena,
            returnSecureToken = true
        });

        // Hacemos POST a signInWithPassword y parseamos la respuesta
        var json = await EnviarJsonAsync(Url("signInWithPassword"), UnityWebRequest.kHttpVerbPOST, body);
        return JsonConvert.DeserializeObject<RespuestaAuth>(json);
    }
}
