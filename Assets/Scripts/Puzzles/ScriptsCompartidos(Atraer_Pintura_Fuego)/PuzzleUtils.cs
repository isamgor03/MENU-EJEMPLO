using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =====================================================================
//  PuzzleUtils.cs  (COMPARTIDO por los 3 puzzles)
//  ---------------------------------------------------------------------
//  Contiene dos utilidades pequeñas que usan varios scripts:
//    - PuzzleInput: lee teclas/ratón y funciona tanto con el Input Manager
//      antiguo como con el nuevo Input System (según lo que tengas activado
//      en Edit > Project Settings > Player > Active Input Handling).
//    - PuzzleTint: cambia el color de un Renderer sin duplicar materiales.
// =====================================================================

public static class PuzzleInput
{
    // Tecla E: interactuar (coger tinte, etc.)
    public static bool InteractDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.E)) return true;
#endif
        return false;
    }

    // Clic izquierdo del ratón: disparar / lanzar / encender
    public static bool FireDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetMouseButtonDown(0)) return true;
#endif
        return false;
    }

    // Tecla Q: cambiar de objeto/tipo de cebo
    public static bool CycleDown()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame) return true;
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKeyDown(KeyCode.Q)) return true;
#endif
        return false;
    }
}

public static class PuzzleTint
{
    // Nombres de la propiedad de color según el pipeline de render:
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor"); // URP / HDRP
    static readonly int ColorId = Shader.PropertyToID("_Color");         // Built-in

    static MaterialPropertyBlock block;

    // Cambia el color de un Renderer. Usa un MaterialPropertyBlock, así no se crean
    // materiales nuevos y no se afecta a otros objetos que compartan material.
    public static void SetColor(Renderer r, Color c)
    {
        if (r == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        r.GetPropertyBlock(block);
        block.SetColor(BaseColorId, c);
        block.SetColor(ColorId, c);
        r.SetPropertyBlock(block);
    }
}
