using UnityEngine;
using UnityEngine.Events;

//Va en un objeto vacio. Cuando TODOS los objetivos tienen su color, se completa el puzzle
public class PaintPuzzle : MonoBehaviour
{

    //Todos los objetos que hay que pintar para superar el puzzle
    [SerializeField] private PaintTarget[] targets;

    //Evento para que pasen cositas cuando se complete el puzzle (abrir puerta, etc.)
    [SerializeField] private UnityEvent OnPuzzleComplete;

    private int targetsCompleted = 0;
    private bool isComplete = false;

    void Start()
    {
        //Asigna a cada objetivo este puzzle
        foreach (PaintTarget target in targets)
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
