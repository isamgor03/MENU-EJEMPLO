using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  FirePuzzleManager.cs  (PUZZLE 2: FUEGO, LLUVIA Y CUERDAS)
//  ---------------------------------------------------------------------
//  El "cerebro" del puzzle. Va en un objeto vacío. Sirve para dos tipos
//  de puzzle (o para mezclarlos):
//
//  A) CUERDAS: "targets" = las cuerdas correctas, Required State = Burnt.
//     "forbidden" = cuerdas que NO deben arder (si arden, fallas).
//     Al quemarse las correctas -> onCompleted (suelta el contrapeso).
//
//  B) ANTORCHAS: "targets" = las antorchas, Required State = Burning.
//     Hay que tenerlas TODAS encendidas a la vez (la lluvia las apaga).
//     Con "Require Order" deben encenderse en el orden de la lista.
//
//  Si fallas (orden incorrecto o arde algo prohibido) se lanza onFailed y,
//  si "autoResetOnFail" está activo, todo se restaura tras unos segundos.
// =====================================================================
public class FirePuzzleManager : MonoBehaviour
{
    public enum RequiredState { Burning, Burnt }

    // ================== OBJETIVOS ==================
    [Header("Objetivos")]
    [Tooltip("Inflamables que hay que conseguir (antorchas a encender, cuerdas a quemar...). El orden de la lista importa si 'Require Order' está activo")]
    public List<Flammable> targets = new List<Flammable>();

    [Tooltip("Burning = deben estar ardiendo a la vez (antorchas). Burnt = deben haberse quemado del todo (cuerdas)")]
    public RequiredState requiredState = RequiredState.Burning;

    [Tooltip("Solo con 'Burning': las antorchas deben encenderse siguiendo el orden de la lista")]
    public bool requireOrder = false;

    // ================== PROHIBIDOS ==================
    [Header("Prohibidos")]
    [Tooltip("Inflamables que NO deben arder (cuerdas equivocadas). Si arde alguno, el puzzle falla")]
    public List<Flammable> forbidden = new List<Flammable>();

    // ================== FALLO ==================
    [Header("Fallo")]
    [Tooltip("Si está activado, al fallar se restaura todo automáticamente")]
    public bool autoResetOnFail = true;

    [Min(0f), Tooltip("Segundos de espera antes de restaurar tras un fallo")]
    public float resetDelay = 2f;

    // ================== EVENTOS ==================
    [Header("Eventos")]
    public UnityEvent onCompleted;          // Puzzle resuelto (aquí conectas la recompensa)
    public UnityEvent onWrongOrder;         // Se encendió una antorcha fuera de orden
    public UnityEvent onForbiddenBurned;    // Ardió algo prohibido
    public UnityEvent onFailed;             // Cualquier fallo (se lanza además de los dos anteriores)
    public UnityEvent onReset;              // El puzzle se restauró

    [Header("Debug (solo lectura)")]
    [SerializeField] int targetsDone;
    [SerializeField] bool completed;
    [SerializeField] bool failed;

    // Nos suscribimos a los avisos de cada inflamable.
    void OnEnable()
    {
        foreach (Flammable t in targets)
        {
            if (t == null) continue;
            t.Ignited += OnTargetIgnited;
            t.Extinguished += OnTargetChanged;
            t.BecameBurnt += OnTargetChanged;
        }
        foreach (Flammable f in forbidden)
            if (f != null) f.Ignited += OnForbiddenIgnited;
    }

    void OnDisable()
    {
        foreach (Flammable t in targets)
        {
            if (t == null) continue;
            t.Ignited -= OnTargetIgnited;
            t.Extinguished -= OnTargetChanged;
            t.BecameBurnt -= OnTargetChanged;
        }
        foreach (Flammable f in forbidden)
            if (f != null) f.Ignited -= OnForbiddenIgnited;
    }

    // Se enciende un objetivo: si hay orden, comprobamos que se respeta.
    void OnTargetIgnited(Flammable f)
    {
        if (completed || failed) return;

        if (requireOrder && requiredState == RequiredState.Burning)
        {
            int index = targets.IndexOf(f);
            for (int j = 0; j < index; j++)
            {
                // Si alguna antorcha anterior en la lista NO está encendida, el orden es incorrecto
                if (targets[j] == null || !targets[j].IsBurning)
                {
                    onWrongOrder.Invoke();
                    Fail();
                    return;
                }
            }
        }
        Evaluate();
    }

    // Un objetivo se apaga o se quema: reevaluamos.
    void OnTargetChanged(Flammable f)
    {
        if (completed || failed) return;
        Evaluate();
    }

    // Arde algo que no debía: fallo.
    void OnForbiddenIgnited(Flammable f)
    {
        if (completed || failed) return;
        onForbiddenBurned.Invoke();
        Fail();
    }

    // Cuenta cuántos objetivos cumplen la condición y decide si el puzzle está resuelto.
    void Evaluate()
    {
        int done = 0;
        foreach (Flammable t in targets)
        {
            if (t == null) continue;
            bool ok = requiredState == RequiredState.Burning ? t.IsBurning : t.IsBurnt;
            if (ok) done++;
        }
        targetsDone = done;

        if (!completed && !failed && targets.Count > 0 && done == targets.Count)
        {
            completed = true;
            onCompleted.Invoke();
        }
    }

    // Marca el fallo y (opcional) programa el reinicio.
    void Fail()
    {
        if (failed) return;
        failed = true;
        onFailed.Invoke();
        if (autoResetOnFail) StartCoroutine(ResetAfterDelay());
    }

    IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSeconds(resetDelay);
        ResetPuzzle();
    }

    // Restaura todos los inflamables (recupera cuerdas quemadas, apaga antorchas) y reabre el puzzle.
    [ContextMenu("Reiniciar puzzle")]
    public void ResetPuzzle()
    {
        foreach (Flammable t in targets) if (t != null) t.ResetState();
        foreach (Flammable f in forbidden) if (f != null) f.ResetState();

        completed = false;
        failed = false;
        targetsDone = 0;
        onReset.Invoke();
    }
}
