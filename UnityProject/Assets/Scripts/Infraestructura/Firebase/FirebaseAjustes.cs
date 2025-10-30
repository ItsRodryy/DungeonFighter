using UnityEngine;

// Guarda apiKey y projectId de tu proyecto Firebase.
// Así no las quemo en código y las puedo cambiar fácil en el inspector.
[CreateAssetMenu(fileName = "FirebaseAjustes", menuName = "Config/FirebaseAjustes")]
public class FirebaseAjustes : ScriptableObject
{
    [Tooltip("apiKey de Firebase")]
    public string apiKey;

    [Tooltip("ID proyecto de Firebase")]
    public string projectId;
}
