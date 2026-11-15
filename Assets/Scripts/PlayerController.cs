using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField, Min(0f)] float velocidad = 5f;
    [Tooltip("Qué tan rápido alcanza la velocidad máxima (unidades/s²). Muy alto = movimiento instantáneo.")]
    [SerializeField, Min(0f)] float aceleracion = 60f;
    [Tooltip("Qué tan rápido se detiene al soltar las teclas.")]
    [SerializeField, Min(0f)] float desaceleracion = 80f;

    [Header("Input")]
    [Tooltip("Opcional: acción 'Move' de tu Input Actions asset. Si se deja vacío, se crea una con WASD, flechas y gamepad.")]
    [SerializeField] InputActionReference accionMoverAsset;

    [Header("Visual (opcional)")]
    [SerializeField] Animator animator;
    [SerializeField] SpriteRenderer sprite;
    [Tooltip("Voltea el sprite al moverse a la izquierda (si tu arte mira a la derecha).")]
    [SerializeField] bool voltearSpriteEnX = true;

    // Parámetros del Animator (créalos con estos nombres si usas animaciones)
    static readonly int ParamVelocidad = Animator.StringToHash("Velocidad");
    static readonly int ParamDirX = Animator.StringToHash("DirX");
    static readonly int ParamDirY = Animator.StringToHash("DirY");

    Rigidbody2D rb;
    InputAction accionMover;
    bool accionCreadaEnCodigo;

    Vector2 entrada;
    Vector2 ultimaDireccion = Vector2.down; // hacia dónde mira en reposo

    public Vector2 UltimaDireccion => ultimaDireccion;
    public bool PuedeMoverse => GameManager.Instancia == null || GameManager.Instancia.JuegoActivo;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;          // evita tirones con la cámara
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous; // no atraviesa paredes delgadas

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (sprite == null) sprite = GetComponentInChildren<SpriteRenderer>();

        if (accionMoverAsset != null)
        {
            accionMover = accionMoverAsset.action;
        }
        else
        {
            accionMover = CrearAccionPorDefecto();
            accionCreadaEnCodigo = true;
        }
    }

    static InputAction CrearAccionPorDefecto()
    {
        var accion = new InputAction("Mover", InputActionType.Value, expectedControlType: "Vector2");

        accion.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");

        accion.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");

        accion.AddBinding("<Gamepad>/leftStick");
        accion.AddBinding("<Gamepad>/dpad");

        return accion;
    }

    void OnEnable() => accionMover?.Enable();

    void OnDisable()
    {
        accionMover?.Disable();
        entrada = Vector2.zero;
    }

    void OnDestroy()
    {
        if (accionCreadaEnCodigo) accionMover?.Dispose();
    }

    void Update()
    {
        if (!PuedeMoverse)
        {
            entrada = Vector2.zero;
        }
        else
        {
            // ClampMagnitude en vez de normalized: la diagonal no es más rápida
            // y el stick analógico conserva la intensidad (caminar lento si lo inclinas poco)
            entrada = Vector2.ClampMagnitude(accionMover.ReadValue<Vector2>(), 1f);
        }

        if (entrada.sqrMagnitude > 0.01f) ultimaDireccion = entrada.normalized;

        ActualizarVisual();
    }

    void FixedUpdate()
    {
        Vector2 objetivo = entrada * velocidad;
        float tasa = entrada.sqrMagnitude > 0.01f ? aceleracion : desaceleracion;

        rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, objetivo, tasa * Time.fixedDeltaTime);
    }

    void ActualizarVisual()
    {
        if (animator != null)
        {
            animator.SetFloat(ParamVelocidad, entrada.magnitude);
            animator.SetFloat(ParamDirX, ultimaDireccion.x);
            animator.SetFloat(ParamDirY, ultimaDireccion.y);
        }

        if (voltearSpriteEnX && sprite != null && Mathf.Abs(entrada.x) > 0.01f)
        {
            sprite.flipX = entrada.x < 0f;
        }
    }

    /// <summary>Detiene al jugador en seco (por ejemplo, al abrir un panel o una cinemática).</summary>
    public void Detener()
    {
        entrada = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
    }
}
