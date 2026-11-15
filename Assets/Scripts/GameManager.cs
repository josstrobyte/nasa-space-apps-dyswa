using System;
using UnityEngine;

public enum Sala { Energia = 0, Soporte = 1, Alimentos = 2, Radiacion = 3 }

[Serializable]
public class TasasRecursos
{
    [Header("Energía (unidades por segundo)")]
    public float produccionGenerador = 2.5f;
    public float perdidaSinGenerador = 1.5f;
    public float consumoSoporte = 0.8f;
    public float consumoInvernadero = 0.6f;
    public float consumoEscudos = 1.2f;

    [Header("Soporte vital")]
    public float soporteSube = 1.5f;
    public float soporteBaja = 2.0f;

    [Header("Comida")]
    public float comidaSube = 1.2f;
    public float comidaBaja = 1.5f;

    [Header("Radiación")]
    public float radiacionBaja = 2.0f;
    public float radiacionSube = 1.5f;
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instancia { get; private set; }

    // ---------- Eventos: la UI se suscribe en vez de revisar cada frame ----------
    public event Action<int, int> SolCambiado;          // (solActual, totalSoles)
    public event Action<Sala, bool> SalaCambiada;       // (sala, activa)
    public event Action<string> AlertaEmitida;          // mensaje
    public event Action<bool, string> PartidaTerminada; // (gano, razón)

    [Header("Tiempo (12 Soles = 12 Minutos)")]
    [SerializeField, Min(1)] int totalSoles = 12;
    [SerializeField, Min(1f)] float duracionSolSegundos = 60f;

    [Header("Valores iniciales (0 a 100)")]
    [SerializeField, Range(0, 100)] float energiaInicial = 80f;
    [SerializeField, Range(0, 100)] float soporteInicial = 80f;
    [SerializeField, Range(0, 100)] float comidaInicial = 80f;
    [SerializeField, Range(0, 100)] float radiacionInicial = 10f;

    [Header("Salas encendidas al inicio")]
    [SerializeField] bool generadorInicial = true;
    [SerializeField] bool soporteActivoInicial = true;
    [SerializeField] bool invernaderoInicial = true;
    [SerializeField] bool escudosInicial = false;

    [Header("Balance")]
    [SerializeField] TasasRecursos tasas = new TasasRecursos();
    [SerializeField, Range(0, 50)] float umbralCritico = 20f;
    [SerializeField, Range(0, 20)] float histeresisAlerta = 5f;

    [Header("Eventos marcianos (se sortean al iniciar cada Sol)")]
    [SerializeField] bool eventosActivos = true;
    [SerializeField, Range(0, 1)] float probTormentaPolvo = 0.25f;
    [SerializeField, Range(0, 1)] float probTormentaSolar = 0.20f;
    [SerializeField, Min(1f)] float duracionTormenta = 20f;
    [SerializeField, Range(0, 1)] float eficienciaPanelesConPolvo = 0.5f;
    [Header("Minijuego de paneles")]
[SerializeField] MinijuegoPaneles minijuegoPaneles;
    [SerializeField, Min(1f)] float multiplicadorRadiacionSolar = 3f;

    // ---------- Estado público de solo lectura ----------
    public int SolActual { get; private set; }
    public int TotalSoles => totalSoles;
    public float ProgresoSol => temporizadorSol / duracionSolSegundos; // 0..1, útil para una barra
    public float Energia { get; private set; }
    public float SoporteVital { get; private set; }
    public float Comida { get; private set; }
    public float Radiacion { get; private set; }
    public bool JuegoActivo { get; private set; }
    public string UltimaAlerta { get; private set; }
    public bool HayTormentaPolvo => tiempoTormentaPolvo > 0f;
    public bool HayTormentaSolar => tiempoTormentaSolar > 0f;

    public bool EstaActiva(Sala sala) => salaActiva[(int)sala];

    // ---------- Estado interno ----------
    readonly bool[] salaActiva = new bool[4];
    readonly bool[] alertaCriticaMostrada = new bool[4];
    float temporizadorSol;
    float tiempoTormentaPolvo;
    float tiempoTormentaSolar;

    void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        Reiniciar();
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    /// <summary>Deja la partida como al inicio. Útil para un botón "Reintentar".</summary>
    public void Reiniciar()
    {
        SolActual = 1;
        temporizadorSol = 0f;
        tiempoTormentaPolvo = 0f;
        tiempoTormentaSolar = 0f;
        if (minijuegoPaneles != null)
    minijuegoPaneles.ReiniciarMinijuego();

        Energia = energiaInicial;
        SoporteVital = soporteInicial;
        Comida = comidaInicial;
        Radiacion = radiacionInicial;

        salaActiva[(int)Sala.Energia] = generadorInicial;
        salaActiva[(int)Sala.Soporte] = soporteActivoInicial;
        salaActiva[(int)Sala.Alimentos] = invernaderoInicial;
        salaActiva[(int)Sala.Radiacion] = escudosInicial;

        Array.Clear(alertaCriticaMostrada, 0, alertaCriticaMostrada.Length);

        JuegoActivo = true;
        EmitirAlerta("Bienvenido a la base marciana. Mantén los recursos estables.");
        SolCambiado?.Invoke(SolActual, totalSoles);
    }

    void Update()
    {
        if (!JuegoActivo) return;

        float dt = Time.deltaTime; // respeta Time.timeScale, así que pausar = timeScale 0

        AvanzarReloj(dt);
        if (!JuegoActivo) return;

        ActualizarTormentas(dt);
        ActualizarRecursos(dt);
        RevisarAlertas();
        RevisarDerrota();
    }

    // ---------- Reloj ----------
    void AvanzarReloj(float dt)
    {
        temporizadorSol += dt;

        // "-=" en vez de "= 0" para no perder el sobrante de tiempo de ese frame
        while (temporizadorSol >= duracionSolSegundos)
        {
            temporizadorSol -= duracionSolSegundos;

            if (SolActual >= totalSoles)
            {
                TerminarPartida(true, "¡Misión cumplida! Sobrevivieron los " + totalSoles + " Soles en Marte.");
                return;
            }

            SolActual++;
            SolCambiado?.Invoke(SolActual, totalSoles);
            EmitirAlerta("¡Nuevo Sol alcanzado! Sol " + SolActual + " de " + totalSoles);
            SortearEventos();
        }
    }

    // ---------- Eventos aleatorios ----------
    void SortearEventos()
    {
        if (!eventosActivos) return;

        if (UnityEngine.Random.value < probTormentaPolvo)
        {
            tiempoTormentaPolvo = duracionTormenta;
            EmitirAlerta("Tormenta de polvo: los paneles solares producen menos energía.");
        }
        if (UnityEngine.Random.value < probTormentaSolar)
        {
            tiempoTormentaSolar = duracionTormenta;
            EmitirAlerta("¡Tormenta solar! La radiación sube más rápido. Activa los escudos.");
        }
    }

    void ActualizarTormentas(float dt)
    {
        tiempoTormentaPolvo = Mathf.Max(0f, tiempoTormentaPolvo - dt);
        tiempoTormentaSolar = Mathf.Max(0f, tiempoTormentaSolar - dt);
    }

    // ---------- Simulación ----------
    void ActualizarRecursos(float dt)
    {
        bool generador = EstaActiva(Sala.Energia);
        bool soporte = EstaActiva(Sala.Soporte);
        bool invernadero = EstaActiva(Sala.Alimentos);
        bool escudos = EstaActiva(Sala.Radiacion);

        float multPaneles = minijuegoPaneles != null
    ? minijuegoPaneles.EficienciaPaneles : 1f;

if (HayTormentaPolvo)
    multPaneles *= eficienciaPanelesConPolvo;
        float multRadiacion = HayTormentaSolar ? multiplicadorRadiacionSolar : 1f;

        // Energía
        float cambioEnergia = generador ? tasas.produccionGenerador * multPaneles : -tasas.perdidaSinGenerador;
        if (soporte) cambioEnergia -= tasas.consumoSoporte;
        if (invernadero) cambioEnergia -= tasas.consumoInvernadero;
        if (escudos) cambioEnergia -= tasas.consumoEscudos;
        Energia = Limitar(Energia + cambioEnergia * dt);

        // Sin energía, ningún sistema produce
        bool hayLuz = Energia > 0f;

        SoporteVital = Limitar(SoporteVital + ((soporte && hayLuz) ? tasas.soporteSube : -tasas.soporteBaja) * dt);
        Comida = Limitar(Comida + ((invernadero && hayLuz) ? tasas.comidaSube : -tasas.comidaBaja) * dt);
        Radiacion = Limitar(Radiacion + ((escudos && hayLuz) ? -tasas.radiacionBaja : tasas.radiacionSube * multRadiacion) * dt);
    }

    static float Limitar(float valor) => Mathf.Clamp(valor, 0f, 100f);

    // ---------- Alertas (sin spam: una vez al entrar en zona crítica) ----------
    void RevisarAlertas()
    {
        RevisarUmbral(0, Energia < umbralCritico, Energia > umbralCritico + histeresisAlerta,
            "⚠ Energía baja. Apaga sistemas o enciende el generador.");
        RevisarUmbral(1, SoporteVital < umbralCritico, SoporteVital > umbralCritico + histeresisAlerta,
            "⚠ Soporte vital crítico.");
        RevisarUmbral(2, Comida < umbralCritico, Comida > umbralCritico + histeresisAlerta,
            "⚠ Reservas de comida bajas.");
        RevisarUmbral(3, Radiacion > 100f - umbralCritico, Radiacion < 100f - umbralCritico - histeresisAlerta,
            "⚠ Radiación peligrosa. Activa los escudos.");
    }

    void RevisarUmbral(int indice, bool critico, bool recuperado, string mensaje)
    {
        if (critico && !alertaCriticaMostrada[indice])
        {
            alertaCriticaMostrada[indice] = true;
            EmitirAlerta(mensaje);
        }
        else if (recuperado)
        {
            alertaCriticaMostrada[indice] = false;
        }
    }

    void RevisarDerrota()
    {
        if (SoporteVital <= 0f) TerminarPartida(false, "¡Se agotó el soporte vital en la base!");
        else if (Comida <= 0f) TerminarPartida(false, "¡La tripulación se quedó sin alimentos!");
        else if (Radiacion >= 100f) TerminarPartida(false, "¡Nivel crítico de radiación solar!");
    }

    // ---------- API para la UI ----------
    public void AlternarSala(Sala sala) => FijarSala(sala, !EstaActiva(sala));

    public void FijarSala(Sala sala, bool activa)
    {
        if (!JuegoActivo || salaActiva[(int)sala] == activa) return;
        salaActiva[(int)sala] = activa;
        SalaCambiada?.Invoke(sala, activa);
    }

    // Los botones de Unity (OnClick) no aceptan enums en el Inspector, así que se usa un int:
    // 0 = Energía, 1 = Soporte, 2 = Alimentos, 3 = Radiación
    public void AlternarSalaPorIndice(int indice)
    {
        if (indice < 0 || indice >= salaActiva.Length)
        {
            Debug.LogWarning("Índice de sala inválido: " + indice);
            return;
        }
        AlternarSala((Sala)indice);
    }

    // ---------- Utilidades ----------
    void EmitirAlerta(string mensaje)
    {
        UltimaAlerta = mensaje;
        AlertaEmitida?.Invoke(mensaje);
    }

    void TerminarPartida(bool gano, string razon)
    {
        if (!JuegoActivo) return;
        JuegoActivo = false;
        EmitirAlerta(razon);
        PartidaTerminada?.Invoke(gano, razon);
    }
}