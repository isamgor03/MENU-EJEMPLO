using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Script ENLAZADO: va en un objeto vacio (por ejemplo "Reloj_Unity").
/// Comprueba que las dos agujas esten en la hora correcta y lanza el evento.
/// </summary>
public class RelojPuzzle : MonoBehaviour
{
    [Header("Agujas (arrastra aqui los objetos con AgujaReloj)")]
    public AgujaReloj agujaHoras;
    public AgujaReloj agujaMinutos;

    [Header("Hora correcta (en pasos de cada aguja)")]
    [Tooltip("Ej: para las 3:00 -> horas = 3, minutos = 0 (con 12 pasos por vuelta en ambas agujas)")]
    public int pasoHorasCorrecto = 3;
    public int pasoMinutosCorrecto = 0;

    [Header("Evento")]
    [Tooltip("Se ejecuta UNA vez cuando ambas agujas estan en la posicion correcta.")]
    public UnityEvent AlResolver;

    public bool resuelto { get; private set; }

    void OnEnable()
    {
        agujaHoras.AlTerminarDeGirar += Comprobar;
        agujaMinutos.AlTerminarDeGirar += Comprobar;
    }

    void OnDisable()
    {
        agujaHoras.AlTerminarDeGirar -= Comprobar;
        agujaMinutos.AlTerminarDeGirar -= Comprobar;
    }

    void Comprobar(AgujaReloj _)
    {
        if (resuelto) return;

        // Solo comprobamos cuando ninguna aguja se esta moviendo
        if (!agujaHoras.EstaQuieta || !agujaMinutos.EstaQuieta) return;

        if (agujaHoras.PasoActual == pasoHorasCorrecto &&
            agujaMinutos.PasoActual == pasoMinutosCorrecto)
        {
            resuelto = true;
            Debug.Log("Puzzle del reloj resuelto");
            AlResolver?.Invoke();
        }
    }
}