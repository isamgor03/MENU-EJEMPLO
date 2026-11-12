using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;   // Para EventSystem (seleccionar botones con teclado/mando)
using UnityEngine.InputSystem;    // Nuevo Input System (Unity 6 por defecto)
using UnityEngine.UI;             // Para Selectable (Button, Toggle, Slider...)

// ---------------------------------------------------------------------------
// Esta clase representa UN menú de tu juego. Al ser [System.Serializable]
// aparece como una lista editable en el Inspector.
// Cada menú tiene: su panel, su cámara y el primer botón que se selecciona.
// ---------------------------------------------------------------------------
[System.Serializable]
public class MenuEntry
{
    public string nombre;          // Solo para identificarlo en el Inspector (ej: "Principal")
    public GameObject panel;       // Canvas o Panel de este menú
    public GameObject camara;      // Cámara que se usa cuando este menú está abierto
    public Selectable primerBoton; // Botón que queda seleccionado al entrar al menú


    [Header("Conexiones automáticas")]
    public Button botonQueLoAbre;   // Botón que, al pulsarlo, abre ESTE menú
    public Button botonVolver;      // (Opcional) botón "Volver" dentro de este menú
}

public class MenuOPcionesTeclas : MonoBehaviour
{
    [Header("Lista de menús (el orden define su número)")]
    [Tooltip("Elemento 0 = Menú principal, 1 = Partidas guardadas, 2 = Opciones, etc.")]
    [SerializeField] private List<MenuEntry> menus = new List<MenuEntry>();

    [Header("Configuración")]
    [Tooltip("Número (posición en la lista) del menú principal")]
    [SerializeField] private int indiceMenuPrincipal = 0;

    [Tooltip("Si está activado, Escape vuelve SIEMPRE al menú principal.\n" +
             "Si no, vuelve al menú anterior (paso a paso).")]
    [SerializeField] private bool escapeVuelveAlPrincipal = false;

    // Historial de menús visitados. Guardamos el NÚMERO del menú, no el objeto.
    // El último que entra (Peek) es el menú que se ve ahora mismo.
    private readonly Stack<int> historial = new Stack<int>();

    // -----------------------------------------------------------------------
    // Start: se ejecuta una vez al empezar. Deja visible solo el menú principal.
    // -----------------------------------------------------------------------
    private void Start()
    {
        // Conectamos cada botón con el menú al que lleva
        for (int i = 0; i < menus.Count; i++)
        {
            int indice = i; // Copia local: necesaria para que cada botón recuerde SU número

            if (menus[i].botonQueLoAbre != null)
                menus[i].botonQueLoAbre.onClick.AddListener(() => AbrirMenu(indice));

            if (menus[i].botonVolver != null)
                menus[i].botonVolver.onClick.AddListener(Volver);
        }

        historial.Push(indiceMenuPrincipal);
        MostrarMenu(indiceMenuPrincipal);
    }

    // -----------------------------------------------------------------------
    // Update: se ejecuta cada frame. Comprueba si se ha pulsado Escape.
    // wasPressedThisFrame = true solo en el frame en que se pulsa (no mientras se mantiene).
    // -----------------------------------------------------------------------
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Debug.Log("Escape pulsado. Menú actual: " + historial.Peek());
            VolverAlPrincipal();
        }
    }


    // Escape llama a esta función: vuelve directo al menú principal
    public void VolverAlPrincipal()
    {
        // Si ya estamos en el principal, no hacemos nada
        if (historial.Peek() == indiceMenuPrincipal) return;

        historial.Clear();                      // Borramos el historial
        historial.Push(indiceMenuPrincipal);    // Solo queda el principal
        MostrarMenu(indiceMenuPrincipal);       // Panel + cámara + primer botón
    }

    // -----------------------------------------------------------------------
    // AbrirMenu: llámalo desde el OnClick de un botón, pasando el número del menú.
    // Ejemplo: botón "Cargar Partida" -> AbrirMenu(1)
    // -----------------------------------------------------------------------
    public void AbrirMenu(int indice)
    {
        // Evitamos errores si el número no existe en la lista
        if (indice < 0 || indice >= menus.Count) return;

        // Si ya estamos en ese menú, no hacemos nada
        if (historial.Peek() == indice) return;

        historial.Push(indice); // Lo guardamos en el historial
        MostrarMenu(indice);    // Y lo mostramos
    }

    // -----------------------------------------------------------------------
    // Volver: quita el menú actual del historial y muestra el anterior.
    // También puedes enlazarlo a un botón "Volver" del juego.
    // -----------------------------------------------------------------------
    public void Volver()
    {
        // Si solo queda un menú en el historial (el principal), no hay a dónde volver
        if (historial.Count <= 1) return;

        if (escapeVuelveAlPrincipal)
        {
            // Vaciamos el historial hasta que solo quede el menú principal
            while (historial.Count > 1) historial.Pop();
        }
        else
        {
            // Quitamos solo el menú actual; el que queda arriba es el anterior
            historial.Pop();
        }

        // Mostramos el que ha quedado arriba del historial (cámara y botón incluidos)
        MostrarMenu(historial.Peek());
    }

    // -----------------------------------------------------------------------
    // MostrarMenu: la función central. Para el menú indicado:
    //   1) Activa su panel y su cámara
    //   2) Desactiva los paneles y cámaras de todos los demás
    //   3) Selecciona su primer botón
    // -----------------------------------------------------------------------
    private void MostrarMenu(int indice)
    {
        // 1) Apagamos TODOS los paneles y cámaras
        foreach (MenuEntry m in menus)
        {
            if (m.panel != null) m.panel.SetActive(false);
            if (m.camara != null) m.camara.SetActive(false);
        }

        // 2) Encendemos solo lo del menú actual (aunque comparta cámara con otro)
        MenuEntry actual = menus[indice];
        if (actual.panel != null) actual.panel.SetActive(true);
        if (actual.camara != null) actual.camara.SetActive(true);

        // 3) Seleccionamos el primer botón (esperando un frame, ver abajo)
        StartCoroutine(SeleccionarTrasUnFrame(indice));
    }

    private System.Collections.IEnumerator SeleccionarTrasUnFrame(int indice)
    {
        yield return null; // Espera a que el panel recién activado esté listo
        SeleccionarPrimerBoton(indice);
    }

    // -----------------------------------------------------------------------
    // SeleccionarPrimerBoton: deja marcado un botón para poder navegar con
    // flechas/teclas/mando sin usar el ratón.
    // -----------------------------------------------------------------------
    private void SeleccionarPrimerBoton(int indice)
    {
        Selectable boton = menus[indice].primerBoton;

        // Necesitas un EventSystem en la escena (se crea solo con un Canvas)
        if (boton == null || EventSystem.current == null) return;

        // Primero se deselecciona lo que hubiera: así el botón se resalta
        // correctamente aunque ya estuviera seleccionado de antes.
        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(boton.gameObject);
    }
}