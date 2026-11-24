using UnityEngine;

//Va en el JUGADOR. Con el clic lanza un cebo hacia donde miras
public class LureThrower : MonoBehaviour
{

    //El PREFAB del cebo (arrastralo desde la carpeta Assets, no desde la escena)
    [SerializeField] private Lure lurePrefab;
    //Hacia donde se lanza (el pivot / camara). Si se deja vacio usa la camara principal
    [SerializeField] private Transform aimOrigin;
    //Fuerza del lanzamiento
    [SerializeField] private float throwForce = 12f;
    //Fuerza extra hacia arriba para que haga arco
    [SerializeField] private float upForce = 3f;
    //Cebos que le quedan al jugador
    [SerializeField] private int ammo = 5;
    //Boton del raton (0 = izquierdo, 1 = derecho)
    [SerializeField] private int mouseButton = 0;

    void Start()
    {
        if (aimOrigin == null) aimOrigin = Camera.main.transform;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(mouseButton))
        {
            if (ammo > 0) ThrowLure();
            else Debug.Log("LureThrower: no quedan cebos");
        }
    }

    void ThrowLure()
    {
        ammo--;

        //Aparece un poco delante de la camara para no chocar con el propio jugador
        Lure lure = Instantiate(lurePrefab, aimOrigin.position + aimOrigin.forward * 0.8f, Quaternion.identity);
        lure.Throw(aimOrigin.forward * throwForce + Vector3.up * upForce);
    }

}
