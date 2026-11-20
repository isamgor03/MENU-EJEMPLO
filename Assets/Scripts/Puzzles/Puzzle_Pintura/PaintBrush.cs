using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  PaintBrush.cs  (PUZZLE 1: PINTURA Y COLORES)
//  ---------------------------------------------------------------------
//  Va en el JUGADOR. Es su "pincel / cañón de pintura".
//    - E (cerca de un PaintSource): carga ese tinte. Si ya llevas otro
//      color, se mezclan según la tabla de PaintColors.cs.
//    - Clic izquierdo: dispara un rayo hacia donde apunta la cámara; si
//      golpea un PaintReceiver, lo pinta con el color que lleves.
//  La pintura es limitada: tras "shotsPerLoad" disparos te quedas sin color.
//
//  Si ya tienes tu propio control del jugador, desactiva "useBuiltInInput"
//  y llama tú a TryLoadNearest() y Fire() desde tu script.
// =====================================================================
public class PaintBrush : MonoBehaviour
{
    [Header("Cargar tinte")]
    [Min(0.5f), Tooltip("Distancia máxima a una fuente de tinte para poder cogerlo")]
    public float interactRange = 2.5f;

    [Tooltip("Si está activado, un tinte nuevo se MEZCLA con el que ya llevas. Si no, lo reemplaza")]
    public bool mixOnPickup = true;

    [Min(0), Tooltip("Disparos que da cada carga. 0 = infinitos")]
    public int shotsPerLoad = 3;

    [Header("Disparo")]
    [Tooltip("Desde dónde sale el rayo. Si lo dejas vacío usa la cámara principal (MainCamera)")]
    public Transform aimOrigin;

    [Min(1f), Tooltip("Alcance máximo del disparo en metros")]
    public float maxDistance = 30f;

    [Tooltip("Capas que puede golpear el disparo. Quita la capa del jugador para no dispararte a ti")]
    public LayerMask layerMask = ~0;

    [Min(0f), Tooltip("Segundos mínimos entre disparos")]
    public float shotCooldown = 0.25f;

    [Header("Feedback")]
    [Tooltip("Objeto que muestra el color que llevas (la punta del pincel, un frasco, etc.)")]
    public Renderer colorIndicator;

    [Tooltip("Prefab con partículas que aparece al impactar (se tiñe del color de la pintura). Opcional")]
    public GameObject hitEffectPrefab;

    [Header("Entrada")]
    [Tooltip("Desactívalo si controlas el pincel desde tu propio script")]
    public bool useBuiltInInput = true;

    [Header("Eventos")]
    public UnityEvent onColorChanged;   // Cambia el color que llevas
    public UnityEvent onShoot;          // Disparas
    public UnityEvent onNoPaint;        // Intentas disparar sin pintura

    [Header("Debug (solo lectura)")]
    [SerializeField] PuzzleColor currentColor = PuzzleColor.None;
    [SerializeField] int shotsLeft;

    float lastShotTime = -99f;

    public PuzzleColor CurrentColor => currentColor;

    void Start() { RefreshIndicator(); }

    // Cada frame lee las teclas (si la entrada integrada está activada).
    void Update()
    {
        if (!useBuiltInInput) return;
        if (PuzzleInput.InteractDown()) TryLoadNearest();
        if (PuzzleInput.FireDown()) Fire();
    }

    // Busca la fuente de tinte más cercana dentro del alcance y carga su color.
    public bool TryLoadNearest()
    {
        PaintSource best = null;
        float bestDist = interactRange;

        foreach (PaintSource s in PaintSource.All)
        {
            float d = Vector3.Distance(transform.position, s.transform.position);
            if (d <= bestDist) { bestDist = d; best = s; }
        }

        if (best == null) return false;
        LoadDye(best.color);
        return true;
    }

    // Carga un color en el pincel (mezclándolo con el actual si mixOnPickup está activo).
    public void LoadDye(PuzzleColor dye)
    {
        if (dye == PuzzleColor.None)
        {
            Clear();            // "Agua": limpia el pincel
            return;
        }

        currentColor = mixOnPickup ? PaintMixer.Mix(currentColor, dye) : dye;
        shotsLeft = shotsPerLoad;
        RefreshIndicator();
        onColorChanged.Invoke();
    }

    // Vacía el pincel.
    public void Clear()
    {
        currentColor = PuzzleColor.None;
        shotsLeft = 0;
        RefreshIndicator();
        onColorChanged.Invoke();
    }

    // Dispara un rayo y pinta lo que golpee.
    public void Fire()
    {
        if (Time.time - lastShotTime < shotCooldown) return;

        if (currentColor == PuzzleColor.None)
        {
            onNoPaint.Invoke();
            return;
        }

        lastShotTime = Time.time;
        Transform aim = GetAim();

        if (Physics.Raycast(aim.position, aim.forward, out RaycastHit hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            // El receptor puede estar en el objeto golpeado o en uno de sus padres
            PaintReceiver receiver = hit.collider.GetComponentInParent<PaintReceiver>();
            if (receiver != null) receiver.ApplyPaint(currentColor);

            SpawnHitEffect(hit.point + hit.normal * 0.02f, hit.normal);
        }

        onShoot.Invoke();
        ConsumeShot();
    }

    // Resta un disparo y vacía el pincel si se acaba la carga.
    void ConsumeShot()
    {
        if (shotsPerLoad <= 0) return; // Infinitos
        shotsLeft--;
        if (shotsLeft <= 0) Clear();
    }

    // Crea las partículas del impacto con el color de la pintura.
    void SpawnHitEffect(Vector3 point, Vector3 normal)
    {
        if (hitEffectPrefab == null) return;

        GameObject fx = Instantiate(hitEffectPrefab, point, Quaternion.LookRotation(normal));
        Color c = PaintMixer.ToColor(currentColor);
        foreach (ParticleSystem ps in fx.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = ps.main;
            main.startColor = c;
        }
        Destroy(fx, 3f);
    }

    // Origen del disparo: el que asignes, la cámara principal, o el propio jugador.
    Transform GetAim()
    {
        if (aimOrigin != null) return aimOrigin;
        if (Camera.main != null) return Camera.main.transform;
        return transform;
    }

    // Actualiza el color del indicador visual.
    void RefreshIndicator()
    {
        PuzzleTint.SetColor(colorIndicator, PaintMixer.ToColor(currentColor));
    }

    // Muestra el alcance de interacción en la ventana Scene.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
