using UnityEngine;
using UnityEngine.Events;
public class Esc_Event : MonoBehaviour
{
    public UnityEvent Accion;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Cancel"))
        {
            Accion.Invoke();

        }
    }
}
