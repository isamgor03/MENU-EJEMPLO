using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

//Va en el animal / NPC / enemigo. Necesita un NavMeshAgent y el suelo con el NavMesh horneado
//Si hay un cebo cerca va hacia el, se lo come y vuelve a su sitio
[RequireComponent(typeof(NavMeshAgent))]
public class LureAnimal : MonoBehaviour
{

    //A que distancia nota un cebo
    [SerializeField] private float detectRadius = 15f;
    //A que distancia del cebo empieza a comer
    [SerializeField] private float eatDistance = 1.5f;
    //Segundos que tarda en comer
    [SerializeField] private float eatTime = 2f;

    //Para que pasen cositas cuando empieza a comer (animacion, sonido...)
    [SerializeField] private UnityEvent OnEat;

    private NavMeshAgent agent;
    private Vector3 startPosition;
    private Lure target;
    private bool isEating = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        startPosition = transform.position;
        StartCoroutine(DetectCrt());
    }

    IEnumerator DetectCrt()
    {
        while (true)
        {
            if (isEating == false) DetectLure();
            yield return new WaitForSeconds(0.2f);
        }
    }

    void DetectLure()
    {
        //Busca el cebo mas cercano dentro del radio (sin contar la altura)
        target = null;
        float bestDistance = detectRadius;

        foreach (Lure lure in FindObjectsByType<Lure>(FindObjectsSortMode.None))
        {
            Vector3 diff = lure.transform.position - transform.position;
            diff.y = 0;

            if (diff.magnitude < bestDistance)
            {
                bestDistance = diff.magnitude;
                target = lure;
            }
        }

        //Si no hay cebo vuelve a su sitio
        if (target == null)
        {
            agent.SetDestination(startPosition);
        }
        //Si hay cebo va hacia el, y si esta lo bastante cerca se lo come
        else
        {
            agent.SetDestination(target.transform.position);

            if (bestDistance <= eatDistance) StartCoroutine(EatCrt());
        }
    }

    IEnumerator EatCrt()
    {
        isEating = true;
        agent.ResetPath();
        OnEat?.Invoke();

        yield return new WaitForSeconds(eatTime);

        //Se come el cebo (desaparece)
        if (target != null) Destroy(target.gameObject);

        isEating = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        //Dibuja el radio en el que detecta cebos
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }

}
