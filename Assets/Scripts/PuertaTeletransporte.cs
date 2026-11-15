using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class PuertaTeletransporte : MonoBehaviour
{
    [Header("Destino")]
    [SerializeField] Transform puntoLlegadaJugador;
    [SerializeField] Transform centroSalaDestino;
    [SerializeField] PuertaTeletransporte puertaDestino;

    [Header("Interacción (Pasillo Hub)")]
    [Tooltip("Marcado = pide pulsar E frente a la puerta. Desmarcado = teletransporta apenas la tocas.")]
    [SerializeField] bool requiereTecla = true;
    [SerializeField] Key teclaInteraccion = Key.E;
    [Tooltip("El letrerito '[E]' encima de esta puerta.")]
    [SerializeField] GameObject indicadorPulsaE;

    [Header("Reglas")]
    [SerializeField, Min(0f)] float enfriamiento = 0.2f;
    [SerializeField] bool requiereEnergia = false;

    [Header("Transición (Efecto de teletransportación)")]
    [SerializeField] CanvasGroup fundidoNegro;
    [SerializeField, Min(0f)] float duracionFundido = 0.2f;

    [Header("Eventos (sonido, partículas...)")]
    [SerializeField] UnityEvent alTeletransportar;
    [SerializeField] UnityEvent alEstarBloqueada;

    static float ultimoTeletransporte = -999f;
    static bool enTransicion;

    Collider2D jugadorEnZona;
    Collider2D ignorarHastaSalir;
    Camera camara;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Awake()
    {
        camara = Camera.main;
        enTransicion = false;
        ultimoTeletransporte = -999f;
        if (indicadorPulsaE != null) indicadorPulsaE.SetActive(false);
    }

    void Update()
    {
        if (!requiereTecla || jugadorEnZona == null) return;
        if (enTransicion || Time.time - ultimoTeletransporte < enfriamiento) return;

        if (Keyboard.current != null && Keyboard.current[teclaInteraccion].wasPressedThisFrame)
        {
            IntentarTeletransportar(jugadorEnZona);
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!EsJugador(otro)) return;

        jugadorEnZona = otro;

        if (requiereTecla)
        {
            if (indicadorPulsaE != null) indicadorPulsaE.SetActive(true);
            return;
        }

        if (otro == ignorarHastaSalir) return;
        IntentarTeletransportar(otro);
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (!EsJugador(otro)) return;

        if (otro == jugadorEnZona) jugadorEnZona = null;
        if (otro == ignorarHastaSalir) ignorarHastaSalir = null;
        if (indicadorPulsaE != null) indicadorPulsaE.SetActive(false);
    }

    void IntentarTeletransportar(Collider2D otro)
    {
        if (enTransicion || Time.time - ultimoTeletransporte < enfriamiento) return;
        if (GameManager.Instancia != null && !GameManager.Instancia.JuegoActivo) return;

        if (requiereEnergia && GameManager.Instancia != null && GameManager.Instancia.Energia <= 0f)
        {
            alEstarBloqueada?.Invoke();
            return;
        }

        if (puntoLlegadaJugador == null)
        {
            Debug.LogWarning($"La puerta '{name}' no tiene punto de llegada asignado.", this);
            return;
        }

        if (indicadorPulsaE != null) indicadorPulsaE.SetActive(false);
        StartCoroutine(Teletransportar(otro));
    }

    static bool EsJugador(Collider2D otro)
    {
        return otro.CompareTag("Player") ||
               (otro.attachedRigidbody != null && otro.attachedRigidbody.CompareTag("Player"));
    }

    IEnumerator Teletransportar(Collider2D otro)
    {
        enTransicion = true;
        ultimoTeletransporte = Time.time;

        var jugador = otro.attachedRigidbody != null
            ? otro.attachedRigidbody.GetComponent<PlayerController>()
            : otro.GetComponent<PlayerController>();

        if (jugador != null)
        {
            jugador.Detener();
            jugador.enabled = false;
        }

        if (fundidoNegro != null) yield return Fundir(0f, 1f);

        MoverJugador(otro);
        MoverCamara();

        if (puertaDestino != null) puertaDestino.ignorarHastaSalir = otro;
        alTeletransportar?.Invoke();

        if (fundidoNegro != null) yield return Fundir(1f, 0f);

        if (jugador != null) jugador.enabled = true;
        ultimoTeletransporte = Time.time;
        enTransicion = false;
    }

    void MoverJugador(Collider2D otro)
    {
        Vector2 destino = puntoLlegadaJugador.position;
        Rigidbody2D rb = otro.attachedRigidbody;

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.position = destino;
            rb.transform.position = new Vector3(destino.x, destino.y, rb.transform.position.z);
        }
        else
        {
            otro.transform.position = new Vector3(destino.x, destino.y, otro.transform.position.z);
        }

        Physics2D.SyncTransforms();
    }

    void MoverCamara()
    {
        if (centroSalaDestino == null) return;
        if (camara == null) camara = Camera.main;
        if (camara == null) return;

        Vector3 pos = centroSalaDestino.position;
        pos.z = camara.transform.position.z;
        camara.transform.position = pos;
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        fundidoNegro.blocksRaycasts = true;

        for (float t = 0f; t < duracionFundido; t += Time.unscaledDeltaTime)
        {
            fundidoNegro.alpha = Mathf.Lerp(desde, hasta, t / duracionFundido);
            yield return null;
        }

        fundidoNegro.alpha = hasta;
        fundidoNegro.blocksRaycasts = hasta > 0f;
    }

    void OnDisable()
    {
        if (enTransicion)
        {
            enTransicion = false;
            if (fundidoNegro != null)
            {
                fundidoNegro.alpha = 0f;
                fundidoNegro.blocksRaycasts = false;
            }
        }
    }

    void OnDrawGizmos()
    {
        if (puntoLlegadaJugador == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, puntoLlegadaJugador.position);
        Gizmos.DrawWireSphere(puntoLlegadaJugador.position, 0.25f);

        if (centroSalaDestino != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(centroSalaDestino.position, new Vector3(1f, 1f, 0f));
        }
    }
}