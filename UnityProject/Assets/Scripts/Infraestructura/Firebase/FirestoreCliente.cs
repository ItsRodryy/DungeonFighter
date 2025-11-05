using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

// Cliente REST para Firestore

// Colecciones:
// - usuarios/{uid} -> email, nombreUsuario, fechaCreacion, esAdmin, activo
// - partidasGuardadas/{uid} -> nombrePartida, ultimaActualizacion, datosJugador, datosInventario, estadoMundo

public class FirestoreCliente : MonoBehaviour
{
    public FirebaseAjustes ajustes;

    // Construímos la URL del documento
    string DocUrl(string col, string id) =>
        $"https://firestore.googleapis.com/v1/projects/{ajustes.projectId}/databases/(default)/documents/{col}/{id}";

    // MODELOS

    [System.Serializable]
    public class UsuarioPerfil
    {
        public string email;
        public string nombreUsuario;
        public bool esAdmin;
    }

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

    // CORE HTTP

    // Enviamos JSON con UnityWebRequest
    async Task<string> EnviarJsonAsync(string url, string metodo, string json, string idToken = null)
    {
        var req = new UnityWebRequest(url, metodo);
        if (json != null)
        {
            var body = Encoding.UTF8.GetBytes(json);
            req.uploadHandler = new UploadHandlerRaw(body);
            req.SetRequestHeader("Content-Type", "application/json");
        }
        req.downloadHandler = new DownloadHandlerBuffer();
        if (!string.IsNullOrEmpty(idToken))
            req.SetRequestHeader("Authorization", $"Bearer {idToken}");

        var tcs = new TaskCompletionSource<bool>();
        req.SendWebRequest().completed += _ => tcs.SetResult(true);
        await tcs.Task;

        if (req.result != UnityWebRequest.Result.Success)
            throw new System.Exception($"{metodo} {url} => {req.responseCode}: {req.error} {req.downloadHandler.text}");

        return req.downloadHandler.text;
    }

    // USUARIOS

    // Crea/actualiza /usuarios/{uid} con el ROL indicado
    public async Task UpsertUsuarioAsync(string idToken, string uid, string email, string nombreUsuario, bool esAdmin)
    {
        var url = DocUrl("usuarios", uid);
        var payload = new
        {
            fields = new
            {
                email = new { stringValue = email },
                nombreUsuario = new { stringValue = nombreUsuario ?? "" },
                fechaCreacion = new { timestampValue = System.DateTime.UtcNow.ToString("o") },
                esAdmin = new { booleanValue = esAdmin },
                activo = new { booleanValue = true }
            }
        };
        var body = JsonConvert.SerializeObject(payload);
        await EnviarJsonAsync(url, "PATCH", body, idToken);
    }

    // Leemos /usuarios/{uid} y devolvemos el perfil (incluímos esAdmin)
    public async Task<UsuarioPerfil> GetUsuarioPerfilAsync(string idToken, string uid)
    {
        var url = DocUrl("usuarios", uid);
        var txt = await EnviarJsonAsync(url, UnityWebRequest.kHttpVerbGET, null, idToken);

        var doc = JObject.Parse(txt);
        var f = doc["fields"] ?? throw new System.Exception("Usuario sin 'fields'");

        return new UsuarioPerfil
        {
            email = GetString(f, "email"),
            nombreUsuario = GetString(f, "nombreUsuario"),
            esAdmin = (bool?)f["esAdmin"]?["booleanValue"] ?? false
        };
    }

    // PARTIDAS

    // Convertimos nuestro objeto C# al formato de Firestore REST
    object ToFirestore(PartidaGuardada p) => new
    {
        fields = new
        {
            nombrePartida = new { stringValue = p.nombrePartida },

            // datosJugador
            datosJugador = new
            {
                mapValue = new
                {
                    fields = new
                    {
                        vida = new { integerValue = p.datosJugador.vida.ToString() },
                        vidaMaxima = new { integerValue = p.datosJugador.vidaMaxima.ToString() },
                        // posX/posY
                        posX = new { doubleValue = p.datosJugador.posX },
                        posY = new { doubleValue = p.datosJugador.posY },
                        nombreEscena = new { stringValue = p.datosJugador.nombreEscena }
                    }
                }
            },

            // datosInventario
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

            // estadoMundo
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

            // Marcamos el estado del último guardado
            ultimaActualizacion = new { timestampValue = System.DateTime.UtcNow.ToString("o") }
        }
    };

    // Guarda sobrescribiendo en /partidasGuardadas/{uid} con el último estado
    public async Task GuardarPartidaAsync(string idToken, string uid, PartidaGuardada p)
    {
        var url = DocUrl("partidasGuardadas", uid);
        var body = JsonConvert.SerializeObject(ToFirestore(p));
        await EnviarJsonAsync(url, "PATCH", body, idToken);
    }

    // Carga /partidasGuardadas/{uid} convertimos a nuestro objeto C#
    public async Task<PartidaGuardada> CargarPartidaAsync(string idToken, string uid)
    {
        var url = DocUrl("partidasGuardadas", uid);
        var txt = await EnviarJsonAsync(url, UnityWebRequest.kHttpVerbGET, null, idToken);

        // Parseo con JObject
        var doc = JObject.Parse(txt);
        var f = doc["fields"] ?? throw new System.Exception("Documento sin 'fields'");

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

    // Panel Admin

    // Lista Partidas
    public async Task<List<(string uid, PartidaGuardada partida)>> ListarPartidasAsync(string idToken, int pageSize = 50)
    {
        var url = $"https://firestore.googleapis.com/v1/projects/{ajustes.projectId}/databases/(default)/documents:runQuery";
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
        foreach (var r in arr)
        {
            var doc = r["document"];
            if (doc == null) continue;
            var name = doc["name"]?.ToString();
            var uid = name?.Substring(name.LastIndexOf('/') + 1);
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

    // Elimina la partida de un uid (solo admin)
    public async Task EliminarPartidaAsync(string idToken, string uid)
    {
        var url = DocUrl("partidasGuardadas", uid);
        await EnviarJsonAsync(url, UnityWebRequest.kHttpVerbDELETE, null, idToken);
    }

    // HELPERS

    static string GetString(JToken parent, string name)
        => parent?[name]?["stringValue"]?.ToString() ?? "";

    static int GetInt(JToken parent, string name)
    {
        var t = parent?[name]?["integerValue"];
        if (t == null) return 0;
        int.TryParse(t.ToString(), out var v);
        return v;
    }

    static float GetFloat(JToken parent, string name)
    {
        var tD = parent?[name]?["doubleValue"];
        if (tD != null && (tD.Type == JTokenType.Float || tD.Type == JTokenType.Integer)) return (float)tD;

        var tS = parent?[name]?["stringValue"];
        if (tS != null && double.TryParse(tS.ToString(), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var dv)) return (float)dv;

        var tI = parent?[name]?["integerValue"];
        if (tI != null && long.TryParse(tI.ToString(), out var iv)) return (float)iv;

        return 0f;
    }

    static List<string> GetArrayStrings(JToken parent, string name)
    {
        var list = new List<string>();
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
