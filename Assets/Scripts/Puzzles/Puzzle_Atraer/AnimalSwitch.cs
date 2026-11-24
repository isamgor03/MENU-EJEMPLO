using System.Collections;
using UnityEngine;
using UnityEngine.Events;

//El interruptor de presion: se activa cuando hay un animal (LureAnimal) encima
public class AnimalSwitch : MonoBehaviour
{

    //Tamano de la caja que detecta si hay un animal encima
    [SerializeField] private Vector3 checkBoxSize = new Vector3(1.5f, 1f, 1.5f);
    //Donde se crea la caja
    [SerializeField] private Vector3 checkBoxPosition = new Vector3(0f, 0.5f, 0f);

    //Eventos para que pasen cositas (abrir puerta, etc.)
    [SerializeField] private UnityEvent OnPressed;
    [SerializeField] private UnityEvent OnReleased;

    private bool isPressed = false;

    void Start()
    {
        StartCoroutine(DetectCrt());
    }

    IEnumerator DetectCrt()
    {
        while (true)
        {
            DetectAnimal();
            yield return new WaitForSeconds(0.1f);
        }
    }

    void DetectAnimal()
    {
        bool animalOnTop = false;

        //Crea una caja encima para detectar si hay algun animal
        Collider[] detected = Physics.OverlapBox(transform.position + checkBoxPosition, checkBoxSize * 0.5f, transform.rotation);

        foreach (Collider other in detected)
        {
            if (other.GetComponentInParent<LureAnimal>() != null) animalOnTop = true;
        }

        //Avisa solo cuando cambia el estado
        if (animalOnTop == true && isPressed == false)
        {
            isPressed = true;
            OnPressed?.Invoke();
        }
        else if (animalOnTop == false && isPressed == true)
        {
            isPressed = false;
            OnReleased?.Invoke();
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(transform.position + checkBoxPosition, checkBoxSize);
    }

}
