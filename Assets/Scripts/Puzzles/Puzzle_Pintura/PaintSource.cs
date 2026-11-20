using System.Collections.Generic;
using UnityEngine;

// =====================================================================
//  PaintSource.cs  (PUZZLE 1: PINTURA Y COLORES)
//  ---------------------------------------------------------------------
//  Un bote / fuente / charco de tinte de UN color.
//  El jugador se acerca y pulsa E para cargar ese color en su pincel
//  (ver PaintBrush). Si ya llevaba otro color, se MEZCLAN.
//
//  Truco: una fuente con color "None" actúa como FUENTE DE AGUA y limpia
//  el pincel (sirve para quitar una mezcla mala y empezar de nuevo).
//  No necesita Collider: se detecta por distancia.
// =====================================================================
public class PaintSource : MonoBehaviour
{
    // Registro de todas las fuentes activas (así el pincel las encuentra sin usar físicas)
    public static readonly List<PaintSource> All = new List<PaintSource>();

    [Tooltip("Color del tinte. 'None' = agua que limpia el pincel")]
    public PuzzleColor color = PuzzleColor.Red;

    [Tooltip("Renderer que se tiñe del color del tinte. Si lo dejas vacío se busca automáticamente")]
    public Renderer tintRenderer;

    void OnEnable() { if (!All.Contains(this)) All.Add(this); }
    void OnDisable() { All.Remove(this); }

    // Al empezar, pinta el objeto con el color de su tinte para que el jugador lo reconozca.
    void Start()
    {
        if (tintRenderer == null) tintRenderer = GetComponentInChildren<Renderer>();
        PuzzleTint.SetColor(tintRenderer, PaintMixer.ToColor(color));
    }

    // Dibuja una esfera del color en la ventana Scene para ver de un vistazo qué tinte es.
    void OnDrawGizmos()
    {
        Gizmos.color = PaintMixer.ToColor(color);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f, 0.35f);
    }
}
