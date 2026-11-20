using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  LureItem.cs  (PUZZLE 3: ATENCIÓN DE ANIMALES / NPC / ENEMIGOS)
//  ---------------------------------------------------------------------
//  Es el CEBO: comida, un objeto brillante, una piedra que hace ruido...
//  Va en el PREFAB que lanza el jugador (junto a un Rigidbody y un Collider).
//
//  Funcionamiento:
//    1. Vuela por el aire tras el lanzamiento (aún no atrae a nadie).
//    2. Al chocar con algo (o pasados "maxFlightSeconds") se ACTIVA y se
//       registra como punto de interés: los Attractable cercanos que
//       acepten este tipo de cebo irán hacia él.
//    3. Desaparece cuando alguien se lo come ("consumedWhenEaten") o al
//       pasar su "lifetime".
// =====================================================================

// Tipos de cebo. Cada Attractable decide cuáles le interesan.
public enum LureKind { Food, Meat, Noise, Shiny }

public class LureItem : MonoBehaviour
{
    // Registro de cebos ACTIVOS (los animales los consultan para decidir adónde ir)
    public static readonly List<LureItem> Active = new List<LureItem>();

    [Header("Tipo")]
    [Tooltip("Tipo de cebo. Un animal solo reacciona a los tipos que tenga en su lista 'acceptedKinds'")]
    public LureKind kind = LureKind.Food;

    [Header("Alcance")]
    [Min(0f), Tooltip("Distancia (metros) a la que este cebo se nota (se compara con el radio de percepción del animal)")]
    public float attractRadius = 15f;

    [Header("Vida")]
    [Min(0f), Tooltip("Segundos que dura el cebo una vez activo. 0 = no caduca")]
    public float lifetime = 20f;

    [Tooltip("Si está activado, el cebo desaparece cuando un animal termina de comérselo")]
    public bool consumedWhenEaten = true;

    [Header("Activación")]
    [Tooltip("Se activa al primer choque (con el suelo, una plataforma...)")]
    public bool activateOnFirstCollision = true;

    [Min(0.1f), Tooltip("Si no choca con nada, se activa solo pasados estos segundos")]
    public float maxFlightSeconds = 3f;

    [Header("Eventos")]
    public UnityEvent onLanded;     // Se activa (aterriza)
    public UnityEvent onConsumed;   // Un animal se lo come
    public UnityEvent onExpired;    // Caduca sin que nadie lo coma

    bool isActive;        // ¿Ya está atrayendo?
    float flightTimer;    // Tiempo en vuelo
    float activeTimer;    // Tiempo activo

    public bool IsActive => isActive;
    public Vector3 Position => transform.position;

    // Cuenta el vuelo (para activarse solo) y la vida (para caducar).
    void Update()
    {
        if (!isActive)
        {
            flightTimer += Time.deltaTime;
            if (flightTimer >= maxFlightSeconds) Activate();
        }
        else if (lifetime > 0f)
        {
            activeTimer += Time.deltaTime;
            if (activeTimer >= lifetime)
            {
                onExpired.Invoke();
                Despawn();
            }
        }
    }

    // Al chocar por primera vez, se activa.
    void OnCollisionEnter(Collision collision)
    {
        if (activateOnFirstCollision && !isActive) Activate();
    }

    // Lo usa el lanzador en modo "colocar en el punto apuntado" (no hay vuelo).
    public void ActivateNow() { Activate(); }

    void Activate()
    {
        if (isActive) return;
        isActive = true;
        if (!Active.Contains(this)) Active.Add(this);
        onLanded.Invoke();
    }

    // Lo llama el animal cuando termina de comérselo.
    public void Consume()
    {
        if (!consumedWhenEaten) return;
        onConsumed.Invoke();
        Despawn();
    }

    void Despawn()
    {
        isActive = false;
        Active.Remove(this);
        Destroy(gameObject);
    }

    void OnDisable() { Active.Remove(this); }

    // Muestra el radio de atracción en la ventana Scene.
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 1f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, attractRadius);
    }
}
