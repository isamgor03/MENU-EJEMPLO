using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  LureThrower.cs  (PUZZLE 3: ATENCIÓN DE ANIMALES / NPC / ENEMIGOS)
//  ---------------------------------------------------------------------
//  Va en el JUGADOR. Es lo que lanza los cebos.
//    - Clic izquierdo: lanza el cebo seleccionado.
//    - Tecla Q: cambia entre los cebos de la lista (si hay varios).
//
//  Dos modos de lanzamiento:
//    Physics          -> el cebo es un objeto físico que vuela en arco y
//                        aterriza (más natural, hay que apuntar bien).
//    PlaceAtAimPoint  -> el cebo aparece directamente donde apunta la
//                        cámara (preciso y sin sorpresas; ideal para
//                        probar o si no quieres lanzamientos físicos).
//
//  Si tienes tu propio control del jugador, desactiva "useBuiltInInput"
//  y llama tú a Throw() y SelectLure().
// =====================================================================
public class LureThrower : MonoBehaviour
{
    public enum ThrowMode { Physics, PlaceAtAimPoint }

    // ================== QUÉ LANZA ==================
    [Header("Qué lanza")]
    [Tooltip("Prefabs de cebo (cada uno con LureItem, Rigidbody y Collider). Q cambia entre ellos")]
    public List<LureItem> lurePrefabs = new List<LureItem>();

    [SerializeField, Tooltip("Cebo seleccionado ahora (posición en la lista)")]
    int currentIndex = 0;

    // ================== CÓMO LANZA ==================
    [Header("Cómo lanza")]
    public ThrowMode mode = ThrowMode.Physics;

    [Tooltip("Dirección de apuntado. Si lo dejas vacío usa la cámara principal (MainCamera)")]
    public Transform aimOrigin;

    [Tooltip("Desde dónde sale el cebo (la mano). Si lo dejas vacío sale desde delante de la cámara")]
    public Transform throwPoint;

    [Min(0f), Tooltip("(Physics) Velocidad de salida hacia delante")]
    public float throwForce = 12f;

    [Min(0f), Tooltip("(Physics) Velocidad extra hacia arriba para que haga arco")]
    public float upwardForce = 3f;

    [Min(1f), Tooltip("(PlaceAtAimPoint) Distancia máxima a la que puedes colocar el cebo")]
    public float maxPlaceDistance = 25f;

    [Tooltip("(PlaceAtAimPoint) Capas donde puede caer el cebo. Quita la capa del jugador")]
    public LayerMask aimMask = ~0;

    [Min(0f), Tooltip("Segundos mínimos entre lanzamientos")]
    public float cooldown = 0.5f;

    // ================== MUNICIÓN ==================
    [Header("Munición")]
    [Tooltip("Si está activado, nunca se acaban los cebos")]
    public bool infiniteAmmo = false;

    [Min(0), Tooltip("Cebos disponibles (si la munición no es infinita)")]
    public int ammo = 5;

    // ================== ENTRADA Y EVENTOS ==================
    [Header("Entrada")]
    [Tooltip("Desactívalo si controlas el lanzador desde tu propio script")]
    public bool useBuiltInInput = true;

    [Header("Eventos")]
    public UnityEvent onThrow;
    public UnityEvent onOutOfAmmo;
    public UnityEvent onLureChanged;

    float lastThrowTime = -99f;

    public LureItem CurrentLure =>
        (lurePrefabs.Count > 0) ? lurePrefabs[Mathf.Clamp(currentIndex, 0, lurePrefabs.Count - 1)] : null;

    void Update()
    {
        if (!useBuiltInInput) return;
        if (PuzzleInput.CycleDown()) SelectNext();
        if (PuzzleInput.FireDown()) Throw();
    }

    // Selecciona un cebo concreto de la lista (útil desde un menú o botones de UI).
    public void SelectLure(int index)
    {
        if (lurePrefabs.Count == 0) return;
        currentIndex = Mathf.Clamp(index, 0, lurePrefabs.Count - 1);
        onLureChanged.Invoke();
    }

    // Pasa al siguiente cebo de la lista (vuelve al primero al llegar al final).
    public void SelectNext()
    {
        if (lurePrefabs.Count < 2) return;
        SelectLure((currentIndex + 1) % lurePrefabs.Count);
    }

    // Añade cebos (para recoger munición en el mundo).
    public void AddAmmo(int amount) { ammo += amount; }

    // Lanza el cebo seleccionado.
    public void Throw()
    {
        if (Time.time - lastThrowTime < cooldown) return;

        LureItem prefab = CurrentLure;
        if (prefab == null)
        {
            Debug.LogWarning("LureThrower: la lista 'lurePrefabs' está vacía.", this);
            return;
        }

        if (!infiniteAmmo && ammo <= 0)
        {
            onOutOfAmmo.Invoke();
            return;
        }

        Transform aim = GetAim();

        if (mode == ThrowMode.Physics)
        {
            // Sale de la mano (o delante de la cámara) y se lanza con fuerza hacia delante y arriba
            Vector3 pos = throwPoint != null ? throwPoint.position : aim.position + aim.forward * 0.8f;
            LureItem item = Instantiate(prefab, pos, Quaternion.identity);

            Rigidbody rb = item.GetComponent<Rigidbody>();
            if (rb == null) rb = item.gameObject.AddComponent<Rigidbody>();

            IgnoreOwnColliders(item);
            // VelocityChange ignora la masa: el lanzamiento es igual de fuerte con cualquier peso
            rb.AddForce(aim.forward * throwForce + Vector3.up * upwardForce, ForceMode.VelocityChange);
        }
        else
        {
            // Raycast hacia donde apunta la cámara y colocamos el cebo en el punto de impacto
            if (!Physics.Raycast(aim.position, aim.forward, out RaycastHit hit, maxPlaceDistance, aimMask, QueryTriggerInteraction.Ignore))
                return;

            LureItem placed = Instantiate(prefab, hit.point + hit.normal * 0.1f, Quaternion.identity);
            placed.ActivateNow();
        }

        if (!infiniteAmmo) ammo--;
        lastThrowTime = Time.time;
        onThrow.Invoke();
    }

    // Evita que el cebo choque con el propio jugador al salir (se activaría en su pie).
    void IgnoreOwnColliders(LureItem item)
    {
        Collider[] mine = GetComponentsInChildren<Collider>();
        foreach (Collider itemCol in item.GetComponentsInChildren<Collider>())
            foreach (Collider m in mine)
                Physics.IgnoreCollision(itemCol, m);
    }

    Transform GetAim()
    {
        if (aimOrigin != null) return aimOrigin;
        if (Camera.main != null) return Camera.main.transform;
        return transform;
    }
}
