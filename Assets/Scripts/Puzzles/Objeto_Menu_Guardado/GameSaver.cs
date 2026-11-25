using System.IO;
using UnityEngine;

//Los datos que se guardan en el archivo. Si quieres guardar algo mas, anade aqui una variable
//y rellenala en Save() y leela en Load()
[System.Serializable]
public class SaveData
{

    public Vector3 position;
    public float rotationY;
    public int skillPoints;
    public int maxHealth;
    public int damage;
    public float speed;
    public int weapon;

}

//Va en el JUGADOR (junto a PlayerStats y WeaponInventory)
//Guarda y carga la partida en un archivo (save.json)
public class GameSaver : MonoBehaviour
{

    //Si esta marcado, al empezar la escena carga la partida guardada (si existe)
    [SerializeField] private bool loadOnStart = true;

    private PlayerStats stats;
    private WeaponInventory weapons;
    private string path;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        weapons = GetComponent<WeaponInventory>();
        path = Application.persistentDataPath + "/save.json";
    }

    void Start()
    {
        if (loadOnStart == true) Load();
    }

    //Guarda la partida (el boton Guardar del menu llama a esto)
    [ContextMenu("Guardar")]
    public void Save()
    {
        SaveData data = new SaveData();
        data.position = transform.position;
        data.rotationY = transform.eulerAngles.y;
        data.skillPoints = stats.skillPoints;
        data.maxHealth = stats.maxHealth;
        data.damage = stats.damage;
        data.speed = stats.speed;
        data.weapon = weapons.CurrentIndex;

        File.WriteAllText(path, JsonUtility.ToJson(data, true));
        Debug.Log("Partida guardada en " + path);
    }

    //Carga la partida guardada (si no hay ninguna, no hace nada)
    [ContextMenu("Cargar")]
    public void Load()
    {
        if (File.Exists(path) == false) return;

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));

        //Coloca al jugador donde estaba y le quita la velocidad para que no salga disparado
        transform.position = data.position;
        transform.rotation = Quaternion.Euler(0, data.rotationY, 0);
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.linearVelocity = Vector3.zero;

        //Devuelve las stats y el arma
        stats.skillPoints = data.skillPoints;
        stats.maxHealth = data.maxHealth;
        stats.damage = data.damage;
        stats.speed = data.speed;
        stats.ApplyStats();
        weapons.Equip(data.weapon);
    }

    //Borra la partida guardada (para empezar de cero)
    [ContextMenu("Borrar partida")]
    public void DeleteSave()
    {
        if (File.Exists(path) == true) File.Delete(path);
    }

}
