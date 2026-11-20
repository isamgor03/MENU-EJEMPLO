using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  Flammable.cs  (PUZZLE 2: FUEGO, LLUVIA Y CUERDAS)
//  ---------------------------------------------------------------------
//  Convierte cualquier objeto en INFLAMABLE: cuerdas, hierba seca, un
//  puente de madera, una antorcha, un barril...
//
//  Estados:
//    Idle     -> sin fuego (estado normal)
//    Burning  -> ardiendo. Cada "spreadInterval" segundos contagia el fuego
//                a los inflamables cercanos. Tras "burnDuration" segundos
//                se consume.
//    Burnt    -> quemado. No vuelve a arder (salvo canReignite).
//
//  La lluvia (RainController) llama a Wet(): apaga el fuego y lo deja
//  "mojado" unos segundos, tiempo durante el cual no se puede encender.
//
//  No necesita Collider: la propagación se calcula por distancia entre
//  objetos inflamables (todos se registran en la lista estática "All").
// =====================================================================
public class Flammable : MonoBehaviour
{
    public enum FireState { Idle, Burning, Burnt }

    // Registro de todos los inflamables activos (lo usan la propagación, la lluvia y el encendedor)
    public static readonly List<Flammable> All = new List<Flammable>();
    static readonly List<Flammable> scratch = new List<Flammable>(); // Copia temporal para recorrer sin errores

    // ================== FUEGO ==================
    [Header("Fuego")]
    [Min(0f), Tooltip("Segundos que arde antes de quemarse del todo. 0 = arde para siempre (antorchas)")]
    public float burnDuration = 4f;

    [Tooltip("Si está activado, empieza ya encendido (ej: una antorcha eterna que es fuente de fuego)")]
    public bool startBurning = false;

    [Tooltip("Si está activado, un objeto ya quemado puede volver a encenderse")]
    public bool canReignite = false;

    // ================== PROPAGACIÓN ==================
    [Header("Propagación")]
    [Tooltip("Si está activado, este objeto contagia el fuego a otros mientras arde")]
    public bool spreads = true;

    [Min(0.05f), Tooltip("Cada cuántos segundos intenta contagiar el fuego")]
    public float spreadInterval = 1f;

    [Min(0f), Tooltip("Radio (metros) dentro del cual contagia a otros inflamables. 0 = solo a los vecinos de la lista")]
    public float spreadRadius = 1.5f;

    [Range(0f, 1f), Tooltip("Probabilidad de contagiar en cada intento (1 = siempre)")]
    public float spreadChance = 1f;

    [Tooltip("Vecinos que SIEMPRE se contagian, aunque estén lejos (útil para enlazar cuerdas concretas)")]
    public List<Flammable> linkedNeighbors = new List<Flammable>();

    // ================== LLUVIA ==================
    [Header("Lluvia")]
    [Tooltip("Si está activado, este objeto está a cubierto y la lluvia NO le afecta (ej: antorcha bajo un tejado)")]
    public bool sheltered = false;

    [Min(0f), Tooltip("Segundos que tarda en secarse tras mojarse. Mientras está mojado no se puede encender")]
    public float wetSeconds = 5f;

    // ================== VISUAL ==================
    [Header("Visual")]
    [Tooltip("GameObject con partículas de fuego (hijo del objeto). Se activa al arder y se apaga al terminar")]
    public GameObject fireEffect;

    [Tooltip("Renderer que se ennegrece al quemarse. Si lo dejas vacío se busca automáticamente")]
    public Renderer targetRenderer;

    public Color normalColor = Color.white;                       // Color normal
    public Color burntColor = new Color(0.12f, 0.1f, 0.08f);      // Color carbonizado

    [Tooltip("Si está activado, al quemarse del todo el objeto desaparece (ideal para cuerdas)")]
    public bool hideWhenBurnt = false;

    // ================== EVENTOS ==================
    [Header("Eventos")]
    public UnityEvent onIgnite;      // Empieza a arder
    public UnityEvent onExtinguish;  // Se apaga (lluvia, agua...)
    public UnityEvent onBurnt;       // Se quema del todo

    // Avisos para otros scripts (el FirePuzzleManager se suscribe a estos)
    public event System.Action<Flammable> Ignited;
    public event System.Action<Flammable> Extinguished;
    public event System.Action<Flammable> BecameBurnt;

    [Header("Debug (solo lectura)")]
    [SerializeField] FireState state = FireState.Idle;

    float burnTimer;    // Tiempo que lleva ardiendo
    float spreadTimer;  // Tiempo desde el último intento de contagio
    float wetUntil;     // Momento hasta el que sigue mojado

    public FireState CurrentState => state;
    public bool IsBurning => state == FireState.Burning;
    public bool IsBurnt => state == FireState.Burnt;
    public bool IsWet => Time.time < wetUntil;

    // ¿Se podría encender ahora mismo?
    public bool CanIgnite => state != FireState.Burning && !(state == FireState.Burnt && !canReignite) && !IsWet;

    // ---------- Ciclo de vida ----------

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake()
    {
        if (targetRenderer == null) targetRenderer = FindBodyRenderer();
        if (fireEffect != null) fireEffect.SetActive(false);
        PuzzleTint.SetColor(targetRenderer, normalColor);
    }

    void Start()
    {
        if (startBurning) TryIgnite();
    }

    // Mientras arde: cuenta el tiempo, contagia y finalmente se consume.
    void Update()
    {
        if (state != FireState.Burning) return;

        burnTimer += Time.deltaTime;

        // Contagio periódico
        if (spreads)
        {
            spreadTimer += Time.deltaTime;
            if (spreadTimer >= spreadInterval)
            {
                spreadTimer = 0f;
                Spread();
            }
        }

        // Se va ennegreciendo y al final se consume
        if (burnDuration > 0f)
        {
            PuzzleTint.SetColor(targetRenderer, Color.Lerp(normalColor, burntColor, burnTimer / burnDuration));
            if (burnTimer >= burnDuration) BecomeBurnt();
        }
    }

    // ---------- API pública ----------

    // Versión sin valor de retorno para poder usarla desde UnityEvents del Inspector.
    public void Ignite() { TryIgnite(); }

    // Intenta encender el objeto. Devuelve true si realmente se encendió.
    public bool TryIgnite()
    {
        if (!CanIgnite) return false;

        state = FireState.Burning;
        burnTimer = 0f;
        spreadTimer = 0f;
        if (fireEffect != null) fireEffect.SetActive(true);

        onIgnite.Invoke();
        if (Ignited != null) Ignited(this);
        return true;
    }

    // Apaga el fuego (vuelve a Idle, se puede volver a encender).
    public void Extinguish()
    {
        if (state != FireState.Burning) return;

        state = FireState.Idle;
        burnTimer = 0f;
        if (fireEffect != null) fireEffect.SetActive(false);
        PuzzleTint.SetColor(targetRenderer, normalColor);

        onExtinguish.Invoke();
        if (Extinguished != null) Extinguished(this);
    }

    // Moja el objeto: apaga el fuego y lo mantiene húmedo "wetSeconds" segundos.
    public void Wet()
    {
        wetUntil = Time.time + wetSeconds;
        Extinguish();
    }

    // Devuelve el objeto a su estado original (lo usa el manager al reiniciar el puzzle).
    public void ResetState()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true); // Recupera cuerdas ocultas

        state = FireState.Idle;
        burnTimer = 0f;
        spreadTimer = 0f;
        wetUntil = 0f;
        if (fireEffect != null) fireEffect.SetActive(false);
        PuzzleTint.SetColor(targetRenderer, normalColor);

        if (startBurning) TryIgnite();
    }

    // ---------- Lógica interna ----------

    // Contagia a los vecinos enlazados y a los que estén dentro del radio.
    void Spread()
    {
        // 1) Vecinos enlazados a mano
        foreach (Flammable n in linkedNeighbors)
            if (n != null) TrySpreadTo(n);

        // 2) Vecinos por distancia (se recorre una copia de la lista por seguridad)
        if (spreadRadius > 0f)
        {
            scratch.Clear();
            scratch.AddRange(All);
            float sqrRadius = spreadRadius * spreadRadius;

            foreach (Flammable other in scratch)
            {
                if (other == null || other == this) continue;
                if ((other.transform.position - transform.position).sqrMagnitude <= sqrRadius)
                    TrySpreadTo(other);
            }
        }
    }

    void TrySpreadTo(Flammable other)
    {
        if (Random.value <= spreadChance) other.TryIgnite();
    }

    // El objeto se consume del todo.
    void BecomeBurnt()
    {
        state = FireState.Burnt;
        if (fireEffect != null) fireEffect.SetActive(false);
        PuzzleTint.SetColor(targetRenderer, burntColor);

        onBurnt.Invoke();
        if (BecameBurnt != null) BecameBurnt(this);

        if (hideWhenBurnt) gameObject.SetActive(false);
    }

    // Busca el renderer del objeto ignorando el de las partículas de fuego.
    Renderer FindBodyRenderer()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer) continue;
            return r;
        }
        return null;
    }

    // Dibuja el radio de contagio y los enlaces en la ventana Scene.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f);
        if (spreadRadius > 0f) Gizmos.DrawWireSphere(transform.position, spreadRadius);

        Gizmos.color = Color.yellow;
        foreach (Flammable n in linkedNeighbors)
            if (n != null) Gizmos.DrawLine(transform.position, n.transform.position);
    }
}
