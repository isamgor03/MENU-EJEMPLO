using UnityEngine;

//El cebo (comida). Va en el PREFAB que lanza el jugador. Necesita Rigidbody y Collider
[RequireComponent(typeof(Rigidbody))]
public class Lure : MonoBehaviour
{

    //Segundos que dura el cebo si nadie se lo come
    [SerializeField] private float lifeTime = 20f;

    private Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    //Lanza el cebo con una fuerza
    public void Throw(Vector3 force)
    {
        rb.linearVelocity = force;
    }

}
