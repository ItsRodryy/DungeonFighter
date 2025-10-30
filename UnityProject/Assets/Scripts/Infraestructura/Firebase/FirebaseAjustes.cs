using UnityEngine;

// Guarda apiKey y projectId de tu proyecto Firebase.
// As� no las quemo en c�digo y las puedo cambiar f�cil en el inspector.
[CreateAssetMenu(fileName = "FirebaseAjustes", menuName = "Config/FirebaseAjustes")]
public class FirebaseAjustes : ScriptableObject
{
    [Tooltip("apiKey de Firebase")]
    public string apiKey;

    [Tooltip("ID proyecto de Firebase")]
    public string projectId;
}
