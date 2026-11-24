using UnityEngine;

//Va en el JUGADOR
//E = coge el color del bote que estes mirando (si ya llevas uno, se MEZCLAN)
//Q = limpia el pincel
//Clic = dispara pintura a lo que estes mirando
public class PaintBrush : MonoBehaviour
{

    //Desde donde sale el rayo (el pivot / camara). Si se deja vacio usa la camara principal
    [SerializeField] private Transform aimOrigin;
    //A que distancia puedes coger pintura de un bote
    [SerializeField] private float pickupRange = 3f;
    //A que distancia llega el disparo
    [SerializeField] private float shootRange = 20f;
    //Opcional: un objeto que muestra el color que llevas
    [SerializeField] private Renderer indicator;
    //Boton del raton para disparar (0 = izquierdo, 1 = derecho)
    [SerializeField] private int mouseButton = 0;

    //El color que lleva el pincel ahora mismo
    private PaintColor currentColor = PaintColor.None;

    void Start()
    {
        if (aimOrigin == null) aimOrigin = Camera.main.transform;
        ShowColor();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) TakePaint();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            currentColor = PaintColor.None;
            ShowColor();
        }

        if (Input.GetMouseButtonDown(mouseButton)) Shoot();
    }

    void TakePaint()
    {
        //Lanza un rayo hacia donde miras para ver si hay un bote
        if (Physics.Raycast(aimOrigin.position, aimOrigin.forward, out RaycastHit hit, pickupRange))
        {
            PaintSource source = hit.collider.GetComponentInParent<PaintSource>();

            if (source != null)
            {
                //Mezclar = unir los bits de los dos colores (rojo + amarillo = naranja...)
                currentColor = (PaintColor)((int)currentColor | (int)source.color);
                ShowColor();
            }
        }
    }

    void Shoot()
    {
        if (currentColor == PaintColor.None) return;

        if (Physics.Raycast(aimOrigin.position, aimOrigin.forward, out RaycastHit hit, shootRange))
        {
            PaintTarget target = hit.collider.GetComponentInParent<PaintTarget>();

            if (target != null) target.Paint(currentColor);
        }
    }

    void ShowColor()
    {
        if (indicator != null) indicator.material.color = PaintSource.realColors[(int)currentColor];
    }

}
