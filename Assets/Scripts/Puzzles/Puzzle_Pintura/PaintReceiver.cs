using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  PaintReceiver.cs  (PUZZLE 1: PINTURA Y COLORES)
//  ---------------------------------------------------------------------
//  Va en cada ESTATUA / VELA / PUERTA / CRISTAL que se pinta.
//  Guarda el color que tiene ahora y lo compara con el color que NECESITA.
//    - Si coinciden: lanza "onMatched" (puedes encender una luz, etc.)
//    - Si deja de coincidir: lanza "onUnmatched".
//  Necesita un Collider en el objeto (o en un hijo) para recibir disparos.
//
//  Opciones para dar dificultad:
//    - lockWhenCorrect: queda fijado al acertar (no se puede estropear).
//    - fadeBackSeconds: la pintura se desvanece pasado un tiempo, así que
//      hay que acertar todos los objetos "a la vez" antes de que se borren.
//    - mixWithCurrent: la pintura nueva se mezcla con la que ya tiene.
// =====================================================================
public class PaintReceiver : MonoBehaviour
{
    [Header("Objetivo")]
    [Tooltip("Color que este objeto necesita para considerarse correcto")]
    public PuzzleColor requiredColor = PuzzleColor.Red;

    [Header("Comportamiento")]
    [Tooltip("Si está activado, la pintura nueva se mezcla con la que ya tiene. Si no, la reemplaza")]
    public bool mixWithCurrent = false;

    [Tooltip("Si está activado, al acertar el color queda bloqueado y ya no cambia")]
    public bool lockWhenCorrect = false;

    [Min(0f), Tooltip("Segundos que dura la pintura antes de borrarse sola. 0 = permanente")]
    public float fadeBackSeconds = 0f;

    [Header("Visual")]
    [Tooltip("Renderer que se pinta. Si lo dejas vacío se busca automáticamente")]
    public Renderer targetRenderer;

    [Tooltip("Color del objeto sin pintar")]
    public Color unpaintedColor = new Color(0.85f, 0.85f, 0.85f);

    [Tooltip("Opcional: un objeto pequeño (gema, corona) que muestra el color que hace falta como pista. Debe ser DISTINTO al Target Renderer")]
    public Renderer hintRenderer;

    [Header("Eventos")]
    public UnityEvent onPainted;     // Cada vez que recibe pintura
    public UnityEvent onMatched;     // Cuando pasa a tener el color correcto
    public UnityEvent onUnmatched;   // Cuando deja de tener el color correcto

    [Header("Debug (solo lectura)")]
    [SerializeField] PuzzleColor currentColor = PuzzleColor.None;

    // Aviso interno para el manager (se lanza cada vez que cambia el estado)
    public System.Action<PaintReceiver> StateChanged;

    bool locked;       // ¿Bloqueado en el color correcto?
    float paintTime;   // Momento en que se pintó por última vez

    public PuzzleColor CurrentColor => currentColor;
    public bool IsMatched => requiredColor != PuzzleColor.None && currentColor == requiredColor;

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
    }

    void Start() { Refresh(); }

    // Gestiona el desvanecimiento de la pintura.
    void Update()
    {
        if (fadeBackSeconds > 0f && !locked && currentColor != PuzzleColor.None &&
            Time.time - paintTime >= fadeBackSeconds)
        {
            SetCurrent(PuzzleColor.None);
        }
    }

    // El pincel llama a esto cuando el disparo golpea este objeto.
    public void ApplyPaint(PuzzleColor paint)
    {
        if (locked || paint == PuzzleColor.None) return;

        onPainted.Invoke();
        SetCurrent(mixWithCurrent ? PaintMixer.Mix(currentColor, paint) : paint);
    }

    // Quita la pintura (por ejemplo, desde la lluvia o un charco). Respeta el bloqueo.
    public void ClearPaint()
    {
        if (locked || currentColor == PuzzleColor.None) return;
        SetCurrent(PuzzleColor.None);
    }

    // Bloquea el objeto en su estado actual (lo usa el manager al completar el puzzle).
    public void Lock() { locked = true; }

    // Desbloquea y deja el objeto sin pintar (lo usa el reinicio del puzzle).
    public void ResetReceiver()
    {
        locked = false;
        SetCurrent(PuzzleColor.None);
    }

    // Cambia el color actual y lanza los eventos que correspondan.
    void SetCurrent(PuzzleColor newColor)
    {
        bool wasMatched = IsMatched;

        currentColor = newColor;
        paintTime = Time.time;
        Refresh();

        bool nowMatched = IsMatched;
        if (!wasMatched && nowMatched)
        {
            onMatched.Invoke();
            if (lockWhenCorrect) locked = true;
        }
        else if (wasMatched && !nowMatched)
        {
            onUnmatched.Invoke();
        }

        if (StateChanged != null) StateChanged(this);
    }

    // Actualiza los colores visibles del objeto y de la pista.
    void Refresh()
    {
        Color shown = currentColor == PuzzleColor.None ? unpaintedColor : PaintMixer.ToColor(currentColor);
        PuzzleTint.SetColor(targetRenderer, shown);

        if (hintRenderer != null)
            PuzzleTint.SetColor(hintRenderer, PaintMixer.ToColor(requiredColor));
    }
}
