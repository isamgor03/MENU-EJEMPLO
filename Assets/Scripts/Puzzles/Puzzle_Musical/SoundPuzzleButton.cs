using System.Collections;
using UnityEngine;

// =====================================================================
//  SoundPuzzleButton
//  ---------------------------------------------------------------------
//  Va en cada objeto que el jugador golpea (campana, tambor, cubo...).
//  Se encarga de:
//    1. Detectar el golpe (clic, trigger de un arma, o llamada desde código).
//    2. Avisar al controlador de qué nota se ha golpeado.
//    3. Hacer el destello visual (color + escala) cuando suena su nota,
//       tanto si la toca el jugador como si la toca la secuencia.
// =====================================================================

[RequireComponent(typeof(Collider))] // Sin collider no se puede detectar el clic ni el trigger
public class SoundPuzzleButton : MonoBehaviour
{
    // ================== CÓMO SE ACTIVA ==================
    [Header("Cómo se activa")]
    [Tooltip("Si está activado, se puede golpear haciendo clic con el ratón sobre el objeto")]
    public bool hitByClick = true;

    [Tooltip("Si está activado, se golpea cuando un objeto con el tag indicado entra en su zona. El Collider de este objeto debe ser 'Is Trigger'")]
    public bool hitByTrigger = false;

    [Tooltip("Tag del objeto que activa el golpe (ej: 'Weapon' para una espada). Debe existir en Tags & Layers")]
    public string hitterTag = "Weapon";

    [Tooltip("Segundos mínimos entre dos golpes. Evita golpes dobles accidentales")]
    public float cooldown = 0.15f;

    // ================== FEEDBACK VISUAL ==================
    [Header("Feedback visual")]
    [Tooltip("Renderer cuyo color cambia al sonar. Si lo dejas vacío, se busca solo en este objeto o sus hijos")]
    public Renderer targetRenderer;

    [Tooltip("Color normal del objeto cuando no suena")]
    public Color idleColor = Color.white;

    [Tooltip("Color que toma el objeto mientras suena su nota")]
    public Color activeColor = Color.yellow;

    [Tooltip("Cuánto dura el destello, en segundos")]
    public float flashSeconds = 0.25f;

    [Range(1f, 1.5f), Tooltip("Cuánto crece el objeto durante el destello (1 = no cambia de tamaño)")]
    public float scaleMultiplier = 1.15f;

    // ================== VARIABLES INTERNAS ==================
    SoundPuzzleController controller;   // Controlador al que avisamos de los golpes
    int noteIndex;                      // Número de nota que representa este botón
    float lastHitTime = -999f;          // Momento del último golpe (para el cooldown)
    Vector3 baseScale;                  // Tamaño original, para restaurarlo tras el destello
    MaterialPropertyBlock block;        // Permite cambiar el color sin crear materiales nuevos
    Coroutine flashRoutine;             // Referencia al destello en curso

    // Dos nombres posibles de la propiedad de color según el pipeline de render:
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP / HDRP
    static readonly int ColorId = Shader.PropertyToID("_Color");         // Pipeline Built-in

    // ---------------------------------------------------------------------
    // Setup: lo llama el controlador al iniciarse para enlazar este botón
    // con su controlador y su número de nota. Tú no tienes que llamarlo.
    // ---------------------------------------------------------------------
    public void Setup(SoundPuzzleController owner, int index)
    {
        controller = owner;
        noteIndex = index;
    }

    // ---------------------------------------------------------------------
    // Awake: guarda el tamaño original, localiza el Renderer y pone el color de reposo.
    // ---------------------------------------------------------------------
    void Awake()
    {
        baseScale = transform.localScale;
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        block = new MaterialPropertyBlock();
        SetColor(idleColor);
    }

    // ---------------------------------------------------------------------
    // OnMouseDown: Unity lo llama al hacer clic sobre el collider.
    // Requiere el Input Manager antiguo (Player Settings > Active Input Handling: Old o Both).
    // ---------------------------------------------------------------------
    void OnMouseDown()
    {
        if (hitByClick) Hit();
    }

    // ---------------------------------------------------------------------
    // OnTriggerEnter: Unity lo llama cuando otro collider entra en la zona trigger.
    // Sirve para golpes con espada, proyectiles, bumerán, etc.
    // ---------------------------------------------------------------------
    void OnTriggerEnter(Collider other)
    {
        if (hitByTrigger && other.tag == hitterTag) Hit();
    }

    // ---------------------------------------------------------------------
    // Hit: el golpe en sí. Es pública, así que puedes llamarla desde tu script de
    // ataque, un raycast o un UnityEvent. Avisa al controlador de qué nota se tocó.
    // ---------------------------------------------------------------------
    public void Hit()
    {
        if (controller == null || Time.time - lastHitTime < cooldown) return; // Sin controlador o demasiado pronto
        lastHitTime = Time.time;
        controller.PlayerHit(noteIndex);
    }

    // ---------------------------------------------------------------------
    // Flash: lo llama el controlador cuando suena esta nota, para que el objeto
    // se ilumine y crezca un momento. Si ya había un destello, lo reinicia.
    // ---------------------------------------------------------------------
    public void Flash()
    {
        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashRoutine());
    }

    // Corrutina del destello: aplica el color y la escala activos, espera y los restaura.
    IEnumerator FlashRoutine()
    {
        SetColor(activeColor);
        transform.localScale = baseScale * scaleMultiplier;
        yield return new WaitForSeconds(flashSeconds);
        SetColor(idleColor);
        transform.localScale = baseScale;
        flashRoutine = null;
    }

    // Cambia el color del objeto con un MaterialPropertyBlock
    // (así no se duplican materiales y no afecta a otros objetos que compartan el suyo).
    void SetColor(Color c)
    {
        if (targetRenderer == null) return;
        targetRenderer.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        block.SetColor(ColorId, c);
        targetRenderer.SetPropertyBlock(block);
    }
}