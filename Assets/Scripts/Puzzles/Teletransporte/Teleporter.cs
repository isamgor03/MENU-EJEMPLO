using System.Collections;
using UnityEngine;
using UnityEngine.Events;

//Para  PUERTAS / PORTALES... Cuando el personaje esta cerca y pulsa E, se teletransporta al destino
//El personaje necesita un Collider y el tag "Player" (el objeto que tiene el Rigidbody)
public class Teleporter : MonoBehaviour
{

    //Donde aparece el personaje (un objeto vacio colocado en el otro sitio)
    [SerializeField] private Transform destination;
    //Si esta marcado, el personaje mira hacia donde mira el destino (la flecha azul)
    [SerializeField] private bool copyRotation = true;
    //Tag del personaje
    [SerializeField] private string playerTag = "Player";
    //Si esta marcado, la puerta no funciona hasta que se llame a Unlock()
    [SerializeField] private bool locked = false;
    //Cartel de "Pulsa E" que se muestra cuando el personaje esta cerca (opcional)
    [SerializeField] private GameObject prompt;

    //Tamano de la caja que detecta si el personaje esta cerca
    [SerializeField] private Vector3 checkBoxSize = new Vector3(2f, 2f, 2f);
    //Donde se crea la caja
    [SerializeField] private Vector3 checkBoxPosition = Vector3.up;

    //Para que pasen cositas al teletransportar (sonido, fundido a negro...)
    [SerializeField] private UnityEvent OnTeleport;

    //El personaje que esta cerca ahora mismo (null si no hay ninguno)
    private Transform playerNearby;

    void Start()
    {
        if (prompt != null) prompt.SetActive(false);
        StartCoroutine(DetectCrt());
    }

    void Update()
    {
        //Si el personaje esta cerca, la puerta esta abierta y pulsa E, se teletransporta
        if (playerNearby != null && locked == false && Input.GetKeyDown(KeyCode.E))
        {
            Teleport();
        }
    }

    IEnumerator DetectCrt()
    {
        while (true)
        {
            DetectPlayer();
            yield return new WaitForSeconds(0.1f);
        }
    }

    void DetectPlayer()
    {
        playerNearby = null;

        //Crea una caja delante de la puerta para detectar si el personaje esta cerca
        Collider[] detected = Physics.OverlapBox(transform.position + checkBoxPosition, checkBoxSize * 0.5f, transform.rotation);

        foreach (Collider other in detected)
        {
            if (other.CompareTag(playerTag))
            {
                //Si el collider esta en un hijo, movemos el objeto que tiene el Rigidbody
                playerNearby = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
                break;
            }
        }

        //Muestra el cartel solo si el personaje esta cerca y la puerta esta abierta
        if (prompt != null) prompt.SetActive(playerNearby != null && locked == false);
    }

    void Teleport()
    {
        if (destination == null)
        {
            Debug.Log("Teleporter: falta asignar el destino en " + name);
            return;
        }

        //Si el personaje usa CharacterController hay que desactivarlo un momento para poder moverlo
        CharacterController controller = playerNearby.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;

        playerNearby.position = destination.position;

        //Solo giramos en horizontal (eje Y), igual que el controlador
        if (copyRotation == true) playerNearby.rotation = Quaternion.Euler(0, destination.eulerAngles.y, 0);

        if (controller != null) controller.enabled = true;

        //Si usa Rigidbody, le quitamos la velocidad para que no salga disparado
        Rigidbody rb = playerNearby.GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        playerNearby = null;
        if (prompt != null) prompt.SetActive(false);
        OnTeleport?.Invoke();
    }

    //Abre la puerta (conectalo al evento de un puzzle)
    public void Unlock()
    {
        locked = false;
    }

    //Cierra la puerta
    public void Lock()
    {
        locked = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        //Dibuja la caja donde el personaje puede usar la puerta
        Gizmos.DrawWireCube(transform.position + checkBoxPosition, checkBoxSize);

        //Dibuja una linea hasta el destino
        if (destination != null) Gizmos.DrawLine(transform.position, destination.position);
    }

}