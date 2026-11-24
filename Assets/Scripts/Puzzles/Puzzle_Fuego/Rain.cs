using System.Collections;
using UnityEngine;

//Va en un objeto vacio. Pasados unos segundos llueve y la lluvia apaga todos los Flammable
//(menos los que tengan marcado "Protected From Rain"). Es opcional: el puzzle funciona sin ella
public class Rain : MonoBehaviour
{

    //Segundos hasta que empieza a llover
    [SerializeField] private float delay = 20f;
    //Segundos que dura la lluvia
    [SerializeField] private float duration = 10f;
    //Particulas de lluvia (opcional)
    [SerializeField] private GameObject rainEffect;

    void Start()
    {
        if (rainEffect != null) rainEffect.SetActive(false);
        StartCoroutine(RainCrt());
    }

    IEnumerator RainCrt()
    {
        yield return new WaitForSeconds(delay);

        if (rainEffect != null) rainEffect.SetActive(true);

        float timer = 0f;

        while (timer < duration)
        {
            //Apaga todo lo que arda y no este a cubierto
            foreach (Flammable flammable in FindObjectsByType<Flammable>(FindObjectsSortMode.None))
            {
                if (flammable.protectedFromRain == false) flammable.Extinguish();
            }

            yield return new WaitForSeconds(0.25f);
            timer += 0.25f;
        }

        if (rainEffect != null) rainEffect.SetActive(false);
    }

}
