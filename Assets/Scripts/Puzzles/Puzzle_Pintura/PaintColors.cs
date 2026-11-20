using UnityEngine;

// =====================================================================
//  PaintColors.cs  (PUZZLE 1: PINTURA Y COLORES)
//  ---------------------------------------------------------------------
//  Define los colores del puzzle y la TABLA DE MEZCLAS.
//  No va en ningún objeto: es solo una "librería" que usan los demás scripts.
//
//  Tabla de mezclas (el orden no importa):
//     Rojo     + Amarillo = Naranja
//     Azul     + Amarillo = Verde
//     Rojo     + Azul     = Morado
//     X        + X        = X          (mismo color no cambia)
//     Vacío    + X        = X
//     Cualquier otra combinación = Marrón ("pintura sucia")
//  Si quieres otras reglas, edita el método Mix().
// =====================================================================

// Colores posibles. "None" significa "sin pintura".
public enum PuzzleColor { None, Red, Yellow, Blue, Orange, Green, Purple, Brown }

public static class PaintMixer
{
    // Devuelve el resultado de mezclar dos colores.
    public static PuzzleColor Mix(PuzzleColor a, PuzzleColor b)
    {
        if (a == PuzzleColor.None) return b;   // Sin pintura + X = X
        if (b == PuzzleColor.None) return a;
        if (a == b) return a;                  // Mismo color: no cambia

        if (IsPair(a, b, PuzzleColor.Red, PuzzleColor.Yellow)) return PuzzleColor.Orange;
        if (IsPair(a, b, PuzzleColor.Blue, PuzzleColor.Yellow)) return PuzzleColor.Green;
        if (IsPair(a, b, PuzzleColor.Red, PuzzleColor.Blue)) return PuzzleColor.Purple;

        return PuzzleColor.Brown;              // Mezcla "mala"
    }

    // ¿Son (a, b) la pareja (x, y) en cualquier orden?
    static bool IsPair(PuzzleColor a, PuzzleColor b, PuzzleColor x, PuzzleColor y)
    {
        return (a == x && b == y) || (a == y && b == x);
    }

    // Convierte un PuzzleColor en un color real de Unity para pintar los objetos.
    public static Color ToColor(PuzzleColor c)
    {
        switch (c)
        {
            case PuzzleColor.Red:    return new Color(0.90f, 0.15f, 0.15f);
            case PuzzleColor.Yellow: return new Color(0.98f, 0.85f, 0.10f);
            case PuzzleColor.Blue:   return new Color(0.15f, 0.35f, 0.90f);
            case PuzzleColor.Orange: return new Color(1.00f, 0.50f, 0.05f);
            case PuzzleColor.Green:  return new Color(0.20f, 0.75f, 0.25f);
            case PuzzleColor.Purple: return new Color(0.55f, 0.20f, 0.80f);
            case PuzzleColor.Brown:  return new Color(0.40f, 0.26f, 0.13f);
            default:                 return new Color(0.85f, 0.85f, 0.85f); // None = gris claro
        }
    }
}
