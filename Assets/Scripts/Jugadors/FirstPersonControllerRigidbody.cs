using UnityEngine;

public class FirstPersonControllerRigidBody : MonoBehaviour
{

    public float movementSpeed = 4;
    public float rotationSpeed = 0;
    public float jumpforce = 5;

    public float sprintMultiplier = 1.5f; // Mio: multiplicador de velocidad al pulsar Shift

    //El objeto que rota hacia arriba y abajo 
    public Transform pivot;

    //Guarda la rotacion en el eje X que necesita el pivote para mirar arriba o abajo 
    private float pivotRotationX;


    //Para almacenar el Input del jugador
    private Vector3 input;

    private Rigidbody rb;
    private GroundChecker groundChecker;

    void Start()
    {

        rb = GetComponent<Rigidbody>();
        groundChecker = GetComponent<GroundChecker>();

    }

    // Update is called once per frame
    void Update()
    {

        //Guardar el valor del movimiento en los ejes X Z
        input = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        //Hay que cambiar el vector de input para que sea relativo al transform del jugador siempre
        //se mueva segun hacia donde esta mirando y no relativo al mundo 
        input = transform.TransformDirection(input);


        if (Input.GetButtonDown("Jump") && groundChecker.IsGrounded == true)
        {

            rb.AddForce(Vector3.up * jumpforce, ForceMode.VelocityChange);

        }




        //Rotacion Horizontal 

        transform.Rotate(new Vector3(0, Input.GetAxisRaw("Mouse X") * rotationSpeed, 0));

        //Al valor actual de rotacion le sumamos o restamos segun si subimos o bajamos el raton
        pivotRotationX -= Input.GetAxisRaw("Mouse Y") * rotationSpeed;

        //lIMITAR EL VALOR DE LA ROTACION PARA QUE SIEMPRE ESTE ENTRE LOS VALORES
        pivotRotationX = Mathf.Clamp(pivotRotationX, -90, 90);

        //Actualiza los angulos de rotacion del pivote para que su rotacion en X sea la que hemos calculado con el raton 
        pivot.localEulerAngles = new Vector3(pivotRotationX, 0, 0);

        //Respawn Inicial al f1
        if (Input.GetKeyDown(KeyCode.F1))
        {
            transform.position = new Vector3(0, 1, 0);
        }

    }




    private void FixedUpdate()
    {
        //Si se mantiene Shift la velocidad se multiplica por sprintMultiplier   Mio
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? movementSpeed * sprintMultiplier : movementSpeed; // Mio

        Vector3 movement = input.normalized * currentSpeed;
        //Mantener velocity Y original para no reventar la gravedad 
        movement.y = rb.linearVelocity.y;
        rb.linearVelocity = movement;



    }




}


