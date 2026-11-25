using UnityEngine;
using TMPro;

//Va en un objeto que SIEMPRE este activo (el Canvas o un objeto vacio). Es el menu del juego
//Se abre desde un SavePoint y se cierra con Esc o con el boton Cerrar
//Los botones de la interfaz llaman a las funciones publicas de este script
public class GameMenu : MonoBehaviour
{

    //El jugador (arrastra el objeto Player: ahi estan PlayerStats, WeaponInventory y GameSaver)
    [SerializeField] private GameObject player;

    //Todo el menu (fondo, texto y paneles). Se activa al abrir y se desactiva al cerrar
    [SerializeField] private GameObject menuRoot;
    //Los tres paneles del menu (solo se ve uno a la vez)
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject statsPanel;
    [SerializeField] private GameObject weaponsPanel;
    //Texto con la informacion (puntos, stats, arma y mensajes)
    [SerializeField] private TMP_Text infoText;

    //Scripts del jugador que se apagan mientras el menu esta abierto (controlador, ataques...)
    [SerializeField] private MonoBehaviour[] scriptsToDisable;
    //Si esta marcado, el juego se pausa mientras el menu esta abierto
    [SerializeField] private bool pauseGame = true;
    //Si esta marcado, al cerrar el menu el raton se vuelve a bloquear (juego en primera persona)
    [SerializeField] private bool lockCursorOnClose = true;

    private PlayerStats stats;
    private WeaponInventory weapons;
    private GameSaver saver;
    private Rigidbody playerBody;

    private bool isOpen = false;
    //Ultimo mensaje que se muestra en el texto ("Partida guardada"...)
    private string message = "";

    public bool IsOpen => isOpen;

    void Start()
    {
        stats = player.GetComponent<PlayerStats>();
        weapons = player.GetComponent<WeaponInventory>();
        saver = player.GetComponent<GameSaver>();
        playerBody = player.GetComponent<Rigidbody>();

        //El menu empieza cerrado
        menuRoot.SetActive(false);
    }

    void Update()
    {
        //Esc cierra el menu
        if (isOpen == true && Input.GetKeyDown(KeyCode.Escape)) Close();
    }

    //Abre el menu (lo llama el SavePoint)
    public void Open()
    {
        if (isOpen == true) return;

        isOpen = true;
        message = "";
        menuRoot.SetActive(true);
        ShowMain();

        //Apaga el controlador y los ataques para que el personaje no se mueva ni dispare
        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null) script.enabled = false;
        }
        if (playerBody != null) playerBody.linearVelocity = Vector3.zero;

        if (pauseGame == true) Time.timeScale = 0f;

        //Libera el raton para poder pulsar los botones
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    //Cierra el menu (boton Cerrar)
    public void Close()
    {
        isOpen = false;
        menuRoot.SetActive(false);

        foreach (MonoBehaviour script in scriptsToDisable)
        {
            if (script != null) script.enabled = true;
        }

        if (pauseGame == true) Time.timeScale = 1f;

        if (lockCursorOnClose == true)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    //---------- Cambiar de panel (botones Mejorar stats, Cambiar arma, Volver) ----------

    public void ShowMain()
    {
        SetPanels(true, false, false);
    }

    public void ShowStats()
    {
        SetPanels(false, true, false);
    }

    public void ShowWeapons()
    {
        SetPanels(false, false, true);
    }

    void SetPanels(bool main, bool statsOn, bool weaponsOn)
    {
        mainPanel.SetActive(main);
        statsPanel.SetActive(statsOn);
        weaponsPanel.SetActive(weaponsOn);
        Refresh();
    }

    //---------- Funciones de los botones ----------

    public void SaveGame()
    {
        saver.Save();
        message = "Partida guardada";
        Refresh();
    }

    public void UpgradeHealth()
    {
        message = stats.UpgradeHealth() ? "Vida mejorada" : "No quedan puntos";
        Refresh();
    }

    public void UpgradeDamage()
    {
        message = stats.UpgradeDamage() ? "Daño mejorado" : "No quedan puntos";
        Refresh();
    }

    public void UpgradeSpeed()
    {
        message = stats.UpgradeSpeed() ? "Velocidad mejorada" : "No quedan puntos";
        Refresh();
    }

    //Equipa el arma con ese numero (0 = la primera de la lista). Cada boton de arma pone su numero
    public void EquipWeapon(int index)
    {
        weapons.Equip(index);
        message = "Arma equipada: " + weapons.CurrentName;
        Refresh();
    }

    //Pasa a la siguiente arma
    public void NextWeapon()
    {
        weapons.NextWeapon();
        message = "Arma equipada: " + weapons.CurrentName;
        Refresh();
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        //En el editor no se puede cerrar el juego, asi que se para el Play
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    //Actualiza el texto del menu
    void Refresh()
    {
        if (infoText == null) return;

        infoText.text = "Puntos de mejora: " + stats.skillPoints
            + "\nVida: " + stats.maxHealth
            + "\nDaño: " + stats.damage
            + "\nVelocidad: " + stats.speed.ToString("0.0")
            + "\nArma: " + weapons.CurrentName
            + "\n\n" + message;
    }

}
