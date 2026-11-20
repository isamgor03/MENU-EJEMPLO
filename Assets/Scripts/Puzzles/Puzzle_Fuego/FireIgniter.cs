using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  FireIgniter.cs  (PUZZLE 2: FUEGO, LLUVIA Y CUERDAS)
//  ---------------------------------------------------------------------
//  La "fuente de fuego" del jugador. Tiene dos modos (puedes usar ambos):
//
//  1) Con el clic izquierdo: enciende el inflamable más cercano que tengas
//     DELANTE (dentro de "range" metros y del ángulo "maxAngle"). Va en el
//     jugador. No necesita colliders.
//
//  2) Por contacto: si este objeto es una antorcha en la mano, una flecha
//     de fuego o una bola de fuego, enciende lo que toque. Requiere un
//     Collider marcado como "Is Trigger" y un Rigidbody (puede ser Kinematic)
//     en este objeto o en el inflamable.
// =====================================================================
public class FireIgniter : MonoBehaviour
{
    [Header("Encender con clic izquierdo")]
    [Tooltip("Activa el encendido manual con el clic izquierdo")]
    public bool igniteWithInput = true;

    [Min(0.1f), Tooltip("Distancia máxima a la que puedes encender algo")]
    public float range = 2.5f;

    [Range(5f, 180f), Tooltip("Ángulo máximo respecto a donde miras (60 = cono amplio delante tuyo)")]
    public float maxAngle = 60f;

    [Min(0f), Tooltip("Segundos mínimos entre intentos")]
    public float cooldown = 0.4f;

    [Tooltip("Desde dónde se calcula 'delante'. Si lo dejas vacío usa este mismo objeto")]
    public Transform origin;

    [Tooltip("Efecto opcional que aparece sobre el objeto encendido")]
    public GameObject igniteEffectPrefab;

    [Header("Encender por contacto")]
    [Tooltip("Si está activado, enciende cualquier inflamable que entre en su zona trigger")]
    public bool igniteOnContact = false;

    [Header("Eventos")]
    public UnityEvent onIgnite;   // Cuando consigues encender algo

    float lastTime = -99f;

    // Lee el clic cada frame.
    void Update()
    {
        if (igniteWithInput && PuzzleInput.FireDown()) TryIgniteInFront();
    }

    // Enciende el inflamable ENCENDIBLE más cercano que esté delante. Devuelve true si lo logra.
    public bool TryIgniteInFront()
    {
        if (Time.time - lastTime < cooldown) return false;
        lastTime = Time.time;

        Transform o = origin != null ? origin : transform;
        Flammable best = null;
        float bestDist = range;

        foreach (Flammable f in Flammable.All)
        {
            if (!f.CanIgnite) continue; // Ya arde, está mojado o quemado

            Vector3 to = f.transform.position - o.position;
            float dist = to.magnitude;
            if (dist > bestDist) continue;
            if (Vector3.Angle(o.forward, to) > maxAngle) continue;

            bestDist = dist;
            best = f;
        }

        if (best == null) return false;

        best.TryIgnite();
        if (igniteEffectPrefab != null)
            Destroy(Instantiate(igniteEffectPrefab, best.transform.position, Quaternion.identity), 2f);
        onIgnite.Invoke();
        return true;
    }

    // Modo contacto: enciende lo que entra en el trigger.
    void OnTriggerEnter(Collider other)
    {
        if (!igniteOnContact) return;
        Flammable f = other.GetComponentInParent<Flammable>();
        if (f != null && f.TryIgnite()) onIgnite.Invoke();
    }

    // Muestra el alcance en la ventana Scene.
    void OnDrawGizmosSelected()
    {
        Transform o = origin != null ? origin : transform;
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(o.position, range);
        Gizmos.DrawRay(o.position, o.forward * range);
    }
}
