using UnityEngine;
using UnityEngine.Events;

//Va en un objeto vacio. Se completa cuando TODOS los objetivos estan completados:
//- Cuerdas (Burn Time > 0): cuando se han quemado del todo
//- Antorchas (Burn Time = 0): cuando estan encendidas a la vez (si llueve y se apagan, dejan de contar)
public class FirePuzzle : MonoBehaviour
{

    //Los inflamables que hay que conseguir (cuerdas a quemar, antorchas a encender)
    [SerializeField] private Flammable[] targets;

    //Evento para que pasen cositas cuando se complete el puzzle (soltar el contrapeso, abrir puerta...)
    [SerializeField] private UnityEvent OnPuzzleComplete;

    private int targetsCompleted = 0;
    private bool isComplete = false;

    //Es Awake (y no Start) para que ya este asignado cuando un Flammable con Start Burning haga su Start
    void Awake()
    {
        foreach (Flammable target in targets)
        {
            target.assignedPuzzle = this;
        }
    }

    public void CompletedTarget()
    {
        if (isComplete == true) return;

        targetsCompleted++;

        if (targetsCompleted == targets.Length)
        {
            isComplete = true;
            OnPuzzleComplete?.Invoke();
        }
    }

    public void RemoveTarget()
    {
        if (isComplete == true) return;

        //Impedir que haya un valor negativo
        targetsCompleted = Mathf.Max(0, targetsCompleted - 1);
    }

}
