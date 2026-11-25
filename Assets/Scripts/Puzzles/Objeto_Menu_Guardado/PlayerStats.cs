using UnityEngine;

//Va en el JUGADOR. Guarda las estadisticas y los puntos para mejorarlas
//La velocidad se aplica sola al controlador. La vida y el dano los tienen que LEER tus otros scripts
//(por ejemplo: stats.maxHealth en tu vida, stats.damage en tu ataque)
public class PlayerStats : MonoBehaviour
{

    //Puntos que se pueden gastar en mejorar
    public int skillPoints = 3;

    //Stats actuales
    public int maxHealth = 100;
    public int damage = 10;
    public float speed = 4f;

    //Cuanto sube cada stat con una mejora
    [SerializeField] private int healthPerUpgrade = 10;
    [SerializeField] private int damagePerUpgrade = 2;
    [SerializeField] private float speedPerUpgrade = 0.3f;

    //El controlador del jugador, para aplicarle la velocidad (opcional)
    [SerializeField] private FirstPersonControllerRigidBody controller;

    void Awake()
    {
        //Coge la velocidad que ya tiene el controlador como valor inicial
        if (controller != null) speed = controller.movementSpeed;
    }

    //Las tres funciones devuelven false si no quedan puntos
    public bool UpgradeHealth()
    {
        if (SpendPoint() == false) return false;

        maxHealth += healthPerUpgrade;
        return true;
    }

    public bool UpgradeDamage()
    {
        if (SpendPoint() == false) return false;

        damage += damagePerUpgrade;
        return true;
    }

    public bool UpgradeSpeed()
    {
        if (SpendPoint() == false) return false;

        speed += speedPerUpgrade;
        ApplyStats();
        return true;
    }

    bool SpendPoint()
    {
        if (skillPoints <= 0) return false;

        skillPoints--;
        return true;
    }

    //Da puntos de mejora (al subir de nivel, al resolver un puzzle... se puede llamar desde un UnityEvent)
    public void AddPoints(int amount)
    {
        skillPoints += amount;
    }

    //Aplica la velocidad al controlador
    public void ApplyStats()
    {
        if (controller != null) controller.movementSpeed = speed;
    }

}
