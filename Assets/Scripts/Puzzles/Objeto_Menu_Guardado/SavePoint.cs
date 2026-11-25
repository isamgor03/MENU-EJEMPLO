using System.Collections;
using UnityEngine;

//Va en el OBJETO que abre el menu (un altar, una hoguera, un banco, un cofre...)
//Cuando el personaje esta cerca y pulsa E, se abre el menu del juego (GameMenu)
//El personaje necesita un Collider y el tag "Player" (el objeto que tiene el Rigidbody)
public class SavePoint : MonoBehaviour
{

    //El menu que se abre (arrastra el objeto que tiene el script GameMenu)
    [SerializeField] private GameMenu menu;
    //Tag del personaje
    [SerializeField] private string playerTag = "Player";
    //Cartel de "Pulsa E" que se muestra cuando el personaje esta cerca (opcional)
    [SerializeField] private GameObject prompt;

    //Tamano de la caja que detecta si el personaje esta cerca
    [SerializeField] private Vector3 checkBoxSize = new Vector3(3f, 2f, 3f);
    //Donde se crea la caja
    [SerializeField] private Vector3 checkBoxPosition = Vector3.up;

    //Si el personaje esta cerca ahora mismo
    private bool playerNearby = false;

    void Start()
    {
        if (menu == null) Debug.Log("SavePoint: falta asignar el menu en " + name);
        if (prompt != null) prompt.SetActive(false);
        StartCoroutine(DetectCrt());
    }

    void Update()
    {
        if (menu == null) return;

        //Si el personaje esta cerca y pulsa E, se abre el menu
        if (playerNearby == true && Input.GetKeyDown(KeyCode.E)) menu.Open();

        //El cartel solo se ve si esta cerca y el menu esta cerrado
        if (prompt != null) prompt.SetActive(playerNearby == true && menu.IsOpen == false);
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
        playerNearby = false;

        //Crea una caja alrededor del objeto para detectar si el personaje esta cerca
        Collider[] detected = Physics.OverlapBox(transform.position + checkBoxPosition, checkBoxSize * 0.5f, transform.rotation);

        foreach (Collider other in detected)
        {
            if (other.CompareTag(playerTag))
            {
                playerNearby = true;
                break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        //Dibuja la caja donde el personaje puede abrir el menu
        Gizmos.DrawWireCube(transform.position + checkBoxPosition, checkBoxSize);
    }

}
