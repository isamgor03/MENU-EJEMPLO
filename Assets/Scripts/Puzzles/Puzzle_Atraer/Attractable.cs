using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

// =====================================================================
//  Attractable.cs  (PUZZLE 3: ATENCIÓN DE ANIMALES / NPC / ENEMIGOS)
//  ---------------------------------------------------------------------
//  La IA del animal / NPC / enemigo que REACCIONA a los cebos.
//  Va en cada criatura. Funciona así:
//
//    PATRULLA   -> Va de punto en punto (o se queda en su sitio).
//    DETECTA    -> Si hay un LureItem activo, de un tipo que le interese
//                  y dentro de su radio de percepción, corre hacia él.
//    COME       -> Al llegar, se queda "eatSeconds" segundos interactuando
//                  (come, investiga...). Al terminar se consume el cebo.
//    VUELVE     -> Reanuda la patrulla o regresa a su punto de origen.
//
//  Dos formas de moverse (Move Mode):
//    NavMesh -> usa un NavMeshAgent (rodea obstáculos; requiere NavMesh
//               horneado y un NavMeshAgent en el objeto).
//    Direct  -> se mueve en línea recta por transform (ideal para aves
//               voladoras o para probar rápido; ignora obstáculos).
//
//  Los eventos permiten conectar cualquier reacción: animaciones, sonidos,
//  que se enfade, que abra algo, etc.
// =====================================================================
public class Attractable : MonoBehaviour
{
    public enum MoveMode { NavMesh, Direct }
    public enum State { Patrol, GoingToLure, Eating }

    // Registro de todas las criaturas (los interruptores de presión lo consultan)
    public static readonly List<Attractable> All = new List<Attractable>();

    // ================== PERCEPCIÓN ==================
    [Header("Percepción")]
    [Tooltip("Tipos de cebo que le interesan a esta criatura")]
    public List<LureKind> acceptedKinds = new List<LureKind> { LureKind.Food };

    [Min(0f), Tooltip("Distancia máxima a la que nota un cebo")]
    public float perceptionRadius = 15f;

    [Min(0.05f), Tooltip("Cada cuántos segundos busca cebos (más alto = menos coste de CPU)")]
    public float checkInterval = 0.25f;

    // ================== MOVIMIENTO ==================
    [Header("Movimiento")]
    public MoveMode moveMode = MoveMode.NavMesh;

    [Min(0f), Tooltip("Velocidad al patrullar")]
    public float walkSpeed = 2f;

    [Min(0f), Tooltip("Velocidad al ir hacia un cebo")]
    public float runSpeed = 3.5f;

    [Min(0f), Tooltip("(Direct) Velocidad de giro en grados por segundo")]
    public float turnSpeed = 360f;

    [Min(0.1f), Tooltip("Distancia a la que se considera que ha llegado a su destino")]
    public float arriveDistance = 0.8f;

    [Min(0.1f), Tooltip("(NavMesh) Distancia máxima para buscar el punto del NavMesh más cercano al cebo")]
    public float navSampleDistance = 2f;

    // ================== PATRULLA ==================
    [Header("Patrulla")]
    [Tooltip("Puntos de patrulla (objetos vacíos en la escena). Si está vacío, se queda en su sitio inicial")]
    public List<Transform> patrolPoints = new List<Transform>();

    [Min(0f), Tooltip("Segundos que espera en cada punto de patrulla")]
    public float patrolWait = 2f;

    // ================== INTERACCIÓN ==================
    [Header("Interacción con el cebo")]
    [Min(0f), Tooltip("Segundos que pasa comiendo / investigando al llegar al cebo")]
    public float eatSeconds = 3f;

    // ================== ANIMACIÓN ==================
    [Header("Animación (opcional)")]
    [Tooltip("Animator de la criatura. Si lo dejas vacío se busca en sus hijos")]
    public Animator animator;

    [Tooltip("Parámetro Float del Animator que recibe la velocidad actual")]
    public string speedParam = "Speed";

    [Tooltip("Parámetro Trigger del Animator que se dispara al empezar a comer")]
    public string eatTrigger = "Eat";

    // ================== EVENTOS ==================
    [Header("Eventos")]
    public UnityEvent onLureDetected;    // Nota un cebo y va hacia él
    public UnityEvent onLureLost;        // El cebo desaparece antes de llegar
    public UnityEvent onStartEating;     // Llega y empieza a comer
    public UnityEvent onFinishedEating;  // Termina de comer

    [Header("Debug (solo lectura)")]
    [SerializeField] State state = State.Patrol;
    [SerializeField] LureItem target;

    NavMeshAgent agent;
    Vector3 home;               // Posición inicial (a la que vuelve si no hay patrulla)
    int patrolIndex;
    float waitTimer;
    float scanTimer;
    float eatTimer;
    LureItem lastEaten;         // Último cebo comido (para no repetirlo si no desaparece)
    Vector3 lastDest;
    bool hasDest;
    Vector3 lastPos;
    bool hasSpeedParam, hasEatParam;

    public State CurrentState => state;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    // Prepara el agente, guarda el punto de origen y comprueba los parámetros del Animator.
    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (moveMode == MoveMode.NavMesh && agent == null)
        {
            Debug.LogWarning("Attractable: falta un NavMeshAgent. Cambio a modo Direct.", this);
            moveMode = MoveMode.Direct;
        }
        if (moveMode == MoveMode.Direct && agent != null) agent.enabled = false;

        home = transform.position;
        lastPos = transform.position;

        if (animator == null) animator = GetComponentInChildren<Animator>();
        hasSpeedParam = HasParam(speedParam, AnimatorControllerParameterType.Float);
        hasEatParam = HasParam(eatTrigger, AnimatorControllerParameterType.Trigger);
    }

    // Bucle principal: busca cebos cada cierto tiempo y ejecuta el comportamiento del estado actual.
    void Update()
    {
        scanTimer += Time.deltaTime;
        if (scanTimer >= checkInterval)
        {
            scanTimer = 0f;
            ScanForLure();
        }

        switch (state)
        {
            case State.Patrol: UpdatePatrol(); break;
            case State.GoingToLure: UpdateGoingToLure(); break;
            case State.Eating: UpdateEating(); break;
        }

        UpdateAnimator();
    }

    // ---------- Detección ----------

    // Busca el cebo activo más cercano que le interese. Si ya tiene objetivo válido, lo mantiene.
    void ScanForLure()
    {
        if (state == State.Eating) return;
        if (target != null && target.IsActive) return;   // Ya va hacia uno

        LureItem best = null;
        float bestDist = float.MaxValue;

        foreach (LureItem lure in LureItem.Active)
        {
            if (lure == null || lure == lastEaten) continue;
            if (!acceptedKinds.Contains(lure.kind)) continue;

            float d = Vector3.Distance(transform.position, lure.Position);
            float reach = Mathf.Min(perceptionRadius, lure.attractRadius); // Debe cumplir ambos radios
            if (d <= reach && d < bestDist)
            {
                bestDist = d;
                best = lure;
            }
        }

        if (best != null)
        {
            target = best;
            state = State.GoingToLure;
            hasDest = false;
            onLureDetected.Invoke();
        }
    }

    // ---------- Estados ----------

    // PATRULLA: recorre los puntos esperando en cada uno; sin puntos, vuelve a su sitio de origen.
    void UpdatePatrol()
    {
        if (patrolPoints != null && patrolPoints.Count > 0)
        {
            Transform p = patrolPoints[patrolIndex % patrolPoints.Count];
            if (p == null) { patrolIndex++; return; }

            if (Arrived(p.position))
            {
                StopMoving();
                waitTimer += Time.deltaTime;
                if (waitTimer >= patrolWait)
                {
                    waitTimer = 0f;
                    patrolIndex = (patrolIndex + 1) % patrolPoints.Count;
                }
            }
            else
            {
                waitTimer = 0f;
                MoveTo(p.position, walkSpeed);
            }
        }
        else
        {
            if (!Arrived(home)) MoveTo(home, walkSpeed);
            else StopMoving();
        }
    }

    // IR AL CEBO: corre hacia él; si desaparece, vuelve a patrullar.
    void UpdateGoingToLure()
    {
        if (target == null || !target.IsActive)
        {
            target = null;
            state = State.Patrol;
            hasDest = false;
            onLureLost.Invoke();
            return;
        }

        if (Arrived(target.Position))
        {
            StartEating();
        }
        else
        {
            MoveTo(target.Position, runSpeed);
        }
    }

    // COMER: espera "eatSeconds" mirando al cebo y luego lo consume.
    void UpdateEating()
    {
        if (target == null || !target.IsActive)
        {
            // Otro se lo comió antes de que terminara
            target = null;
            state = State.Patrol;
            hasDest = false;
            onLureLost.Invoke();
            return;
        }

        FaceTowards(target.Position);
        eatTimer += Time.deltaTime;

        if (eatTimer >= eatSeconds)
        {
            onFinishedEating.Invoke();
            lastEaten = target;     // No volver a interesarse por este cebo si no desaparece
            target.Consume();
            target = null;
            state = State.Patrol;
            hasDest = false;
        }
    }

    void StartEating()
    {
        StopMoving();
        state = State.Eating;
        eatTimer = 0f;
        if (hasEatParam) animator.SetTrigger(eatTrigger);
        onStartEating.Invoke();
    }

    // ---------- Movimiento ----------

    // ¿Ha llegado al destino? (distancia en horizontal, o el agente dice que terminó su camino)
    bool Arrived(Vector3 dest)
    {
        Vector3 d = dest - transform.position;
        d.y = 0f;
        if (d.magnitude <= arriveDistance) return true;

        if (moveMode == MoveMode.NavMesh && agent != null && agent.enabled && agent.isOnNavMesh && hasDest)
        {
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
                return true;
        }
        return false;
    }

    // Se mueve hacia un punto con la velocidad indicada.
    void MoveTo(Vector3 dest, float speed)
    {
        if (moveMode == MoveMode.NavMesh)
        {
            if (agent == null || !agent.enabled || !agent.isOnNavMesh) return;

            // Ajusta el destino al punto del NavMesh más cercano
            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, navSampleDistance, NavMesh.AllAreas))
                dest = hit.position;

            // Solo recalcula la ruta si el destino cambió (evita recalcular cada frame)
            if (!hasDest || (dest - lastDest).sqrMagnitude > 0.0625f)
            {
                agent.SetDestination(dest);
                lastDest = dest;
                hasDest = true;
            }
            agent.speed = speed;
            agent.isStopped = false;
        }
        else
        {
            // Modo Direct: avanza en línea recta (en 3D) y gira hacia el destino
            hasDest = true;
            Vector3 to = dest - transform.position;
            transform.position = Vector3.MoveTowards(transform.position, dest, speed * Time.deltaTime);

            to.y = 0f;
            if (to.sqrMagnitude > 0.001f)
            {
                Quaternion look = Quaternion.LookRotation(to);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
            }
        }
    }

    // Se detiene.
    void StopMoving()
    {
        if (moveMode == MoveMode.NavMesh && agent != null && agent.enabled && agent.isOnNavMesh)
            agent.isStopped = true;
        hasDest = false;
    }

    // Gira hacia un punto (para mirar la comida mientras come).
    void FaceTowards(Vector3 point)
    {
        Vector3 to = point - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.001f) return;
        Quaternion look = Quaternion.LookRotation(to);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, look, turnSpeed * Time.deltaTime);
    }

    // ---------- Animación ----------

    // Envía al Animator la velocidad real (calculada con el desplazamiento del frame).
    void UpdateAnimator()
    {
        float speed = Time.deltaTime > 0f ? (transform.position - lastPos).magnitude / Time.deltaTime : 0f;
        lastPos = transform.position;
        if (hasSpeedParam) animator.SetFloat(speedParam, speed);
    }

    // Comprueba que el Animator tiene ese parámetro (así no llena la consola de avisos).
    bool HasParam(string paramName, AnimatorControllerParameterType type)
    {
        if (animator == null || animator.runtimeAnimatorController == null || string.IsNullOrEmpty(paramName)) return false;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.name == paramName && p.type == type) return true;
        return false;
    }

    // Dibuja el radio de percepción y la ruta de patrulla en la ventana Scene.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.9f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, perceptionRadius);

        Gizmos.color = Color.cyan;
        for (int i = 0; i < patrolPoints.Count; i++)
        {
            if (patrolPoints[i] == null) continue;
            Gizmos.DrawSphere(patrolPoints[i].position, 0.15f);
            Transform next = patrolPoints[(i + 1) % patrolPoints.Count];
            if (next != null) Gizmos.DrawLine(patrolPoints[i].position, next.position);
        }
    }
}
