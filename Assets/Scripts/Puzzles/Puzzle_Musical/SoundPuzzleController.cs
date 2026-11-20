using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// =====================================================================
//  SoundPuzzleController
//  ---------------------------------------------------------------------
//  Es el "cerebro" del puzzle. Se encarga de:
//    1. Guardar las notas (sonido + objeto que se golpea).
//    2. Construir la secuencia que hay que memorizar.
//    3. Reproducir esa secuencia (sonido + destello visual) al ritmo del BPM.
//    4. Recibir los golpes del jugador y comprobar si son correctos.
//    5. Lanzar eventos (UnityEvents) cuando pasa algo: acierto, fallo, victoria...
// =====================================================================

// Modo de generación de la secuencia:
//  - Manual: tú escribes la secuencia exacta en el Inspector.
//  - Random: el script crea una secuencia aleatoria al empezar.
public enum SequenceMode { Manual, Random }

// ---------------------------------------------------------------------
// PuzzleNote: define UNA nota del puzzle (un sonido + el objeto que lo toca).
// [System.Serializable] hace que Unity la muestre dentro de la lista del Inspector.
// ---------------------------------------------------------------------
[System.Serializable]
public class PuzzleNote
{
    [Tooltip("Nombre solo para organizarte (ej: 'Do', 'Campana roja')")]
    public string name = "Nota";

    [Tooltip("Sonido que se reproduce cuando suena esta nota")]
    public AudioClip clip;

    [Tooltip("Objeto de la escena que el jugador golpea / clica para tocar esta nota")]
    public SoundPuzzleButton button;
}

// ---------------------------------------------------------------------
// SequenceStep: define UN paso de la secuencia (qué nota suena y cuánto dura).
// ---------------------------------------------------------------------
[System.Serializable]
public class SequenceStep
{
    [Tooltip("Posición de la nota en la lista 'Notes' (0 = primera, 1 = segunda...). Usa -1 para un silencio.")]
    public int noteIndex = 0;

    [Min(1), Tooltip("Cuánto dura este paso medido en ticks. Más ticks = espera más larga hasta el siguiente paso.")]
    public int durationTicks = 1;
}

[RequireComponent(typeof(AudioSource))] // Unity añade solo un AudioSource si no lo hay
public class SoundPuzzleController : MonoBehaviour
{
    // Estados posibles del puzzle:
    //  Idle         -> todavía no ha empezado (los golpes solo suenan, no cuentan)
    //  Playing      -> está reproduciendo la secuencia para que la memorices
    //  WaitingInput -> es tu turno: debes repetir la secuencia
    //  Completed    -> puzzle resuelto
    public enum State { Idle, Playing, WaitingInput, Completed }

    // ================== NOTAS ==================
    [Header("Notas (arrastra aquí clips y objetos)")]
    [Tooltip("Lista de todas las notas disponibles. El orden importa: la primera es la 0, la segunda la 1, etc.")]
    public List<PuzzleNote> notes = new List<PuzzleNote>();

    // ================== SECUENCIA ==================
    [Header("Secuencia")]
    [Tooltip("Manual: tú escribes la secuencia. Random: se genera aleatoriamente.")]
    public SequenceMode mode = SequenceMode.Manual;

    [Tooltip("Solo se usa en modo Manual. Cada elemento es un paso: qué nota y cuánto dura.")]
    public List<SequenceStep> manualSequence = new List<SequenceStep>();

    // ================== SECUENCIA ALEATORIA ==================
    [Header("Secuencia aleatoria (modo Random)")]
    [Min(1), Tooltip("Cuántos pasos tendrá la secuencia generada")]
    public int randomLength = 5;

    [Min(1), Tooltip("Duración mínima (en ticks) que puede tener un paso")]
    public int randomMinTicks = 1;

    [Min(1), Tooltip("Duración máxima (en ticks) que puede tener un paso")]
    public int randomMaxTicks = 2;

    [Range(0f, 1f), Tooltip("Probabilidad de que un paso sea un silencio (0 = nunca, 1 = siempre)")]
    public float randomRestChance = 0f;

    [Tooltip("Si está desactivado, la misma nota no suena dos veces seguidas")]
    public bool allowRepeatedNotes = true;

    [Tooltip("0 = una secuencia distinta cada vez. Otro número = siempre la misma secuencia (útil para probar)")]
    public int randomSeed = 0;

    // ================== TIEMPO / RITMO ==================
    [Header("Tiempo / ritmo")]
    [Min(1), Tooltip("Beats por minuto: la velocidad general. Más BPM = más rápido")]
    public float bpm = 100f;

    [Min(1), Tooltip("Cuántos ticks caben en un beat. Más ticks por beat = más precisión rítmica y pasos más cortos")]
    public int ticksPerBeat = 2;

    [Min(0), Tooltip("Ticks de espera antes de empezar a reproducir la secuencia")]
    public int startDelayTicks = 2;

    // Duración de UN tick en segundos. Es la unidad básica de tiempo del puzzle.
    // Fórmula: 60 segundos / (beats por minuto * ticks por beat)
    public float TickDuration => 60f / (bpm * ticksPerBeat);

    // ================== REGLAS ==================
    [Header("Reglas")]
    [Tooltip("Si está activado, la secuencia suena sola al empezar la escena")]
    public bool playOnStart = true;

    [Tooltip("Si está activado, los golpes del jugador se ignoran mientras suena la secuencia")]
    public bool lockInputDuringPlayback = true;

    [Min(0), Tooltip("Fallos permitidos antes de reiniciar el puzzle. 0 = fallos ilimitados")]
    public int maxMistakes = 0;

    [Tooltip("Tras un fallo, vuelve a reproducir la secuencia para ayudar al jugador")]
    public bool replayAfterMistake = true;

    [Min(0), Tooltip("Segundos máximos entre un golpe y el siguiente. Si se pasa, cuenta como fallo. 0 = sin límite")]
    public float inputTimeoutSeconds = 0f;

    // ================== MODO PROGRESIVO ==================
    [Header("Modo progresivo (tipo Simon)")]
    [Tooltip("Si está activado, la secuencia empieza corta y crece un paso en cada ronda acertada")]
    public bool progressive = false;

    [Min(1), Tooltip("Cuántos pasos tiene la secuencia en la primera ronda (solo modo progresivo)")]
    public int startLength = 1;

    // ================== AUDIO ==================
    [Header("Audio")]
    [Range(0f, 1f), Tooltip("Volumen de todas las notas")]
    public float volume = 1f;

    [Tooltip("Sonido que suena al completar el puzzle (opcional)")]
    public AudioClip completeClip;

    [Tooltip("Sonido que suena al fallar una nota (opcional)")]
    public AudioClip failClip;

    // ================== EVENTOS ==================
    // Los UnityEvent te permiten conectar acciones desde el Inspector sin programar
    // (por ejemplo: abrir una puerta, activar un objeto, reproducir una animación...).
    [Header("Eventos")]
    [Tooltip("Se lanza cuando empieza a sonar la secuencia")]
    public UnityEvent onSequenceStart;

    [Tooltip("Se lanza cuando termina de sonar la secuencia (empieza el turno del jugador)")]
    public UnityEvent onSequenceEnd;

    [Tooltip("Se lanza cada vez que el jugador acierta una nota")]
    public UnityEvent onCorrectNote;

    [Tooltip("Se lanza cada vez que el jugador falla una nota")]
    public UnityEvent onWrongNote;

    [Tooltip("Se lanza al acertar una ronda completa en modo progresivo")]
    public UnityEvent onRoundCompleted;

    [Tooltip("Se lanza cuando se superan los fallos máximos y el puzzle se reinicia")]
    public UnityEvent onFailed;

    [Tooltip("Se lanza cuando el puzzle se resuelve del todo. Aquí conectas la recompensa")]
    public UnityEvent onPuzzleCompleted;

    // ================== DEBUG ==================
    // Campos solo para ver en el Inspector cómo va el puzzle mientras juegas.
    // [SerializeField] los muestra aunque sean privados.
    [Header("Debug (solo lectura)")]
    [SerializeField] State state = State.Idle;   // Estado actual
    [SerializeField] int mistakes;               // Fallos acumulados
    [SerializeField] int inputIndex;             // Cuántas notas ha acertado ya el jugador en esta ronda
    [SerializeField] int currentLength;          // Cuántos pasos de la secuencia están activos ahora

    // ================== VARIABLES INTERNAS ==================
    AudioSource audioSource;                                  // Reproduce los sonidos
    List<SequenceStep> sequence = new List<SequenceStep>();   // Secuencia final (manual o aleatoria)
    List<int> expected = new List<int>();                     // Solo las notas que el jugador debe pulsar (sin silencios)
    Coroutine playRoutine;                                    // Referencia a la reproducción en curso, para poder pararla
    float lastInputTime;                                      // Momento del último golpe (para el timeout)

    // Permite a otros scripts leer el estado sin poder modificarlo
    public State CurrentState => state;

    // ---------------------------------------------------------------------
    // Awake: se ejecuta al cargar el objeto, antes que Start.
    // Prepara el AudioSource y "presenta" cada botón al controlador.
    // ---------------------------------------------------------------------
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false; // Evita que suene algo por sí solo

        // A cada botón le decimos quién es su controlador y qué número de nota es.
        for (int i = 0; i < notes.Count; i++)
            if (notes[i].button != null) notes[i].button.Setup(this, i);
    }

    // ---------------------------------------------------------------------
    // Start: se ejecuta justo antes del primer frame.
    // Construye la secuencia, deja el puzzle en estado inicial y (si toca) la reproduce.
    // ---------------------------------------------------------------------
    void Start()
    {
        BuildSequence();
        ResetPuzzle();
        if (playOnStart) PlaySequence();
    }

    // ---------------------------------------------------------------------
    // Update: se ejecuta cada frame.
    // Aquí solo vigila el timeout: si el jugador tarda demasiado entre golpes, cuenta como fallo.
    // ---------------------------------------------------------------------
    void Update()
    {
        if (state == State.WaitingInput && inputTimeoutSeconds > 0f && inputIndex > 0 &&
            Time.time - lastInputTime > inputTimeoutSeconds)
        {
            RegisterMistake();
        }
    }

    // =====================================================================
    //  API PÚBLICA (funciones que puedes llamar desde otros scripts o UnityEvents)
    // =====================================================================

    // Empieza el puzzle desde cero. Llámalo desde un trigger, un NPC, una palanca, etc.
    // [ContextMenu] lo añade al menú de clic derecho del componente para probarlo en el editor.
    [ContextMenu("Empezar puzzle")]
    public void StartPuzzle()
    {
        BuildSequence();  // Regenera la secuencia (útil en modo Random)
        ResetPuzzle();
        PlaySequence();
    }

    // Reproduce (o repite) la secuencia actual. Útil para un botón "escuchar otra vez".
    [ContextMenu("Reproducir secuencia")]
    public void PlaySequence()
    {
        if (state == State.Completed || sequence.Count == 0) return;
        if (playRoutine != null) StopCoroutine(playRoutine); // Evita dos reproducciones a la vez
        playRoutine = StartCoroutine(PlaySequenceRoutine());
    }

    // Devuelve el puzzle al estado inicial: sin fallos y sin progreso.
    [ContextMenu("Reiniciar puzzle")]
    public void ResetPuzzle()
    {
        if (playRoutine != null) StopCoroutine(playRoutine);
        mistakes = 0;
        inputIndex = 0;

        // En modo progresivo empieza con pocos pasos; si no, con la secuencia entera
        currentLength = progressive ? Mathf.Clamp(startLength, 1, sequence.Count) : sequence.Count;

        RebuildExpected();
        state = State.Idle;
    }

    // Los botones llaman a esta función cuando el jugador los golpea.
    // Aquí se decide si el golpe es correcto o no.
    public void PlayerHit(int noteIndex)
    {
        if (noteIndex < 0 || noteIndex >= notes.Count) return; // Índice inválido

        // Mientras suena la secuencia y el input está bloqueado, ignoramos el golpe
        if (state == State.Playing && lockInputDuringPlayback) return;

        // Si el puzzle no está en juego (Idle o Completed), la nota suena pero no cuenta
        if (state != State.WaitingInput && state != State.Playing)
        {
            PlayNote(noteIndex);
            return;
        }

        PlayNote(noteIndex);          // Siempre damos feedback sonoro y visual
        lastInputTime = Time.time;    // Reinicia el contador del timeout

        if (expected.Count == 0) return;

        // ¿Es la nota que tocaba en este punto de la secuencia?
        if (noteIndex == expected[inputIndex])
        {
            inputIndex++;                 // Avanzamos al siguiente paso esperado
            onCorrectNote.Invoke();

            // Si ya hemos acertado todas, la ronda termina
            if (inputIndex >= expected.Count) RoundFinished();
        }
        else
        {
            RegisterMistake();
        }
    }

    // =====================================================================
    //  LÓGICA INTERNA
    // =====================================================================

    // Se llama cuando el jugador repite correctamente toda la secuencia activa.
    void RoundFinished()
    {
        if (progressive && currentLength < sequence.Count)
        {
            // Modo progresivo: añadimos un paso más y volvemos a reproducir
            currentLength++;
            inputIndex = 0;
            RebuildExpected();
            onRoundCompleted.Invoke();
            PlaySequence();
        }
        else
        {
            // Puzzle resuelto
            state = State.Completed;
            if (completeClip != null) audioSource.PlayOneShot(completeClip, volume);
            onPuzzleCompleted.Invoke();
        }
    }

    // Se llama cuando el jugador falla (nota incorrecta o se pasa el tiempo).
    void RegisterMistake()
    {
        mistakes++;
        inputIndex = 0; // El jugador debe empezar la secuencia de nuevo
        if (failClip != null) audioSource.PlayOneShot(failClip, volume);
        onWrongNote.Invoke();

        // Si hay límite de fallos y se ha alcanzado, el puzzle se reinicia
        if (maxMistakes > 0 && mistakes >= maxMistakes)
        {
            onFailed.Invoke();
            ResetPuzzle();
            if (mode == SequenceMode.Random) BuildSequence(); // Nueva secuencia aleatoria
            PlaySequence();
            return;
        }

        if (replayAfterMistake) PlaySequence();
    }

    // Corrutina que reproduce la secuencia paso a paso.
    // Una corrutina permite "esperar" entre pasos sin bloquear el juego.
    IEnumerator PlaySequenceRoutine()
    {
        state = State.Playing;
        inputIndex = 0;
        onSequenceStart.Invoke();

        // Pausa inicial antes de empezar a sonar
        yield return new WaitForSeconds(startDelayTicks * TickDuration);

        for (int i = 0; i < currentLength; i++)
        {
            SequenceStep step = sequence[i];

            // Si el paso es una nota válida (no un silencio), la tocamos
            if (step.noteIndex >= 0 && step.noteIndex < notes.Count)
                PlayNote(step.noteIndex);

            // Esperamos tantos ticks como dure el paso antes de pasar al siguiente
            yield return new WaitForSeconds(Mathf.Max(1, step.durationTicks) * TickDuration);
        }

        // Fin de la secuencia: ahora es el turno del jugador
        state = State.WaitingInput;
        lastInputTime = Time.time;
        onSequenceEnd.Invoke();
        playRoutine = null;
    }

    // Reproduce el sonido de una nota y hace que su objeto parpadee.
    void PlayNote(int index)
    {
        PuzzleNote n = notes[index];
        if (n.clip != null) audioSource.PlayOneShot(n.clip, volume);
        if (n.button != null) n.button.Flash();
    }

    // Construye la lista "expected": las notas que el jugador debe pulsar (los silencios no cuentan).
    void RebuildExpected()
    {
        expected.Clear();
        for (int i = 0; i < currentLength && i < sequence.Count; i++)
            if (sequence[i].noteIndex >= 0) expected.Add(sequence[i].noteIndex);

        // En modo progresivo evita rondas con solo silencios (no habría nada que pulsar)
        while (expected.Count == 0 && currentLength < sequence.Count)
        {
            currentLength++;
            if (sequence[currentLength - 1].noteIndex >= 0)
                expected.Add(sequence[currentLength - 1].noteIndex);
        }
    }

    // Crea la secuencia final: copia la manual o genera una aleatoria.
    void BuildSequence()
    {
        sequence.Clear();

        // Modo manual (o sin notas): usamos tal cual lo que hayas escrito en el Inspector
        if (mode == SequenceMode.Manual || notes.Count == 0)
        {
            sequence.AddRange(manualSequence);
            return;
        }

        // Modo aleatorio. System.Random con semilla permite repetir la misma secuencia si quieres.
        System.Random rng = randomSeed == 0 ? new System.Random() : new System.Random(randomSeed);
        int last = -1; // Última nota usada (para evitar repetirla si no se permite)
        int safeMax = Mathf.Max(randomMinTicks, randomMaxTicks); // Evita que max sea menor que min

        for (int i = 0; i < randomLength; i++)
        {
            var step = new SequenceStep { durationTicks = rng.Next(randomMinTicks, safeMax + 1) };

            if (rng.NextDouble() < randomRestChance && i > 0)
            {
                step.noteIndex = -1; // Silencio (nunca en el primer paso)
            }
            else
            {
                int idx;
                int tries = 0;
                // Elegimos una nota; si no se permiten repeticiones, reintentamos hasta que sea distinta
                do { idx = rng.Next(0, notes.Count); tries++; }
                while (!allowRepeatedNotes && idx == last && notes.Count > 1 && tries < 20);
                step.noteIndex = idx;
                last = idx;
            }
            sequence.Add(step);
        }
    }
}