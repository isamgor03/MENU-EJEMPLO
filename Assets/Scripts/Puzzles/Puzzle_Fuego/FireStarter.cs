using UnityEngine;

//Va en el JUGADOR. Con el clic enciende lo inflamable que estes MIRANDO
public class FireStarter : MonoBehaviour
{

    //Desde donde sale el rayo (el pivot / camara). Si se deja vacio usa la camara principal
    [SerializeField] private Transform aimOrigin;
    //A que distancia puedes encender cosas
    [SerializeField] private float range = 4f;
    //Grosor del rayo, para acertar a cuerdas finas
    [SerializeField] private float radius = 0.3f;
    //Boton del raton (0 = izquierdo, 1 = derecho)
    [SerializeField] private int mouseButton = 0;

    void Start()
    {
        if (aimOrigin == null) aimOrigin = Camera.main.transform;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(mouseButton)) IgniteTarget();
    }

    void IgniteTarget()
    {
        //Lanza un rayo gordo hacia donde miras
        if (Physics.SphereCast(aimOrigin.position, radius, aimOrigin.forward, out RaycastHit hit, range))
        {
            Flammable flammable = hit.collider.GetComponentInParent<Flammable>();

            if (flammable != null) flammable.Ignite();
            //Esto sirve para ver en la consola que esta golpeando el rayo
            else Debug.Log("FireStarter: el rayo golpea " + hit.collider.name + " pero no es inflamable");
        }
    }

}
