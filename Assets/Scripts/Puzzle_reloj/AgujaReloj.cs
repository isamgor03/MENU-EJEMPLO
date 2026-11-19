using UnityEngine;

/// <summary>
/// Script BASE: va en cada aguja (Flecha_Horas y Flecha_Minutos).
/// La aguja solo puede quedarse en posiciones exactas ("pasos").
///  - Aguja de horas  -> pasosPorVuelta = 12  (30 grados por paso)
///  - Aguja de minutos -> pasosPorVuelta = 12 (5 min por paso) o 60 (1 min por paso)
/// Se gira con Quaternion.Slerp y al llegar se "encaja" en la rotacion exacta.
/// </summary>
public class AgujaReloj : MonoBehaviour
{
    [Header("Aguja")]
    [Tooltip("Transform que gira. Si lo dejas vacio usa este mismo objeto.")]
    public Transform aguja;

    [Tooltip("Eje LOCAL sobre el que gira la aguja (Z si la aguja mira al frente del reloj, prueba Y o X si tu modelo esta rotado).")]
    public Vector3 ejeGiro = Vector3.forward;

    [Tooltip("Marca esta casilla si la aguja gira al reves de las agujas del reloj.")]
    public bool invertirSentido = false;

    [Header("Pasos")]
    [Tooltip("Cuantas posiciones tiene una vuelta completa (12 = horas, 60 = minutos).")]
    public int pasosPorVuelta = 12;

    [Tooltip("Paso en el que empieza la aguja segun como la dejaste en la escena (0 = apuntando a las 12).")]
    public int pasoInicial = 0;

    [Header("Movimiento")]
    public float velocidad = 8f;          // mas alto = gira mas rapido
    public float margenEncaje = 0.1f;     // grados a partir de los cuales se encaja exacta

    /// <summary>Paso actual (0 a pasosPorVuelta-1). Es la "posicion logica" de la aguja.</summary>
    public int PasoActual { get; private set; }

    /// <summary>True cuando la aguja ya ha terminado de girar y esta quieta.</summary>
    public bool EstaQuieta { get; private set; } = true;

    /// <summary>Se dispara cuando la aguja termina de moverse (lo usa el puzzle).</summary>
    public event System.Action<AgujaReloj> AlTerminarDeGirar;

    Quaternion rotacionBase;   // rotacion correspondiente al paso 0
    Quaternion rotacionObjetivo;

    float GradosPorPaso => 360f / pasosPorVuelta;

    void Awake()
    {
        if (aguja == null) aguja = transform;
        ejeGiro = ejeGiro.normalized;

        PasoActual = Mod(pasoInicial, pasosPorVuelta);

        // Calculamos la rotacion del paso 0 a partir de la rotacion actual
        rotacionBase = aguja.localRotation * Quaternion.Inverse(RotacionDePaso(PasoActual));
        rotacionObjetivo = aguja.localRotation;
    }

    void Update()
    {
        if (EstaQuieta) return;

        aguja.localRotation = Quaternion.Slerp(aguja.localRotation, rotacionObjetivo, velocidad * Time.deltaTime);

        // Cuando casi ha llegado, la dejamos EXACTA (el lerp nunca llega solo al valor final)
        if (Quaternion.Angle(aguja.localRotation, rotacionObjetivo) < margenEncaje)
        {
            aguja.localRotation = rotacionObjetivo;
            EstaQuieta = true;
            AlTerminarDeGirar?.Invoke(this);
        }
    }

    // ---------- METODOS PARA LOS BOTONES (OnClick) ----------

    /// <summary>Gira N pasos. +1 = sentido horario, -1 = antihorario.</summary>
    public void Girar(int pasos)
    {
        IrAlPaso(PasoActual + pasos);
    }

    /// <summary>Va a un paso concreto (se usa modulo, asi 12 = 0).</summary>
    public void IrAlPaso(int paso)
    {
        PasoActual = Mod(paso, pasosPorVuelta);
        rotacionObjetivo = rotacionBase * RotacionDePaso(PasoActual);
        EstaQuieta = false;
    }

    // ---------- AUXILIARES ----------

    Quaternion RotacionDePaso(int paso)
    {
        float grados = paso * GradosPorPaso * (invertirSentido ? 1f : -1f);
        return Quaternion.AngleAxis(grados, ejeGiro);
    }

    static int Mod(int a, int n) => ((a % n) + n) % n;
}