using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  SwitchGroup.cs  (PUZZLE 3: ATENCIÓN DE ANIMALES / NPC / ENEMIGOS)
//  ---------------------------------------------------------------------
//  Agrupa varios PressureSwitch. Cuando TODOS están pulsados a la vez
//  lanza "onAllPressed" (la recompensa). Sirve para puzzles de varias
//  placas: dos animales en dos placas, un enemigo y un NPC, etc.
//  Con un solo interruptor no hace falta: usa directamente su evento.
// =====================================================================
public class SwitchGroup : MonoBehaviour
{
    [Header("Interruptores")]
    [Tooltip("Arrastra aquí todas las placas que deben estar pulsadas a la vez")]
    public List<PressureSwitch> switches = new List<PressureSwitch>();

    [Header("Reglas")]
    [Tooltip("Si está activado, al completarse se queda completado aunque luego se suelte alguna placa")]
    public bool latchWhenComplete = true;

    [Header("Eventos")]
    public UnityEvent onAllPressed;      // Todas pulsadas
    public UnityEvent onNoLongerAll;     // Ya no están todas pulsadas (solo si latchWhenComplete = false)

    [Header("Debug (solo lectura)")]
    [SerializeField] int pressedCount;
    [SerializeField] bool complete;

    void OnEnable()
    {
        foreach (PressureSwitch s in switches)
            if (s != null) s.StateChanged += Evaluate;
    }

    void OnDisable()
    {
        foreach (PressureSwitch s in switches)
            if (s != null) s.StateChanged -= Evaluate;
    }

    // Se llama cada vez que cambia el estado de alguna placa.
    void Evaluate(PressureSwitch changed)
    {
        int count = 0;
        foreach (PressureSwitch s in switches)
            if (s != null && s.IsPressed) count++;
        pressedCount = count;

        bool all = switches.Count > 0 && count == switches.Count;

        if (all && !complete)
        {
            complete = true;
            onAllPressed.Invoke();
        }
        else if (!all && complete && !latchWhenComplete)
        {
            complete = false;
            onNoLongerAll.Invoke();
        }
    }
}
