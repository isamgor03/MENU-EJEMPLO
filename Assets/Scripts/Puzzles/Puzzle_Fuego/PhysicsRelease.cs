using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  PhysicsRelease.cs  (PUZZLE 2: FUEGO, LLUVIA Y CUERDAS)
//  ---------------------------------------------------------------------
//  Es el CONTRAPESO (o cualquier objeto "colgado"): empieza quieto, sujeto
//  por la cuerda, y cuando llamas a Release() se activa la física y CAE.
//  Conecta FirePuzzleManager.onCompleted -> PhysicsRelease.Release().
//  Lo que haga el objeto al caer (pisar un botón, abrir una puerta con
//  su peso, romper algo) lo decides tú con "onReleased" o con colisiones.
// =====================================================================
[RequireComponent(typeof(Rigidbody))]
public class PhysicsRelease : MonoBehaviour
{
    [Header("Liberación")]
    [Min(0f), Tooltip("Segundos de espera entre llamar a Release() y que el objeto caiga")]
    public float delay = 0f;

    [Tooltip("Empujón inicial al soltarse (por ejemplo 0,-5,0 para que caiga con fuerza)")]
    public Vector3 impulse = Vector3.zero;

    [Header("Eventos")]
    public UnityEvent onReleased;   // Se lanza en el momento en que se suelta

    Rigidbody rb;
    Vector3 startPos;      // Posición inicial (para poder reiniciar)
    Quaternion startRot;   // Rotación inicial

    // Guarda la posición inicial y deja el objeto sujeto (kinematic = ignora la gravedad).
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        startPos = transform.position;
        startRot = transform.rotation;
        rb.isKinematic = true;
    }

    // Suelta el objeto (conéctalo al evento de victoria del puzzle).
    [ContextMenu("Soltar")]
    public void Release()
    {
        StartCoroutine(ReleaseRoutine());
    }

    IEnumerator ReleaseRoutine()
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        rb.isKinematic = false;                          // La física empieza a actuar
        rb.useGravity = true;
        if (impulse != Vector3.zero) rb.AddForce(impulse, ForceMode.Impulse);
        onReleased.Invoke();
    }

    // Vuelve a colgar el objeto en su posición original (útil al reiniciar el puzzle).
    [ContextMenu("Reiniciar posición")]
    public void ResetPose()
    {
        StopAllCoroutines();
        rb.isKinematic = true;   // Al hacerlo kinematic se detiene el movimiento
        transform.SetPositionAndRotation(startPos, startRot);
    }
}
