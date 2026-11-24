using System.Collections;
using UnityEngine;
using UnityEngine.Events;

//Convierte un objeto en inflamable (cuerda, antorcha, hierba...)
//Necesita un Collider para que lo detecte el rayo del jugador y el contagio del fuego
public class Flammable : MonoBehaviour
{

    //El puzzle al que pertenece (solo si es uno de sus objetivos)
    [HideInInspector] public FirePuzzle assignedPuzzle;

    //Segundos que arde antes de consumirse. 0 = arde para siempre (antorcha)
    [SerializeField] private float burnTime = 3f;
    //Radio en el que contagia el fuego a otros inflamables (0 = no contagia)
    [SerializeField] private float spreadRadius = 1f;
    //Cada cuantos segundos contagia
    [SerializeField] private float spreadDelay = 0.5f;
    //Si esta marcado, la lluvia NO lo apaga (antorcha bajo un techo)
    public bool protectedFromRain = false;
    //Si esta marcado, empieza ya encendido (sirve tambien para probar)
    [SerializeField] private bool startBurning = false;
    //Si esta marcado, desaparece al consumirse (cuerdas)
    [SerializeField] private bool hideWhenBurnt = true;
    //Particulas de fuego. Tiene que ser un HIJO de este objeto, NO el propio objeto
    [SerializeField] private GameObject fireEffect;

    //Eventos para que pasen cositas (sonidos, soltar algo...)
    [SerializeField] private UnityEvent OnIgnite;
    [SerializeField] private UnityEvent OnBurnt;

    private bool isBurning = false;
    private bool isBurnt = false;

    void Start()
    {
        if (fireEffect != null) fireEffect.SetActive(false);
        if (startBurning == true) Ignite();
    }

    //Enciende el objeto
    public void Ignite()
    {
        //No se puede encender si ya arde o ya esta quemado
        if (isBurning == true || isBurnt == true) return;

        isBurning = true;
        if (fireEffect != null) fireEffect.SetActive(true);
        OnIgnite?.Invoke();
        StartCoroutine(BurnCrt());
    }

    //Apaga el fuego (la lluvia llama a esto)
    public void Extinguish()
    {
        if (isBurning == false) return;

        isBurning = false;
        StopAllCoroutines();
        if (fireEffect != null) fireEffect.SetActive(false);

        //Si era una antorcha, ya no cuenta como encendida
        if (burnTime <= 0) assignedPuzzle?.RemoveTarget();
    }

    IEnumerator BurnCrt()
    {
        //Si arde para siempre (antorcha) cuenta como completado nada mas encenderse
        if (burnTime <= 0) assignedPuzzle?.CompletedTarget();

        float timer = 0f;

        //Mientras arde, contagia el fuego a los vecinos
        while (burnTime <= 0 || timer < burnTime)
        {
            Spread();
            yield return new WaitForSeconds(spreadDelay);
            timer += spreadDelay;
        }

        Burn();
    }

    //Enciende todos los inflamables que esten cerca
    void Spread()
    {
        if (spreadRadius <= 0) return;

        Collider[] nearby = Physics.OverlapSphere(transform.position, spreadRadius);

        foreach (Collider other in nearby)
        {
            Flammable flammable = other.GetComponentInParent<Flammable>();

            if (flammable != null) flammable.Ignite();
        }
    }

    //El objeto se consume del todo
    void Burn()
    {
        isBurning = false;
        isBurnt = true;
        if (fireEffect != null) fireEffect.SetActive(false);

        OnBurnt?.Invoke();
        //Si es una cuerda, cuenta como completado cuando se quema del todo
        assignedPuzzle?.CompletedTarget();

        if (hideWhenBurnt == true) gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.orange;
        //Dibuja el radio de contagio
        Gizmos.DrawWireSphere(transform.position, spreadRadius);
    }

}
