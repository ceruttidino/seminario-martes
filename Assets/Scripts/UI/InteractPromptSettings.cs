using TMPro;
using UnityEngine;

/// <summary>
/// Configuracion del cartel flotante de interaccion (la tecla "F" que aparece cerca de los pozos, las puertas con
/// candado de la tienda y los items de la tienda).
///
/// El asset vive en Assets/Resources/InteractPromptSettings: asi InteractPrompt lo carga solo y no hay que asignar
/// nada en cada puerta, pozo o slot. La fuente es la misma que usa el resto de la UI del juego (PixelifySans SDF).
/// </summary>
[CreateAssetMenu(fileName = "InteractPromptSettings", menuName = "UI/Interact Prompt Settings")]
public class InteractPromptSettings : ScriptableObject
{
    [Header("Texto")]
    [Tooltip("Fuente del cartel. Tiene que ser la misma del resto del juego.")]
    public TMP_FontAsset font;
    [Tooltip("Tamano de la letra (TextMeshPro de mundo: 10 de tamano ~ 1 unidad).")]
    public float fontSize = 5.3f;
    public Color textColor = Color.white;
    [Tooltip("Ajuste fino para centrar la letra dentro de la tecla (unidades de mundo).")]
    public Vector2 textOffset = new Vector2(0f, 0.01f);

    [Header("Tecla")]
    [Tooltip("Lado de la tecla en unidades de mundo.")]
    public float keySize = 0.6f;
    [Tooltip("Orden de dibujo: tiene que quedar por encima de jugador, enemigos e items.")]
    public int sortingOrder = 120;

    [Header("Animacion")]
    public float bobHeight = 0.05f;
    public float bobSpeed = 3.2f;
    public float fadeInSpeed = 9f;
    public float fadeOutSpeed = 7f;
}
