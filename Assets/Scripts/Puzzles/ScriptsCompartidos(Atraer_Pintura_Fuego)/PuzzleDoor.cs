using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  PuzzleDoor.cs  (COMPARTIDO por los 3 puzzles)
//  ---------------------------------------------------------------------
//  Una puerta/compuerta/reja genérica que se desliza al abrirse.
//  Es la "recompensa" típica: conectas el evento "al completar el puzzle"
//  de cualquier manager a PuzzleDoor.Open() y listo, sin animaciones.
//  Si prefieres usar un Animator, ignora este script y usa tus eventos.
// =====================================================================
public class PuzzleDoor : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Cuánto se desplaza la puerta al abrirse, en coordenadas LOCALES (ej: 0,3,0 = sube 3 metros)")]
    public Vector3 openOffset = new Vector3(0f, 3f, 0f);

    [Min(0.01f), Tooltip("Segundos que tarda en abrirse o cerrarse")]
    public float moveSeconds = 1.5f;

    [Tooltip("Curva de suavizado del movimiento")]
    public AnimationCurve ease = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Sonido (opcional)")]
    public AudioClip openClip;
    public AudioClip closeClip;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Estado inicial")]
    [Tooltip("Si está activado, la puerta empieza abierta")]
    public bool startOpen = false;

    [Header("Eventos")]
    public UnityEvent onOpened;
    public UnityEvent onClosed;

    Vector3 closedPos;   // Posición local cerrada
    Vector3 openPos;     // Posición local abierta
    Coroutine routine;   // Movimiento en curso

    public bool IsOpen { get; private set; }

    // Guarda las dos posiciones a partir de donde pongas la puerta en la escena.
    void Awake()
    {
        closedPos = transform.localPosition;
        openPos = closedPos + openOffset;

        if (startOpen)
        {
            transform.localPosition = openPos;
            IsOpen = true;
        }
    }

    // Abre la puerta (conéctala a los eventos de los puzzles)
    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        if (openClip != null) AudioSource.PlayClipAtPoint(openClip, transform.position, volume);
        MoveTo(openPos, onOpened);
    }

    // Cierra la puerta
    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        if (closeClip != null) AudioSource.PlayClipAtPoint(closeClip, transform.position, volume);
        MoveTo(closedPos, onClosed);
    }

    // Alterna entre abierta y cerrada
    public void Toggle()
    {
        if (IsOpen) Close(); else Open();
    }

    void MoveTo(Vector3 target, UnityEvent finishedEvent)
    {
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(MoveRoutine(target, finishedEvent));
    }

    // Mueve la puerta suavemente desde donde esté hasta el destino
    IEnumerator MoveRoutine(Vector3 target, UnityEvent finishedEvent)
    {
        Vector3 from = transform.localPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / moveSeconds;
            transform.localPosition = Vector3.LerpUnclamped(from, target, ease.Evaluate(Mathf.Clamp01(t)));
            yield return null;
        }
        transform.localPosition = target;
        routine = null;
        finishedEvent.Invoke();
    }
}
