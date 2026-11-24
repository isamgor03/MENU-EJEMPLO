using UnityEngine;

public class GroundChecker : MonoBehaviour
{
    public bool IsGrounded = false;

    //tamaño de la caja que detecta el suelo 

    [SerializeField] private Vector3 boxSize = new Vector3(0.25f, 0.1f, 0.25f);

    [SerializeField] private Vector3 offset = new Vector3(0, -0.5f, 0f);

    [SerializeField] private LayerMask groundLayers;




    // Update is called once per frame
    void Update()
    {

        //Crear una caja que detecta si hay algun collider en esa capa
        //Como la funcion devuelve un bool, lo igualamos directamente a la variable isGrounded
        IsGrounded = Physics.CheckBox(transform.position + offset, boxSize * 0.5f, Quaternion.identity, groundLayers);

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.papayaWhip;
        //dIBUJAR LA CAJA QUE DETECTA EL SUELO 
        Gizmos.DrawWireCube(transform.position + offset, boxSize);
    }
}
