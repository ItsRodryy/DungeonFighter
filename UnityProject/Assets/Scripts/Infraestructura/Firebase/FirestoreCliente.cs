using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

// Cliente REST para Firestore
public class FirestoreCliente : MonoBehaviour
{
    public FirebaseAjustes ajustes;

    // Construimos la URL del documento a partir de la colección y el id
    string DocUrl(string col, string id) =>
        $"https://firestore.googleapis.com/v1/projects/{ajustes.projectId}/databases/(default)/documents/{col}/{id}";

    // Modelos de datos que usamos en para mover información por el juego
    [System.Serializable]
    public class UsuarioPerfil
    {
        public string email;
        public string nombreUsuario;
        public bool esAdmin;
    }

    // Estructuras de guardado que luego convertimos al formato REST de Firestore
    [System.Serializable] public class DatosJugador { public int vida, vidaMaxima; public float posX, posY; public string nombreEscena; }
    [System.Serializable] public class DatosInventario { public int monedas, llaves, pociones; }
    [System.Serializable] public class EstadoMundo { public List<string> enemigosEliminados; public List<string> cofresAbiertos; }

    [System.Serializable]
    public class PartidaGuardada
    {
        public string nombrePartida;
        public DatosJugador datosJugador;
        public DatosInventario datosInventario;
        public EstadoMundo estadoMundo;
    }

    // Enviamos una petición HTTP a Firestore con UnityWebRequest y opcionalmente con token
    async Task<string> EnviarJsonAsync(string url, string metodo, string json, string idToken = null)
    {
        var req = new UnityWebRequest(url, metodo);

        // Solo metemos body si tenemos un JSON que enviar
        if (json != null)
        {
            var body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.SetRequestHeader("Content-Type", "application/json");
        }

        // Siempre leemos respuesta en texto
        req.downloadHandler = new DownloadHandlerBuffer();

        // Si tenemos token lo ponemos como Bearer para autorizar en Firestore
        if (!string.IsNullOrEmpty(idToken))
            req.SetRequestHeader("Authorization", $"Bearer {idToken}");

        // Esperamos a que termine la request sin bloquear el hilo principal
        var tcs = new TaskCompletionSource<bool>();
        req.SendWebRequest().completed += _ => tcs.SetResult(true);
        await tcs.Task;

        // Si falla devolvemos una excepción con detalles para debug
        if (req.result != UnityWebRequest.Result.Success)
            throw new System.Exception($"{metodo} {url} => {req.responseCode}: {req.error} {req.downloadHandler.text}");

        return req.downloadHandler.text;
    }

    // Creamos o actualizamos /usuarios/{uid} con el rol indicado
    public async Task UpsertUsuarioAsync(string idToken, string uid, string email, string nombreUsuario, bool esAdmin)
    {
        var url = DocUrl("usuarios", uid);

        // Construimos el payload con el formato fields que exige la API REST de Firestore
        var payload = new
        {
            fields = new
            {
                email = new { stringValue = email ?? "" },
                nombreUsuario = new { stringValue = nombreUsuario ?? "" },
                fechaCreacion = new { timestampValue = System.DateTime.UtcNow.ToString("o") },
                esAdmin = new { booleanValue = esAdmin },
                activo = new { booleanValue = true }
            }
        };

        // Serializamos a JSON y hacemos PATCH para crear o sobrescribir el documento
        var body = JsonConvert.SerializeObject(payload);
        await EnviarJsonAsync(url, "PATCH", body, idToken);
    }

    // Leemos /usuarios/{uid} y devolvemos el perfil con email nombreUsuario y esAdmin
    public async Task<UsuarioPerfil> GetUsuarioPerfilAsync(string idToken, string uid)
    {
        var url = DocUrl("usuarios", uid);

        // Hacemos GET al documento del usuario
        var txt = await EnviarJsonAsync(url, UnityWebRequest.kHttpVerbGET, null, idToken);

        // Parseamos el JSON de Firestore y sacamos el nodo fields
        var doc = JObject.Parse(txt);
        var f = doc["fields"] ?? throw new System.Exception("Usuario sin 'fields'");

        return new UsuarioPerfil
        {
            email = GetString(f, "email"),
            nombreUsuario = GetString(f, "nombreUsuario"),
            esAdmin = GetBool(f, "esAdmin")
        };
    }

    // Listamos usuarios usando runQuery con un POST
    public async Task<List<(string uid, UsuarioPerfil perfil)>> ListarUsuariosAsync(string idToken, int pageSize = 200)
    {
        var url = $"https://firestore.googleapis.com/v1/projects/{ajustes.projectId}/databases/(default)/documents:runQuery";

        // Montamos una structuredQuery simple que trae documentos de la colección usuarios
        var bodyObj = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "usuarios" } },
                limit = pageSize
            }
        };

        // Enviamos el POST y recibimos un array de resultados
        var body = JsonConvert.SerializeObject(bodyObj);
        var txt = await EnviarJsonAsync(url, UnityWebRequest.kHttpVerbPOST, body, idToken);

        var arr = JArray.Parse(txt);
        var list = new List<(string, UsuarioPerfil)>();

        // Recorremos cada resultado y si trae document lo convertimos a nuestro modelo
        foreach (var r in arr)
        {
            var doc = r["document"];
            if (doc == null) continue;

            // Sacamos el uid desde el name del documento que viene como ruta completa
            var name = doc["name"]?.ToString();
            if (string.IsNullOrEmpty(name)) continue;

            var uid = name.Substring(name.LastIndexOf('/') + 1);

            var f = doc["fields"];
            if (f == null) continue;

            var perfil = new UsuarioPerfil
            {
                email = GetString(f, "email"),
                nombreUsuario = GetString(f, "nombreUsuario"),
                esAdmin = GetBool(f, "esAdmin")
            };

            list.Add((uid, perfil));
        }

        return list;
    }

    // Convertimos nuestro objeto PartidaGuardada al formato que espera Firestore REST
    object ToFirestore(PartidaGuardada p) => new
    {
        fields = new
        {
            nombrePartida = new { stringValue = p.nombrePartida ?? "" },

            // Guardamos datosJugador como un mapValue con sus fields internos
            datosJugador = new
            {
                mapValue = new
                {
                    fields = new
                    {
                        vida = new { integerValue = p.datosJugador.vida.ToString() },
                        vidaMaxima = new { integerValue = p.datosJugador.vidaMaxima.ToString() },
                        posX = new { doubleValue = p.datosJugador.posX },
                        posY = new { doubleValue = p.datosJugador.posY },
                        nombreEscena = new { stringValue = p.datosJugador.nombreEscena ?? "Juego" }
                    }
                }
            },

            // Guardamos datosInventario como un mapValue con sus fields internos (el inventario no está desarrollado aún)
            datosInventario = new
            {
                mapValue = new
                {
                    fields = new
                    {
                        monedas = new { integerValue = p.datosInventario.monedas.ToString() },
                        llaves = new { integerValue = p.datosInventario.llaves.ToString() },
                        pociones = new { integerValue = p.datosInventario.pociones.ToString() }
                    }
                }
            },

            // Guardamos estadoMundo con arrays de strings en formato arrayValue values
            estadoMundo = new
            {
                mapValue = new
                {
                    fields = new
                    {
                        enemigosEliminados = new
                        {
                            arrayValue = new
                            {
                                values = (p.estadoMundo.enemigosEliminados ?? new List<string>())
                                    .ConvertAll(s => (object)new { stringValue = s })
                            }
                        },
                        cofresAbiertos = new
                        {
                            arrayValue = new
                            {
                                values = (p.estadoMundo.cofresAbiertos ?? new List<string>())
                                    .ConvertAll(s => (object)new { stringValue = s })
                            }
                        }
                    }
                }
            },

            // Añadimos timestamp de último guardado para tener trazabilidad
            ultimaActualizacion = new { timestampValue = System.DateTime.UtcNow.ToString("o") }
        }
    };

    // Guardamos sobrescribiendo en /partidasGuardadas/{uid}
    public async Task GuardarPartidaAsync(string idToken, string uid, PartidaGuardada p)
    {
        var url = DocUrl("partidasGuardadas", uid);

        // Convertimos a formato Firestore y hacemos PATCH
        var body = JsonConvert.SerializeObject(ToFirestore(p));
        await EnviarJsonAsync(url, "PATCH", body, idToken);
    }

    // Cargamos /partidasGuardadas/{uid} y lo convertimos a nuestro modelo PartidaGuardada
    public async Task<PartidaGuardada> CargarPartidaAsync(string idToken, string uid)
    {
        var url = DocUrl("partidasGuardadas", uid);

        // Hacemos GET al documento de partida
        var txt = await EnviarJsonAsync(url, UnityWebRequest.kHttpVerbGET, null, idToken);

        // Parseamos y sacamos fields del documento
        var doc = JObject.Parse(txt);
        var f = doc["fields"] ?? throw new System.Exception("Documento sin 'fields'");

        // Reconstruimos el objeto PartidaGuardada leyendo los campos anidados
        var p = new PartidaGuardada
        {
            nombrePartida = GetString(f, "nombrePartida"),

            datosJugador = new DatosJugador
            {
                vida = GetInt(f["datosJugador"]?["mapValue"]?["fields"], "vida"),
                vidaMaxima = GetInt(f["datosJugador"]?["mapValue"]?["fields"], "vidaMaxima"),
                posX = GetFloat(f["datosJugador"]?["mapValue"]?["fields"], "posX"),
                posY = GetFloat(f["datosJugador"]?["mapValue"]?["fields"], "posY"),
                nombreEscena = GetString(f["datosJugador"]?["mapValue"]?["fields"], "nombreEscena")
            },

            datosInventario = new DatosInventario
            {
                monedas = GetInt(f["datosInventario"]?["mapValue"]?["fields"], "monedas"),
                llaves = GetInt(f["datosInventario"]?["mapValue"]?["fields"], "llaves"),
                pociones = GetInt(f["datosInventario"]?["mapValue"]?["fields"], "pociones")
            },

            estadoMundo = new EstadoMundo
            {
                enemigosEliminados = GetArrayStrings(f["estadoMundo"]?["mapValue"]?["fields"], "enemigosEliminados"),
                cofresAbiertos = GetArrayStrings(f["estadoMundo"]?["mapValue"]?["fields"], "cofresAbiertos")
            }
        };

        return p;
    }

    // Listamos partidas para un panel admin usando runQuery
    public async Task<List<(string uid, PartidaGuardada partida)>> ListarPartidasAsync(string idToken, int pageSize = 50)
    {
        var url = $"https://firestore.googleapis.com/v1/projects/{ajustes.projectId}/databases/(default)/documents:runQuery";

        // Pedimos documentos de la colección partidasGuardadas con un límite
        var bodyObj = new
        {
            structuredQuery = new
            {
                from = new[] { new { collectionId = "partidasGuardadas" } },
                limit = pageSize
            }
        };

        var body = JsonConvert.SerializeObject(bodyObj);
        var txt = await EnviarJsonAsync(url, UnityWebRequest.kHttpVerbPOST, body, idToken);

        var arr = JArray.Parse(txt);
        var list = new List<(string, PartidaGuardada)>();

        // Recogemos uid y reconstruimos PartidaGuardada igual que en la carga individual
        foreach (var r in arr)
        {
            var doc = r["document"];
            if (doc == null) continue;

            var name = doc["name"]?.ToString();
            if (string.IsNullOrEmpty(name)) continue;

            var uid = name.Substring(name.LastIndexOf('/') + 1);

            var f = doc["fields"];
            if (f == null) continue;

            var p = new PartidaGuardada
            {
                nombrePartida = GetString(f, "nombrePartida"),
                datosJugador = new DatosJugador
                {
                    vida = GetInt(f["datosJugador"]?["mapValue"]?["fields"], "vida"),
                    vidaMaxima = GetInt(f["datosJugador"]?["mapValue"]?["fields"], "vidaMaxima"),
                    posX = GetFloat(f["datosJugador"]?["mapValue"]?["fields"], "posX"),
                    posY = GetFloat(f["datosJugador"]?["mapValue"]?["fields"], "posY"),
                    nombreEscena = GetString(f["datosJugador"]?["mapValue"]?["fields"], "nombreEscena")
                },
                datosInventario = new DatosInventario
                {
                    monedas = GetInt(f["datosInventario"]?["mapValue"]?["fields"], "monedas"),
                    llaves = GetInt(f["datosInventario"]?["mapValue"]?["fields"], "llaves"),
                    pociones = GetInt(f["datosInventario"]?["mapValue"]?["fields"], "pociones")
                },
                estadoMundo = new EstadoMundo
                {
                    enemigosEliminados = GetArrayStrings(f["estadoMundo"]?["mapValue"]?["fields"], "enemigosEliminados"),
                    cofresAbiertos = GetArrayStrings(f["estadoMundo"]?["mapValue"]?["fields"], "cofresAbiertos")
                }
            };

            list.Add((uid, p));
        }

        return list;
    }

    // Eliminamos la partida de un uid
    public async Task EliminarPartidaAsync(string idToken, string uid)
    {
        var url = DocUrl("partidasGuardadas", uid);
        await EnviarJsonAsync(url, UnityWebRequest.kHttpVerbDELETE, null, idToken);
    }

    // Helpers para leer valores de la respuesta REST de Firestore
    static string GetString(JToken parent, string name)
        => parent?[name]?["stringValue"]?.ToString() ?? "";

    static bool GetBool(JToken parent, string name)
        => parent?[name]?["booleanValue"]?.Value<bool>() ?? false;

    static int GetInt(JToken parent, string name)
    {
        var t = parent?[name]?["integerValue"];
        if (t == null) return 0;
        int.TryParse(t.ToString(), out var v);
        return v;
    }

    static float GetFloat(JToken parent, string name)
    {
        // Primero intentamos doubleValue que es lo típico en Firestore para floats
        var tD = parent?[name]?["doubleValue"];
        if (tD != null && (tD.Type == JTokenType.Float || tD.Type == JTokenType.Integer)) return (float)tD;

        // Si viniera como stringValue intentamos parsearlo con InvariantCulture
        var tS = parent?[name]?["stringValue"];
        if (tS != null && double.TryParse(tS.ToString(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var dv)) return (float)dv;

        // Si viniera como integerValue lo convertimos a float
        var tI = parent?[name]?["integerValue"];
        if (tI != null && long.TryParse(tI.ToString(), out var iv)) return (float)iv;

        return 0f;
    }

    static List<string> GetArrayStrings(JToken parent, string name)
    {
        var list = new List<string>();

        // Leemos arrayValue values y sacamos cada stringValue
        var values = parent?[name]?["arrayValue"]?["values"] as JArray;
        if (values != null)
            foreach (var v in values)
            {
                var s = v?["stringValue"]?.ToString();
                if (!string.IsNullOrEmpty(s)) list.Add(s);
            }

        return list;
    }
}
