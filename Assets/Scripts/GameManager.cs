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

    [Header("Ritmo del juego")]
    [Tooltip("Qué tan rápido suben y bajan las barras. 1 = normal, 0.5 = la mitad de rápido.")]
    [SerializeField, Range(0.1f, 2f)] float velocidadBarras = 0.45f;
    [Tooltip("Cada cuántos segundos ocurre un evento (tormenta, avería, etc.).")]
    [SerializeField, Min(3f)] float segundosEntreEventos = 25f;
    [Tooltip("Segundos de calma antes del primer evento.")]
    [SerializeField, Min(0f)] float primerEventoSegundos = 20f;

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

    [Header("Eventos de la base (averías, fugas, sorpresas)")]
    [Tooltip("Probabilidad de que ocurra un evento de la base al iniciar cada Sol.")]
    [SerializeField, Range(0, 1)] float probEventoBase = 0.45f;
    [SerializeField, Range(0, 50)] float perdidaPorFuga = 15f;
    [SerializeField, Range(0, 50)] float perdidaPorMoho = 20f;
    [SerializeField, Range(0, 50)] float energiaPorRemolino = 20f;
    [SerializeField, Min(1f)] float duracionNocheHelada = 15f;
    [SerializeField, Min(0f)] float consumoNocheHelada = 1f;

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
    public bool HayNocheHelada => tiempoNocheHelada > 0f;

    /// <summary>Mientras sea true, el reloj, las barras y los eventos no avanzan (se usa en el tutorial).</summary>
    public bool TiempoDetenido { get; set; }
    public bool SoporteAveriado { get; private set; }

    // Para explicar en pantalla por qué sube o baja cada barra
    readonly float[] tendencia = new float[4];
    readonly string[] explicacion = { "", "", "", "" };
    /// <summary>Cambio por segundo de la barra de esa sala (positivo = sube).</summary>
    public float Tendencia(Sala sala) => tendencia[(int)sala];
    /// <summary>Frase corta que explica por qué la barra de esa sala sube o baja.</summary>
    public string Explicacion(Sala sala) => explicacion[(int)sala];

    // Para la guía educativa: cada evento de la base sube el contador y deja un dato real ("texto|fuente")
    public int ContadorEventos { get; private set; }
    public string UltimoDatoEvento { get; private set; }

    public bool EstaActiva(Sala sala) => salaActiva[(int)sala];

    // ---------- Estado interno ----------
    readonly bool[] salaActiva = new bool[4];
    readonly bool[] alertaCriticaMostrada = new bool[4];
    float temporizadorSol;
    float tiempoTormentaPolvo;
    float tiempoTormentaSolar;
    float tiempoNocheHelada;
    float temporizadorEvento;
    int ultimoEventoBase = -1;

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
        tiempoNocheHelada = 0f;
        if (minijuegoPaneles != null)
    minijuegoPaneles.ReiniciarMinijuego();
        temporizadorEvento = primerEventoSegundos;
        ultimoEventoBase = -1;
        ContadorEventos = 0;
        UltimoDatoEvento = "";
        SoporteAveriado = false;

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

        if (TiempoDetenido)
        {
            ActualizarRecursos(0f); // solo refresca las explicaciones de las barras
            return;
        }

        AvanzarReloj(dt);
        if (!JuegoActivo) return;

        AvanzarEventos(dt);

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
        }
    }

    // ---------- Eventos aleatorios (por tiempo, no por Sol) ----------
    void AvanzarEventos(float dt)
    {
        if (!eventosActivos) return;

        temporizadorEvento -= dt;
        if (temporizadorEvento > 0f) return;

        temporizadorEvento = segundosEntreEventos * UnityEngine.Random.Range(0.8f, 1.2f);
        SortearEventos();
    }

    // Elige UN evento: tormenta de polvo, tormenta solar o evento de la base.
    // Las tres probabilidades del Inspector funcionan como "pesos": la más alta sale más seguido.
    void SortearEventos()
    {
        float pesoPolvo = HayTormentaPolvo ? 0f : probTormentaPolvo;
        float pesoSolar = HayTormentaSolar ? 0f : probTormentaSolar;
        float pesoBase = probEventoBase;
        float total = pesoPolvo + pesoSolar + pesoBase;
        if (total <= 0f) return;

        float tiro = UnityEngine.Random.value * total;

        if (tiro < pesoPolvo)
        {
            tiempoTormentaPolvo = duracionTormenta;
            EmitirAlerta("Tormenta de polvo: los paneles solares producen menos energía.");
        }
        else if (tiro < pesoPolvo + pesoSolar)
        {
            tiempoTormentaSolar = duracionTormenta;
            EmitirAlerta("¡Tormenta solar! La radiación sube más rápido. Activa los escudos.");
        }
        else
        {
            LanzarEventoBase();
        }
    }

    // Elige un evento de la base que tenga sentido ahora y que no repita el anterior
    void LanzarEventoBase()
    {
        const int total = 6;
        int inicio = UnityEngine.Random.Range(0, total);

        for (int paso = 0; paso < total; paso++)
        {
            int tipo = (inicio + paso) % total;
            if (tipo == ultimoEventoBase) continue;
            if (IntentarEventoBase(tipo))
            {
                ultimoEventoBase = tipo;
                ContadorEventos++;
                return;
            }
        }
    }

    bool IntentarEventoBase(int tipo)
    {
        switch (tipo)
        {
            case 0: // Avería del generador
                if (!EstaActiva(Sala.Energia)) return false;
                FijarSala(Sala.Energia, false);
                EmitirAlerta("¡Avería! El generador se apagó. Ve a la sala de Energía y enciéndelo con E.");
                UltimoDatoEvento = "Las máquinas también fallan en Marte: al rover Spirit se le descompuso una rueda en 2006 y siguió trabajando arrastrándola.|NASA - Rover Spirit";
                return true;

            case 1: // Fuga de aire
                SoporteVital = Limitar(SoporteVital - perdidaPorFuga);
                SoporteAveriado = true;
                FijarSala(Sala.Soporte, false);
                EmitirAlerta("¡Fuga de aire! Se perdió oxígeno y el soporte vital se detuvo. Ve a encenderlo con E.");
                UltimoDatoEvento = "En 2020, los astronautas de la Estación Espacial Internacional buscaron una pequeña fuga de aire durante semanas. La encontraron viendo hacia dónde flotaban unas hojas de té.|NASA y Roscosmos - Estación Espacial, 2020";
                return true;

            case 2: // Moho en el invernadero
                Comida = Limitar(Comida - perdidaPorMoho);
                EmitirAlerta("¡Moho en el invernadero! Se perdió parte de la cosecha.");
                UltimoDatoEvento = "En 2015, unas flores llamadas zinnias que crecían en la estación espacial se llenaron de moho. El astronauta Scott Kelly logró salvarlas.|NASA - Veggie, 2015";
                return true;

            case 3: // Impacto de meteorito
                if (!EstaActiva(Sala.Radiacion)) return false;
                FijarSala(Sala.Radiacion, false);
                EmitirAlerta("¡Impacto de un meteorito! Los escudos se apagaron. Ve a la sala de Radiación a encenderlos.");
                UltimoDatoEvento = "El módulo InSight de la NASA detectó en Marte los temblores que causan los meteoritos al chocar contra el suelo.|NASA - Misión InSight";
                return true;

            case 4: // Noche helada
                if (HayNocheHelada) return false;
                tiempoNocheHelada = duracionNocheHelada;
                EmitirAlerta("Noche helada: los calentadores gastan más energía por un rato.");
                UltimoDatoEvento = "Las noches en Marte son heladas: pueden bajar a más de 70 grados bajo cero. Los robots usan calentadores para proteger sus baterías.|NASA - Rover Curiosity";
                return true;

            default: // Remolino de polvo (evento bueno)
                Energia = Limitar(Energia + energiaPorRemolino);
                EmitirAlerta("¡Un remolino de polvo limpió los paneles solares! Ganaste energía extra.");
                UltimoDatoEvento = "Los remolinos de polvo de Marte a veces limpian los paneles solares. Así recuperó energía el rover Spirit en 2005.|NASA - Rover Spirit";
                return true;
        }
    }

    void ActualizarTormentas(float dt)
    {
        tiempoTormentaPolvo = Mathf.Max(0f, tiempoTormentaPolvo - dt);
        tiempoTormentaSolar = Mathf.Max(0f, tiempoTormentaSolar - dt);
        tiempoNocheHelada = Mathf.Max(0f, tiempoNocheHelada - dt);
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
        float multRadiacion = HayTormentaSolar ? multiplicadorRadiacionSolar : 1f;
        float paso = dt * velocidadBarras;

        // Energía
        float cambioEnergia = generador ? tasas.produccionGenerador * multPaneles : -tasas.perdidaSinGenerador;
        if (soporte) cambioEnergia -= tasas.consumoSoporte;
        if (invernadero) cambioEnergia -= tasas.consumoInvernadero;
        if (escudos) cambioEnergia -= tasas.consumoEscudos;
        if (HayNocheHelada) cambioEnergia -= consumoNocheHelada;
        Energia = Limitar(Energia + cambioEnergia * paso);

        // Sin energía, ningún sistema produce
        bool hayLuz = Energia > 0f;

        float cambioSoporte = (soporte && hayLuz) ? tasas.soporteSube : -tasas.soporteBaja;
        float cambioComida = (invernadero && hayLuz) ? tasas.comidaSube : -tasas.comidaBaja;
float cambioRadiacion = (escudos && hayLuz)
    ? (HayTormentaSolar ? 1.5f : 0.15f)
    : tasas.radiacionSube * multRadiacion;

        SoporteVital = Limitar(SoporteVital + cambioSoporte * paso);
        Comida = Limitar(Comida + cambioComida * paso);
        Radiacion = Limitar(Radiacion + cambioRadiacion * paso);

        // ---- Explicaciones para la pantalla ----
        tendencia[(int)Sala.Energia] = cambioEnergia * velocidadBarras;
        tendencia[(int)Sala.Soporte] = cambioSoporte * velocidadBarras;
        tendencia[(int)Sala.Alimentos] = cambioComida * velocidadBarras;
        tendencia[(int)Sala.Radiacion] = cambioRadiacion * velocidadBarras;

        if (!generador) explicacion[(int)Sala.Energia] = "el generador está apagado";
        else if (cambioEnergia >= 0f) explicacion[(int)Sala.Energia] = "el generador produce más de lo que se gasta";
        else if (HayTormentaPolvo) explicacion[(int)Sala.Energia] = "la tormenta de polvo tapa los paneles";
        else if (HayNocheHelada) explicacion[(int)Sala.Energia] = "los calentadores gastan de más";
        else explicacion[(int)Sala.Energia] = "hay muchos sistemas encendidos a la vez";

        if (!soporte) explicacion[(int)Sala.Soporte] = "el soporte vital está apagado";
        else if (!hayLuz) explicacion[(int)Sala.Soporte] = "no hay energía para el soporte vital";
        else explicacion[(int)Sala.Soporte] = "el soporte vital fabrica aire";

        if (!invernadero) explicacion[(int)Sala.Alimentos] = "el invernadero está apagado";
        else if (!hayLuz) explicacion[(int)Sala.Alimentos] = "no hay energía para las plantas";
        else explicacion[(int)Sala.Alimentos] = "el invernadero produce comida";

        if (!escudos) explicacion[(int)Sala.Radiacion] = HayTormentaSolar ? "escudos apagados y hay tormenta solar" : "los escudos están apagados";
        else if (!hayLuz) explicacion[(int)Sala.Radiacion] = "no hay energía para los escudos";
        else explicacion[(int)Sala.Radiacion] = "los escudos protegen la base";
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
    public void CompletarReparacionSoporte()
{
    if (!JuegoActivo || !SoporteAveriado)
        return;

    SoporteAveriado = false;
    FijarSala(Sala.Soporte, true);
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