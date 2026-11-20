using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  PaintPuzzleManager.cs  (PUZZLE 1: PINTURA Y COLORES)
//  ---------------------------------------------------------------------
//  El "cerebro" del puzzle. Va en un objeto vacío de la escena.
//  Vigila una lista de PaintReceiver: cuando TODOS tienen su color
//  correcto a la vez, lanza "onPuzzleCompleted" (ahí conectas la
//  recompensa: abrir una puerta, activar un cofre, etc.).
// =====================================================================
public class PaintPuzzleManager : MonoBehaviour
{
    [Header("Objetos del puzzle")]
    [Tooltip("Arrastra aquí todas las estatuas / velas / cristales que hay que pintar")]
    public List<PaintReceiver> receivers = new List<PaintReceiver>();

    [Header("Reglas")]
    [Tooltip("Al completar, bloquea todos los objetos para que no se puedan estropear")]
    public bool lockReceiversOnComplete = true;

    [Tooltip("Si está activado, el puzzle se completa una sola vez. Si no, puede 'romperse' si algo deja de coincidir")]
    public bool completeOnlyOnce = true;

    [Header("Eventos")]
    public UnityEvent onPuzzleCompleted;   // Todos correctos
    public UnityEvent onPuzzleBroken;      // Ya estaba completo y algo dejó de coincidir (solo si completeOnlyOnce = false)
    public UnityEvent onProgressChanged;   // Cambia el número de objetos correctos

    [Header("Debug (solo lectura)")]
    [SerializeField] int matchedCount;
    [SerializeField] bool completed;

    public int MatchedCount => matchedCount;
    public int TotalCount => receivers.Count;

    // Se suscribe a los avisos de cada receptor.
    void OnEnable()
    {
        foreach (PaintReceiver r in receivers)
            if (r != null) r.StateChanged += Evaluate;
    }

    void OnDisable()
    {
        foreach (PaintReceiver r in receivers)
            if (r != null) r.StateChanged -= Evaluate;
    }

    void Start() { Evaluate(null); }

    // Cuenta cuántos objetos son correctos y decide si el puzzle está completo.
    void Evaluate(PaintReceiver changed)
    {
        int count = 0;
        foreach (PaintReceiver r in receivers)
            if (r != null && r.IsMatched) count++;

        if (count != matchedCount)
        {
            matchedCount = count;
            onProgressChanged.Invoke();
        }

        bool allCorrect = receivers.Count > 0 && count == receivers.Count;

        if (allCorrect && !completed)
        {
            completed = true;
            if (lockReceiversOnComplete)
                foreach (PaintReceiver r in receivers) if (r != null) r.Lock();
            onPuzzleCompleted.Invoke();
        }
        else if (!allCorrect && completed && !completeOnlyOnce)
        {
            completed = false;
            onPuzzleBroken.Invoke();
        }
    }

    // Reinicia todo el puzzle: borra la pintura y desbloquea los objetos.
    [ContextMenu("Reiniciar puzzle")]
    public void ResetPuzzle()
    {
        completed = false;
        foreach (PaintReceiver r in receivers) if (r != null) r.ResetReceiver();
        Evaluate(null);
    }
}
