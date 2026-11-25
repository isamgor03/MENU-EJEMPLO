using UnityEngine;

//Va en el JUGADOR. Guarda las armas y cual esta equipada
//Las armas son objetos (el modelo de cada arma) puestos como hijos del jugador / camara / mano
//Solo una esta activa a la vez
public class WeaponInventory : MonoBehaviour
{

    //Las armas del jugador. El orden importa: la primera es la 0, la segunda la 1, etc.
    [SerializeField] private GameObject[] weapons;

    private int currentIndex = 0;

    public int CurrentIndex => currentIndex;

    //Nombre del arma equipada (el nombre del objeto en la Hierarchy)
    public string CurrentName => weapons.Length > 0 ? weapons[currentIndex].name : "Ninguna";

    void Start()
    {
        Equip(currentIndex);
    }

    //Equipa el arma con ese numero y desactiva las demas
    public void Equip(int index)
    {
        if (index < 0 || index >= weapons.Length) return;

        currentIndex = index;

        for (int i = 0; i < weapons.Length; i++)
        {
            weapons[i].SetActive(i == currentIndex);
        }
    }

    //Pasa a la siguiente arma (despues de la ultima vuelve a la primera)
    public void NextWeapon()
    {
        if (weapons.Length == 0) return;

        Equip((currentIndex + 1) % weapons.Length);
    }

}
