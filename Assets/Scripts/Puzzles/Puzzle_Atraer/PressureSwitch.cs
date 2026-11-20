using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  PressureSwitch.cs  (PUZZLE 3: ATENCIÓN DE ANIMALES / NPC / ENEMIGOS)
//  ---------------------------------------------------------------------
//  Un INTERRUPTOR DE PRESIÓN: se activa cuando hay suficientes criaturas
//  (Attractable) encima. Es el interruptor "que tú no alcanzas".
//  No usa colliders ni triggers: mide la distancia a las criaturas, así
//  que funciona con cualquier tipo de movimiento (NavMesh o Direct).
//
//  La placa se hunde visualmente al activarse (si asignas "plateVisual").
// =====================================================================
public class PressureSwitch : MonoBehaviour
{
    // ================== DETECCIÓN ==================
    [Header("Detección")]
    [Min(0.1f), Tooltip("Radio horizontal (metros) en el que una criatura cuenta como 'encima'")]
    public float pressRadius = 1f;

    [Min(0.1f), Tooltip("Diferencia máxima de altura. Si la criatura está más arriba o abajo que esto, no cuenta")]
    public float heightTolerance = 1.5f;

    [Min(1), Tooltip("Cuántas criaturas hacen falta a la vez para pulsarlo")]
    public int requiredOccupants = 1;

    // ================== COMPORTAMIENTO ==================
    [Header("Comportamiento")]
    [Tooltip("Si está activado, una vez pulsado se queda pulsado aunque la criatura se vaya")]
    public bool stayPressed = false;

    [Min(0f), Tooltip("Segundos que espera antes de soltarse cuando se queda sin criaturas")]
    public float releaseDelay = 0.3f;

    // ================== VISUAL ==================
    [Header("Visual")]
    [Tooltip("Parte móvil de la placa (hijo). Se hunde al pulsarse. Opcional")]
    public Transform plateVisual;

    [Min(0f), Tooltip("Cuánto se hunde la placa (metros)")]
    public float pressDepth = 0.1f;

    [Min(0.1f), Tooltip("Velocidad del movimiento de la placa")]
    public float moveSpeed = 5f;

    [Tooltip("Renderer que cambia de color según el estado (opcional)")]
    public Renderer indicator;
    public Color idleColor = new Color(0.8f, 0.3f, 0.3f);
    public Color pressedColor = new Color(0.3f, 0.9f, 0.4f);

    // ================== EVENTOS ==================
    [Header("Eventos")]
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    [Header("Debug (solo lectura)")]
    [SerializeField] bool pressed;
    [SerializeField] int occupants;

    // Aviso interno para SwitchGroup
    public System.Action<PressureSwitch> StateChanged;

    Vector3 upLocalPos;      // Posición local de la placa sin pulsar
    float releaseTimer;      // Cuenta atrás para soltarse

    public bool IsPressed => pressed;

    void Awake()
    {
        if (plateVisual != null) upLocalPos = plateVisual.localPosition;
        PuzzleTint.SetColor(indicator, idleColor);
    }

    // Cuenta criaturas cada frame, cambia el estado y anima la placa.
    void Update()
    {
        occupants = CountOccupants();
        bool shouldPress = occupants >= requiredOccupants;

        if (!pressed && shouldPress)
        {
            releaseTimer = 0f;
            SetPressed(true);
        }
        else if (pressed && !shouldPress && !stayPressed)
        {
            // Espera un poco antes de soltarse (evita parpadeos si la criatura se mueve en el borde)
            releaseTimer += Time.deltaTime;
            if (releaseTimer >= releaseDelay) SetPressed(false);
        }
        else
        {
            releaseTimer = 0f;
        }

        AnimatePlate();
    }

    // Cuenta las criaturas que están sobre la placa (cerca en horizontal y a una altura parecida).
    int CountOccupants()
    {
        int count = 0;
        Vector3 p = transform.position;
        float sqrRadius = pressRadius * pressRadius;

        foreach (Attractable a in Attractable.All)
        {
            Vector3 d = a.transform.position - p;
            if (Mathf.Abs(d.y) > heightTolerance) continue;
            d.y = 0f;
            if (d.sqrMagnitude <= sqrRadius) count++;
        }
        return count;
    }

    void SetPressed(bool value)
    {
        pressed = value;
        PuzzleTint.SetColor(indicator, pressed ? pressedColor : idleColor);

        if (pressed) onPressed.Invoke(); else onReleased.Invoke();
        if (StateChanged != null) StateChanged(this);
    }

    // Hunde o sube la placa suavemente.
    void AnimatePlate()
    {
        if (plateVisual == null) return;
        Vector3 target = pressed ? upLocalPos + Vector3.down * pressDepth : upLocalPos;
        plateVisual.localPosition = Vector3.MoveTowards(plateVisual.localPosition, target, moveSpeed * Time.deltaTime);
    }

    // Suelta el interruptor a la fuerza (útil al reiniciar un puzzle con stayPressed).
    [ContextMenu("Reiniciar interruptor")]
    public void ResetSwitch()
    {
        if (pressed) SetPressed(false);
    }

    // Dibuja la zona de detección en la ventana Scene.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, pressRadius);
        Gizmos.color = new Color(0f, 1f, 0f, 0.4f);
        Gizmos.DrawWireCube(transform.position, new Vector3(pressRadius * 2f, heightTolerance * 2f, pressRadius * 2f));
    }
}
