using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  RainController.cs  (PUZZLE 2: FUEGO, LLUVIA Y CUERDAS)
//  ---------------------------------------------------------------------
//  Controla la LLUVIA del puzzle. Va en un objeto vacío de la escena.
//    - Tras "delayBeforeRain" segundos empieza a llover.
//    - Mientras llueve, apaga y moja todos los Flammable (menos los que
//      estén "sheltered" o fuera de la zona afectada).
//    - Cuando para de llover, los objetos se van secando (wetSeconds).
//    - Puede repetirse en ciclos (llueve / pausa / llueve...).
//  Así creas la presión de "enciende las antorchas ANTES de que llueva".
// =====================================================================
public class RainController : MonoBehaviour
{
    // ================== CUÁNDO LLUEVE ==================
    [Header("Cuándo llueve")]
    [Tooltip("Si está activado, la cuenta atrás empieza al arrancar la escena")]
    public bool autoStart = true;

    [Min(0f), Tooltip("Segundos hasta que empieza a llover por primera vez")]
    public float delayBeforeRain = 20f;

    [Min(0f), Tooltip("Cuánto dura la lluvia en segundos. 0 = llueve para siempre")]
    public float rainDuration = 10f;

    [Tooltip("Si está activado, la lluvia se repite en ciclos")]
    public bool repeat = false;

    [Min(0f), Tooltip("Segundos de pausa entre una lluvia y la siguiente (solo si repeat está activado)")]
    public float pauseBetweenRains = 15f;

    // ================== ZONA Y EFECTO ==================
    [Header("Zona y efectos")]
    [Tooltip("Opcional: si asignas un Collider, solo se mojan los inflamables DENTRO de su área")]
    public Collider affectedZone;

    [Min(0.05f), Tooltip("Cada cuántos segundos se aplica la lluvia a los inflamables")]
    public float tickInterval = 0.25f;

    [Tooltip("Partículas de lluvia (opcional). Se encienden y apagan solas")]
    public ParticleSystem rainParticles;

    [Tooltip("Sonido de lluvia en bucle (opcional). Se enciende y apaga solo")]
    public AudioSource rainAudio;

    // ================== EVENTOS ==================
    [Header("Eventos")]
    public UnityEvent onRainStart;
    public UnityEvent onRainStop;

    [Header("Debug (solo lectura)")]
    [SerializeField] bool raining;
    [SerializeField] bool counting;   // ¿Está corriendo la cuenta atrás?

    float phaseTimer;     // Tiempo en la fase actual (esperando o lloviendo)
    float currentWait;    // Espera necesaria antes de la próxima lluvia
    float tickTimer;      // Para aplicar la lluvia cada tickInterval

    static readonly List<Flammable> scratch = new List<Flammable>();

    public bool IsRaining => raining;

    // Segundos que faltan para que empiece a llover (útil para un contador en la UI). 0 si ya llueve.
    public float TimeUntilRain => (counting && !raining) ? Mathf.Max(0f, currentWait - phaseTimer) : 0f;

    void Start()
    {
        StopParticlesAndAudio();
        if (autoStart) BeginCountdown();
    }

    // Avanza la cuenta atrás y aplica la lluvia cuando toca.
    void Update()
    {
        if (!counting) return;

        phaseTimer += Time.deltaTime;

        if (!raining)
        {
            if (phaseTimer >= currentWait) StartRain();
        }
        else
        {
            tickTimer += Time.deltaTime;
            if (tickTimer >= tickInterval)
            {
                tickTimer = 0f;
                ApplyRain();
            }

            if (rainDuration > 0f && phaseTimer >= rainDuration) StopRain();
        }
    }

    // ---------- API pública ----------

    // Empieza la cuenta atrás desde cero (llámalo desde un trigger si no usas autoStart).
    [ContextMenu("Empezar cuenta atrás")]
    public void BeginCountdown()
    {
        raining = false;
        counting = true;
        phaseTimer = 0f;
        currentWait = delayBeforeRain;
        StopParticlesAndAudio();
    }

    // Hace llover YA, sin esperar.
    [ContextMenu("Llover ahora")]
    public void StartRain()
    {
        if (raining) return;
        raining = true;
        counting = true;
        phaseTimer = 0f;
        tickTimer = 0f;

        if (rainParticles != null) rainParticles.Play();
        if (rainAudio != null) rainAudio.Play();
        onRainStart.Invoke();
        ApplyRain(); // Aplica la lluvia inmediatamente
    }

    // Para la lluvia YA.
    [ContextMenu("Parar lluvia")]
    public void StopRain()
    {
        if (!raining) return;
        raining = false;
        phaseTimer = 0f;
        StopParticlesAndAudio();
        onRainStop.Invoke();

        if (repeat)
        {
            currentWait = pauseBetweenRains;   // Prepara la siguiente lluvia
        }
        else
        {
            counting = false;                  // No vuelve a llover
        }
    }

    // ---------- Lógica interna ----------

    // Moja y apaga todos los inflamables afectados por la lluvia.
    void ApplyRain()
    {
        scratch.Clear();
        scratch.AddRange(Flammable.All);

        foreach (Flammable f in scratch)
        {
            if (f == null || f.sheltered) continue;
            if (affectedZone != null && !affectedZone.bounds.Contains(f.transform.position)) continue;
            f.Wet();
        }
    }

    void StopParticlesAndAudio()
    {
        if (rainParticles != null) rainParticles.Stop();
        if (rainAudio != null) rainAudio.Stop();
    }

    // Dibuja la zona afectada en la ventana Scene.
    void OnDrawGizmosSelected()
    {
        if (affectedZone == null) return;
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.8f);
        Gizmos.DrawWireCube(affectedZone.bounds.center, affectedZone.bounds.size);
    }
}
