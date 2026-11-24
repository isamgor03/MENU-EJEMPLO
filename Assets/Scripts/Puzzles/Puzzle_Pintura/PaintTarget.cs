using UnityEngine;

//Una estatua / vela / cristal que se pinta. Esta completo cuando tiene el color que necesita
//Necesita un Collider para recibir los disparos
public class PaintTarget : MonoBehaviour
{

    //El puzzle al que pertenece
    [HideInInspector] public PaintPuzzle assignedPuzzle;

    //Color que necesita para estar correcto
    public PaintColor requiredColor;

    public bool isComplete = false;

    private Renderer rend;

    void Start()
    {
        rend = GetComponentInChildren<Renderer>();
        //Empieza sin pintar (gris)
        rend.material.color = PaintSource.realColors[0];
    }

    //El pincel llama a esto cuando le disparas
    public void Paint(PaintColor color)
    {
        rend.material.color = PaintSource.realColors[(int)color];

        bool wasComplete = isComplete;
        isComplete = color == requiredColor;

        //Avisa al puzzle solo cuando cambia el estado
        if (isComplete == true && wasComplete == false) assignedPuzzle?.CompletedTarget();
        else if (isComplete == false && wasComplete == true) assignedPuzzle?.RemoveTarget();
    }

}
