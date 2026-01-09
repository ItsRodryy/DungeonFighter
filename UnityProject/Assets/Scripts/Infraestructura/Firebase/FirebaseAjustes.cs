using UnityEngine;

// Guardamos el apiKey y el projectId del proyecto de Firebase en un ScriptableObject
// Así no los dejamos hardcodeados en los scripts y podemos cambiarlos desde el inspector

[CreateAssetMenu(fileName = "FirebaseAjustes", menuName = "Config/FirebaseAjustes")]
public class FirebaseAjustes : ScriptableObject
{
    // Guardamos la apiKey de Firebase que usamos para llamar a FirebaseAuth por REST
    [Tooltip("apiKey de Firebase")]
    public string apiKey;

    // Guardamos el projectId de Firebase que usamos para construir las URLs de Firestore REST
    [Tooltip("ID proyecto de Firebase")]
    public string projectId;
}
