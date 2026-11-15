using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Networking;

// Ponlo en el objeto GameManager.
// - Pantalla de inicio con la misión y los controles.
// - Tarjetas "¿Sabías que...?" al entrar a cada sala, al empezar un Sol
//   y cuando llega una tormenta.
public class GuiaEducativa : MonoBehaviour
{
    [Header("Pantalla de inicio")]
    [SerializeField] bool mostrarInicio = true;
    [Tooltip("Opcional: hoja de la portada (START.png). Si se pone, primero sale la portada sola y luego las instrucciones.")]
    [SerializeField] Texture2D fondoInicio;
    [Tooltip("Cuántos cuadros tiene la hoja de la portada, de izquierda a derecha.")]
    [SerializeField, Min(1)] int cuadrosFondo = 6;
    [SerializeField, Min(0.1f)] float cuadrosFondoPorSegundo = 4f;

    [Header("Clima espacial real (NASA DONKI)")]
    [Tooltip("Al iniciar, pide a la NASA las llamaradas solares recientes. Si no hay internet, usa un evento real guardado.")]
    [SerializeField] bool usarDonkiEnVivo = true;
    [Tooltip("Clave gratuita de api.nasa.gov. DEMO_KEY funciona, pero con pocos usos por hora.")]
    [SerializeField] string claveApiNasa = "DEMO_KEY";
    [SerializeField, Min(1)] int diasHaciaAtras = 30;

    [Header("Tutorial")]
    [Tooltip("Al empezar, guía al jugador paso a paso. Mientras dura, las barras no bajan.")]
    [SerializeField] bool tutorialActivo = true;

    [Header("Nombres de las puertas del pasillo")]
    [SerializeField] bool mostrarNombresPuertas = true;
    [Tooltip("Altura del letrero sobre el centro del pasillo (en unidades). Súbelo o bájalo para que tape el letrero pintado.")]
    [SerializeField] float alturaNombres = 0.78f;
    [Tooltip("Debe ser igual al Tamano Barras del componente Hud Supervivencia, para que las tarjetas no se encimen con las barras.")]
    [SerializeField, Range(0.5f, 1.2f)] float tamanoBarras = 0.8f;

   [Header("Datos curiosos")]
[SerializeField, Min(2f)] float duracionTarjeta = 7f;
[SerializeField, Min(1f)] float pausaMinima = 10f;
[SerializeField, Min(1f)] float pausaMaxima = 20f;

float pausaEntreDatos = 15f;

    // Cada dato va como "texto|fuente". La fuente sale en chiquito abajo de la tarjeta.
    static readonly string[][] datosSala =
    {
        // 0 = Energía
        new[]
        {
            "A Marte llega menos de la mitad de la luz del Sol que a la Tierra, porque está más lejos. Por eso los paneles solares producen menos.|NASA - Datos de Marte",
            "El polvo cubre los paneles solares y les quita energía. Una tormenta de polvo dejó sin energía al rover Opportunity en 2018.|NASA - Misión Opportunity",
            "El rover Perseverance no usa paneles solares: lleva un generador que convierte el calor del plutonio en electricidad.|NASA - Misión Mars 2020",
            "El módulo InSight se apagó en 2022 porque el polvo de Marte cubrió poco a poco sus paneles solares.|NASA - Misión InSight",
            "El helicóptero Ingenuity cargaba su batería con un pequeño panel solar. ¡Voló 72 veces en Marte!|NASA - Ingenuity",
            "La NASA estudia pequeños reactores nucleares para dar energía a bases en la Luna y Marte, incluso de noche y con tormentas de polvo.|NASA - Fission Surface Power"
        },
        // 1 = Soporte vital
        new[]
        {
            "Cada astronauta necesita 0.84 kg de oxígeno al día y exhala 1 kg de dióxido de carbono que hay que limpiar del aire.|NASA - Soporte vital (ECLSS)",
            "MOXIE, en el rover Perseverance, fabricó oxígeno con el aire de Marte: hasta 12 gramos por hora y 122 gramos en total.|NASA - MOXIE, Mars 2020",
            "El aire de Marte es casi todo dióxido de carbono (95 %). No se puede respirar, pero sirve para fabricar oxígeno.|NASA - Datos de Marte",
            "En la Estación Espacial Internacional se recicla casi toda el agua (98 %), ¡hasta el sudor y la orina!|NASA - Estación Espacial",
            "En la estación espacial, parte del oxígeno se obtiene separando el agua con electricidad. Eso se llama electrólisis.|NASA - Soporte vital (ECLSS)",
            "La presión del aire en Marte es menos del 1 % de la de la Tierra. Sin traje espacial no podrías sobrevivir afuera.|NASA - Datos de Marte"
        },
        // 2 = Alimentos
        new[]
        {
            "Los astronautas ya cultivaron y comieron lechuga en la Estación Espacial Internacional con el experimento Veggie.|NASA - Veggie",
            "Cada astronauta come unos 0.62 kg de alimento seco al día. Para una misión larga no se puede llevar todo: hay que cultivar.|NASA - Soporte vital (ECLSS)",
            "El suelo de Marte tiene sales tóxicas llamadas percloratos. Por eso aquí las plantas crecen en agua con nutrientes: hidroponía.|NASA - Misión Phoenix",
            "En la estación espacial han crecido lechugas, rábanos, chiles y hasta flores.|NASA - Veggie y Advanced Plant Habitat",
            "Las plantas del espacio crecen con luces LED rojas y azules. Por eso el invernadero se ve de color rosa.|NASA - Veggie",
            "Las plantas ayudan dos veces: dan comida y además absorben dióxido de carbono y producen oxígeno.|NASA - Biología espacial"
        },
        // 3 = Radiación
        new[]
        {
            "En la superficie de Marte, el rover Curiosity midió 0.67 milisieverts de radiación al día. En la Tierra recibimos unos 3 en todo un año.|NASA - Instrumento RAD, Curiosity",
            "La NASA permite a un astronauta recibir como máximo 600 milisieverts en toda su carrera. Por eso la base necesita escudos.|NASA - Norma STD-3001",
            "La Tierra tiene un campo magnético que nos protege de la radiación del espacio. Marte perdió el suyo hace miles de millones de años.|NASA - Misión MAVEN",
            "Una forma de protegerse en Marte es cubrir la base con su propio suelo, llamado regolito, como en el techo de esta sala.|NASA - Hábitats en Marte",
            "Un viaje de ida y vuelta a Marte, con 500 días en la superficie, sumaría cerca de 1000 milisieverts de radiación.|NASA - Instrumento RAD, Curiosity",
            "En la Estación Espacial Internacional hay aparatos llamados dosímetros que miden la radiación todos los días.|NASA - RadLab (OSDR)"
        }
    };

    static readonly string[] datosGenerales =
    {
        "Un día en Marte se llama sol y dura 24 horas con 39 minutos.|NASA - Datos de Marte",
        "En Marte pesarías poco más de un tercio de lo que pesas en la Tierra.|NASA - Datos de Marte",
        "Un año en Marte dura 687 días de la Tierra: casi dos años.|NASA - Datos de Marte",
        "La temperatura promedio en Marte es de unos 63 grados bajo cero.|NASA - Datos de Marte",
        "El monte Olimpo, en Marte, es el volcán más alto del sistema solar: mide más del doble que el Everest.|NASA - Datos de Marte",
        "Marte se ve rojo porque su suelo tiene óxido de hierro, ¡como el metal oxidado!|NASA - Datos de Marte",
        "En el viaje a Marte la radiación es casi 3 veces mayor que en su superficie: 1.8 milisieverts al día.|NASA - Instrumento RAD, Curiosity",
        "Marte tiene dos lunas pequeñas: Fobos y Deimos.|NASA - Datos de Marte",
        "En Marte los atardeceres son azules, al revés que en la Tierra.|NASA - Curiosity",
        "Valles Marineris es un cañón de Marte de más de 4000 kilómetros de largo.|NASA - Datos de Marte",
        "Un viaje a Marte dura entre 6 y 9 meses.|NASA - Datos de Marte"
    };

    // Tormentas REALES registradas por la NASA. Salen en orden aleatorio cada vez que hay una tormenta en el juego.
    static readonly string[] eventosSolares =
    {
        "20 de mayo de 2024: una tormenta solar golpeó Marte. El rover Curiosity midió de golpe una radiación igual a 30 radiografías de tórax: la mayor de su misión.|NASA - Curiosity RAD, 2024",
        "Septiembre de 2017: una tormenta solar llegó a Marte. La radiación en la superficie se duplicó y hubo auroras en todo el planeta.|NASA - Curiosity RAD y MAVEN, 2017",
        "Noviembre de 2003: una de las llamaradas más fuertes jamás medidas dañó el instrumento de radiación de la nave Mars Odyssey, que orbitaba Marte.|NASA - Mars Odyssey, 2003",
        "Agosto de 1972: una gran tormenta solar ocurrió entre las misiones Apolo 16 y Apolo 17. Si hubiera habido astronautas en la Luna, habrían corrido peligro.|NASA - Programa Apolo",
        "Mayo de 2024: una tormenta solar provocó auroras en la Tierra que se vieron hasta en México.|NASA - Clima espacial, 2024",
        "3 de octubre de 2024: el Sol lanzó una llamarada clase X9, la más fuerte desde 2017.|NASA - Solar Dynamics Observatory",
        "Las tormentas solares lanzan partículas con muchísima energía. Como Marte no tiene campo magnético, hay que refugiarse tras los escudos.|NASA - Clima espacial"
    };

    static readonly string[] eventosPolvo =
    {
        "2018: una tormenta de polvo cubrió todo Marte durante semanas y dejó sin energía al rover Opportunity.|NASA - Misión Opportunity",
        "2007: una tormenta de polvo global oscureció el cielo de Marte. Los rovers Spirit y Opportunity sobrevivieron ahorrando energía, como tú ahora.|NASA - Mars Exploration Rovers",
        "1971: la nave Mariner 9 llegó a Marte en plena tormenta de polvo y tuvo que esperar semanas para poder ver la superficie.|NASA - Mariner 9",
        "Las tormentas de polvo de Marte pueden cubrir el planeta entero durante semanas y tapar la luz del Sol.|NASA - Datos de Marte"
    };

    // Respuesta de DONKI: una lista de llamaradas solares
    [Serializable] class Llamarada { public string beginTime; public string peakTime; public string classType; }
    [Serializable] class ListaLlamaradas { public Llamarada[] items; }

    static readonly string[] meses =
        { "enero", "febrero", "marzo", "abril", "mayo", "junio", "julio", "agosto", "septiembre", "octubre", "noviembre", "diciembre" };

    string resumenDonki = "";   // se muestra en la pantalla de instrucciones
    string datoDonki;           // se muestra cuando llega una tormenta solar
    int tormentasSolares;

    // Reparte los datos en orden aleatorio y sin repetir hasta haberlos mostrado todos
    class Bolsa
    {
        readonly string[] datos;
        readonly System.Collections.Generic.List<int> restantes = new System.Collections.Generic.List<int>();
        int ultimo = -1;

        public Bolsa(string[] datos) { this.datos = datos; }

        public string Siguiente()
        {
            if (restantes.Count == 0)
            {
                for (int i = 0; i < datos.Length; i++)
                    if (i != ultimo || datos.Length == 1) restantes.Add(i);
            }
            int pos = UnityEngine.Random.Range(0, restantes.Count);
            ultimo = restantes[pos];
            restantes.RemoveAt(pos);
            return datos[ultimo];
        }
    }

    Bolsa[] bolsasSala;
    Bolsa bolsaGeneral;
    Bolsa bolsaSolar;
    Bolsa bolsaPolvo;
    float tiempoSinDato;

    bool enInicio;
    bool enPortada;
    string dato;
    string fuente;
    float tiempoDato;

    Transform jugador;
    int salaActual = -1;
    int solVisto = 1;
    bool habiaPolvo;
    bool habiaSolar;

    // Pantallas de inicio: 0 = misión y controles, 1 = conoce tu base
    int paginaInicio;

    // Tutorial guiado
    bool enTutorial;
    int pasoTutorial;
    float xInicialTutorial;
    bool xGuardada;

    // Letreros de las puertas
    static readonly string[] nombresObjetoPuerta = { "Puerta_Energia", "Puerta_Soporte", "Puerta_Alimentos", "Puerta_Radiacion" };
    static readonly string[] nombresPuerta = { "ENERGÍA", "SOPORTE VITAL", "ALIMENTOS", "RADIACIÓN" };
    static readonly Color[] coloresPuerta =
    {
        new Color(1f, 0.85f, 0.3f), new Color(0.45f, 0.8f, 1f), new Color(0.5f, 1f, 0.5f), new Color(1f, 0.6f, 0.4f)
    };
    readonly Transform[] puertas = new Transform[4];
    float yPasillo;
    bool puertasBuscadas;
    Camera camara;
    GUIStyle estiloNombrePuerta;

    GUIStyle estiloDato;
    GUIStyle estiloTexto;
    GUIStyle estiloEtiqueta;
    GUIStyle estiloFuente;
    GUIStyle estiloTitulo;
    GUIStyle estiloCentro;

    void Start()
    {
        bolsasSala = new Bolsa[datosSala.Length];
        for (int i = 0; i < datosSala.Length; i++) bolsasSala[i] = new Bolsa(datosSala[i]);
        bolsaGeneral = new Bolsa(datosGenerales);
        bolsaSolar = new Bolsa(eventosSolares);
        bolsaPolvo = new Bolsa(eventosPolvo);

        if (mostrarInicio)
        {
            enInicio = true;
            enPortada = fondoInicio != null;
            Time.timeScale = 0f; // el juego espera hasta que pulsen ESPACIO
        }

        if (tutorialActivo && GameManager.Instancia != null)
        {
            enTutorial = true;
            pasoTutorial = 0;
            GameManager.Instancia.TiempoDetenido = true;
        }

        if (usarDonkiEnVivo)
        {
            resumenDonki = "Consultando el clima espacial real en NASA DONKI...";
            StartCoroutine(ConsultarDonki());
        }
    }

    IEnumerator ConsultarDonki()
    {
        DateTime hoy = DateTime.UtcNow;
        string fechas = "startDate=" + hoy.AddDays(-diasHaciaAtras).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) +
                        "&endDate=" + hoy.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

        // Se prueban dos servidores de la NASA con los mismos datos:
        // 1) el de DONKI directo (no pide clave)  2) api.nasa.gov (pide clave)
        string[] urls =
        {
            "https://kauai.ccmc.gsfc.nasa.gov/DONKI/WS/get/FLR?" + fechas,
            "https://api.nasa.gov/DONKI/FLR?" + fechas + "&api_key=" + claveApiNasa
        };

        for (int i = 0; i < urls.Length; i++)
        {
            string json = null;
            bool respondio = false;

            using (UnityWebRequest pedido = UnityWebRequest.Get(urls[i]))
            {
                pedido.timeout = 12;
                yield return pedido.SendWebRequest();

                if (pedido.result == UnityWebRequest.Result.Success)
                {
                    respondio = true;
                    json = pedido.downloadHandler.text;
                }
                else
                {
                    Debug.LogWarning("NASA DONKI (servidor " + (i + 1) + ") no respondió: " + pedido.error +
                                     " [código " + pedido.responseCode + "]");
                }
            }

            if (respondio && InterpretarDonki(json)) yield break;
            if (respondio) Debug.LogWarning("NASA DONKI (servidor " + (i + 1) + ") respondió algo que no se pudo leer.");
        }

        resumenDonki = "Sin conexión con NASA DONKI: el juego usará tormentas reales registradas por la NASA.";
    }

    // Devuelve true si la respuesta de la NASA se pudo leer
    bool InterpretarDonki(string json)
    {
        if (json == null) return false;
        json = json.Trim();

        // DONKI responde vacío cuando no hubo ninguna llamarada en esas fechas
        if (json.Length == 0)
        {
            resumenDonki = "Clima espacial real (NASA DONKI): el Sol ha estado tranquilo, sin llamaradas en los últimos " +
                           diasHaciaAtras + " días.";
            return true;
        }
        if (!json.StartsWith("[")) return false;

        ListaLlamaradas lista;
        try { lista = JsonUtility.FromJson<ListaLlamaradas>("{\"items\":" + json + "}"); }
        catch (Exception) { return false; }

        if (lista == null || lista.items == null) return false;

        if (lista.items.Length == 0)
        {
            resumenDonki = "Clima espacial real (NASA DONKI): el Sol ha estado tranquilo, sin llamaradas en los últimos " +
                           diasHaciaAtras + " días.";
            return true;
        }

        Llamarada mayor = null;
        float fuerzaMayor = -1f;
        foreach (Llamarada f in lista.items)
        {
            float fuerza = Fuerza(f.classType);
            if (fuerza > fuerzaMayor)
            {
                fuerzaMayor = fuerza;
                mayor = f;
            }
        }
        if (mayor == null || string.IsNullOrEmpty(mayor.classType)) return false;

        string fecha = FechaBonita(string.IsNullOrEmpty(mayor.peakTime) ? mayor.beginTime : mayor.peakTime);
        int n = lista.items.Length;

        resumenDonki = "Clima espacial real (NASA DONKI): " + n + " llamaradas solares en los últimos " +
                       diasHaciaAtras + " días. La más fuerte: clase " + mayor.classType + fecha + ".";
        datoDonki = "Dato real de la NASA: en los últimos " + diasHaciaAtras + " días el Sol lanzó " + n +
                    " llamaradas. La más fuerte fue clase " + mayor.classType + fecha +
                    ". Las de clase X son las más potentes.|NASA DONKI - datos en vivo";
        return true;
    }

    // "M5.2" -> número para comparar. Las letras van de menor a mayor: A, B, C, M, X
    static float Fuerza(string clase)
    {
        if (string.IsNullOrEmpty(clase)) return -1f;
        int letra = "ABCMX".IndexOf(char.ToUpperInvariant(clase[0]));
        if (letra < 0) return -1f;
        float numero;
        if (!float.TryParse(clase.Substring(1), NumberStyles.Float, CultureInfo.InvariantCulture, out numero)) numero = 0f;
        return letra * 1000f + numero;
    }

    // "2026-11-05T13:45Z" -> ", el 5 de noviembre"
    static string FechaBonita(string texto)
    {
        if (string.IsNullOrEmpty(texto) || texto.Length < 10) return "";
        int mes, dia;
        if (!int.TryParse(texto.Substring(5, 2), out mes) || !int.TryParse(texto.Substring(8, 2), out dia)) return "";
        if (mes < 1 || mes > 12) return "";
        return ", el " + dia + " de " + meses[mes - 1];
    }

    void OnDestroy()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (enInicio)
        {
            Keyboard teclado = Keyboard.current;
            if (teclado != null &&
                (teclado[Key.Space].wasPressedThisFrame || teclado[Key.Enter].wasPressedThisFrame))
            {
                if (enPortada)
                {
                    enPortada = false; // de la portada a las instrucciones
                }
                else if (paginaInicio == 0)
                {
                    paginaInicio = 1;  // de las instrucciones a "conoce tu base"
                }
                else
                {
                    enInicio = false;
                    Time.timeScale = 1f;
                }
            }
            return;
        }

        GameManager gm = GameManager.Instancia;
        if (gm == null) return;

        if (tiempoDato > 0f) tiempoDato -= Time.unscaledDeltaTime;
        else tiempoSinDato += Time.unscaledDeltaTime;
        if (!gm.JuegoActivo)
        {
            cola.Clear();
            eventosVistos = gm.ContadorEventos;
            return;
        }

if (tiempoDato <= 0f &&
    tiempoSinDato >= pausaEntreDatos &&
    cola.Count > 0)
        {
            string siguiente = cola[0];
            cola.RemoveAt(0);
            MostrarAhora(siguiente);
        }

        // ---- ¿En qué sala está el astronauta? ----
        if (jugador == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) jugador = obj.transform;
        }

        int sala = -1;
        if (jugador != null)
        {
            for (int i = 0; i < SalaInteractiva.Todas.Count; i++)
            {
                SalaInteractiva s = SalaInteractiva.Todas[i];
                Vector3 a = jugador.position;
                Vector3 b = s.transform.position;
                if (Mathf.Abs(a.x - b.x) < 6f && Mathf.Abs(a.y - b.y) < 4f)
                {
                    sala = (int)s.SalaControlada;
                    break;
                }
            }
        }

        if (enTutorial)
        {
            salaActual = sala;
            solVisto = gm.SolActual;
            eventosVistos = gm.ContadorEventos;
            habiaPolvo = gm.HayTormentaPolvo;
            habiaSolar = gm.HayTormentaSolar;
            AvanzarTutorial(gm);
            return;
        }

        if (sala != salaActual)
        {
            salaActual = sala;
            if (sala >= 0 && sala < bolsasSala.Length) Mostrar(bolsasSala[sala].Siguiente());
        }
        else if (sala >= 0 && sala < bolsasSala.Length && tiempoDato <= 0f && tiempoSinDato >= pausaEntreDatos)
        {
            // Si se queda en la sala, sale otro dato distinto cada cierto tiempo
            Mostrar(bolsasSala[sala].Siguiente());
        }

        // ---- Nuevo Sol ----
        if (gm.SolActual != solVisto)
        {
            bool avanzo = gm.SolActual > solVisto;
            solVisto = gm.SolActual;
            if (avanzo) Mostrar(bolsaGeneral.Siguiente());
        }

        // ---- Eventos de la base (averías, fugas, etc.) ----
        if (gm.ContadorEventos != eventosVistos)
        {
            bool nuevo = gm.ContadorEventos > eventosVistos;
            eventosVistos = gm.ContadorEventos;
            if (nuevo) Mostrar(gm.UltimoDatoEvento);
        }

        // ---- Tormentas ----
        if (gm.HayTormentaPolvo && !habiaPolvo) Mostrar(bolsaPolvo.Siguiente());
        if (gm.HayTormentaSolar && !habiaSolar)
        {
            // La primera vez, el dato en vivo de DONKI (si hubo conexión); después, tormentas reales guardadas
            if (tormentasSolares == 0 && !string.IsNullOrEmpty(datoDonki)) Mostrar(datoDonki);
            else Mostrar(bolsaSolar.Siguiente());
            tormentasSolares++;
        }
        habiaPolvo = gm.HayTormentaPolvo;
        habiaSolar = gm.HayTormentaSolar;
    }

    readonly System.Collections.Generic.List<string> cola = new System.Collections.Generic.List<string>();
    int eventosVistos;

    // ---------------- Tutorial guiado ----------------
    void AvanzarTutorial(GameManager gm)
    {
        Keyboard teclado = Keyboard.current;

        // T salta el tutorial en cualquier momento
        if (teclado != null && teclado[Key.T].wasPressedThisFrame)
        {
            TerminarTutorial(gm);
            return;
        }

        switch (pasoTutorial)
        {
            case 0: // caminar
                if (jugador == null) break;
                if (!xGuardada)
                {
                    xInicialTutorial = jugador.position.x;
                    xGuardada = true;
                }
                if (Mathf.Abs(jugador.position.x - xInicialTutorial) > 1.5f) pasoTutorial = 1;
                break;

            case 1: // entrar a la sala de Radiación
                if (salaActual == (int)Sala.Radiacion)
                    pasoTutorial = gm.EstaActiva(Sala.Radiacion) ? 3 : 2;
                break;

            case 2: // encender los escudos
                if (gm.EstaActiva(Sala.Radiacion)) pasoTutorial = 3;
                else if (salaActual != (int)Sala.Radiacion) pasoTutorial = 1;
                break;

            case 3: // salir al pasillo
                if (salaActual == -1) pasoTutorial = 4;
                break;

            default: // resumen final: ESPACIO para empezar
                if (teclado != null &&
                    (teclado[Key.Space].wasPressedThisFrame || teclado[Key.Enter].wasPressedThisFrame))
                    TerminarTutorial(gm);
                break;
        }
    }

    void TerminarTutorial(GameManager gm)
    {
        enTutorial = false;
        gm.TiempoDetenido = false;
        cola.Clear();
        tiempoDato = 0f;
    }

    string TextoTutorial()
    {
        switch (pasoTutorial)
        {
            case 0:
                return "Paso 1 de 5.  Camina por el pasillo con las flechas IZQUIERDA y DERECHA (o las teclas A y D).";
            case 1:
                if (salaActual >= 0)
                    return "Paso 2 de 5.  Esta no es la sala de los escudos. Sal caminando hasta el borde izquierdo y busca la última puerta de la derecha: RADIACIÓN.";
                return "Paso 2 de 5.  Ve a la última puerta de la derecha, la que dice RADIACIÓN. Párate frente a ella y pulsa E para entrar.";
            case 2:
                return "Paso 3 de 5.  Esta sala protege a la base de la radiación del espacio. Los escudos están APAGADOS: pulsa E para encenderlos.";
            case 3:
                return "Paso 4 de 5.  ¡Bien! Mira arriba a la izquierda: Radiación ya dice ENCENDIDO. Ahora camina hasta el borde izquierdo para volver al pasillo.";
            default:
                return "Paso 5 de 5.  Cada puerta cuida una barra: ENERGÍA, SOPORTE VITAL (el aire), ALIMENTOS (la comida) y RADIACIÓN (los escudos). Todo gasta energía, así que no siempre podrás tener todo encendido. Pulsa ESPACIO para empezar la misión.";
        }
    }

    // Si ya hay una tarjeta en pantalla, la nueva espera su turno (y la actual se acorta)
    void Mostrar(string texto)
{
    if (string.IsNullOrEmpty(texto)) return;

    if (tiempoDato > 0f || tiempoSinDato < pausaEntreDatos)
    {
        if (cola.Count < 3)
            cola.Add(texto);

        return;
    }

    MostrarAhora(texto);
}

    void MostrarAhora(string texto)
    {
        int corte = texto.IndexOf('|');
        if (corte >= 0)
        {
            dato = texto.Substring(0, corte);
            fuente = texto.Substring(corte + 1);
        }
        else
        {
            dato = texto;
            fuente = "";
        }
        tiempoDato = duracionTarjeta;
        tiempoSinDato = 0f;
        pausaEntreDatos = UnityEngine.Random.Range(pausaMinima, pausaMaxima);
    }

    // ------------------------------------------------------------------
    void OnGUI()
    {
        CrearEstilos();

        float escala = Screen.height / 720f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
        float ancho = Screen.width / escala;

        if (enInicio)
        {
            GUI.depth = -10; // por encima de las barras
            DibujarInicio(ancho);
        }
        else if (enTutorial)
        {
            DibujarNombresPuertas(escala);
            DibujarTutorial(ancho);
        }
        else if (tiempoDato > 0f && !string.IsNullOrEmpty(dato) &&
                 GameManager.Instancia != null && GameManager.Instancia.JuegoActivo)
        {
            DibujarNombresPuertas(escala);
            DibujarDato(ancho);
        }
        else
        {
            DibujarNombresPuertas(escala);
        }

        GUI.matrix = Matrix4x4.identity;
    }

    // Letrero en español sobre cada puerta del pasillo (solo cuando el astronauta está en el pasillo)
    void DibujarNombresPuertas(float escala)
    {
        if (!mostrarNombresPuertas || salaActual != -1) return;
        GameManager gm = GameManager.Instancia;
        if (gm == null || !gm.JuegoActivo) return;

        if (!puertasBuscadas)
        {
            puertasBuscadas = true;
            for (int i = 0; i < puertas.Length; i++)
            {
                GameObject obj = GameObject.Find(nombresObjetoPuerta[i]);
                if (obj != null) puertas[i] = obj.transform;
            }
            GameObject centro = GameObject.Find("CentroMando");
            if (centro != null) yPasillo = centro.transform.position.y;
        }

        if (camara == null) camara = Camera.main;
        if (camara == null) return;

        for (int i = 0; i < puertas.Length; i++)
        {
            if (puertas[i] == null) continue;

            Vector3 mundo = new Vector3(puertas[i].position.x, yPasillo + alturaNombres, 0f);
            Vector3 p = camara.WorldToScreenPoint(mundo);
            float x = p.x / escala;
            float y = (Screen.height - p.y) / escala;

            Rect r = new Rect(x - 104f, y - 24f, 208f, 48f);
            Caja(r, new Color(0.04f, 0.06f, 0.12f, 0.95f));
            Caja(new Rect(r.x, r.y + r.height - 5f, r.width, 5f), coloresPuerta[i]);

            GUI.color = coloresPuerta[i];
            GUI.Label(r, nombresPuerta[i], estiloNombrePuerta);
            GUI.color = Color.white;
        }
    }

    // Zona libre arriba, entre el panel de barras (izquierda) y el del Sol (derecha)
    Rect ZonaTarjeta(float ancho, float alto)
    {
        float izquierda = 12f + 440f * tamanoBarras + 12f;
        Rect r = new Rect(izquierda, 12f, ancho - izquierda - 264f, alto);
        if (r.width < 380f) r = new Rect(12f, 12f + 300f * tamanoBarras + 10f, ancho - 24f, alto);
        return r;
    }

    void DibujarDato(float ancho)
    {
        Rect r = ZonaTarjeta(ancho, 232f);

        Caja(r, new Color(0.05f, 0.1f, 0.2f, 0.92f));
        Caja(new Rect(r.x, r.y, 8f, r.height), new Color(1f, 0.8f, 0.2f));

        GUI.color = new Color(1f, 0.8f, 0.2f);
        GUI.Label(new Rect(r.x + 22f, r.y + 8f, r.width - 34f, 30f), "¿SABÍAS QUE...?", estiloEtiqueta);
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 22f, r.y + 40f, r.width - 34f, r.height - 72f), dato, estiloDato);

        if (!string.IsNullOrEmpty(fuente))
        {
            GUI.color = new Color(0.7f, 0.85f, 1f);
            GUI.Label(new Rect(r.x + 22f, r.y + r.height - 28f, r.width - 34f, 24f), "Fuente: " + fuente, estiloFuente);
            GUI.color = Color.white;
        }
    }

    void DibujarTutorial(float ancho)
    {
        Rect r = ZonaTarjeta(ancho, 300f);

        Caja(r, new Color(0.08f, 0.2f, 0.1f, 0.94f));
        Caja(new Rect(r.x, r.y, 8f, r.height), new Color(0.5f, 1f, 0.5f));

        GUI.color = new Color(0.5f, 1f, 0.5f);
        GUI.Label(new Rect(r.x + 22f, r.y + 8f, r.width - 34f, 30f), "TUTORIAL (el tiempo está detenido)", estiloEtiqueta);
        GUI.color = Color.white;
        GUI.Label(new Rect(r.x + 22f, r.y + 40f, r.width - 34f, r.height - 72f), TextoTutorial(), estiloDato);

        GUI.color = new Color(0.8f, 0.8f, 0.8f);
        GUI.Label(new Rect(r.x + 22f, r.y + r.height - 28f, r.width - 34f, 24f), "Tecla T: saltar el tutorial", estiloFuente);
        GUI.color = Color.white;
    }

    void DibujarInicio(float ancho)
    {
        Rect todo = new Rect(0f, 0f, ancho, 720f);
        Caja(todo, Color.black);

        if (fondoInicio != null)
        {
            int n = Mathf.Max(1, cuadrosFondo);
            int cuadro = (int)(Time.unscaledTime * cuadrosFondoPorSegundo) % n;
            GUI.DrawTextureWithTexCoords(todo, fondoInicio, new Rect(cuadro / (float)n, 0f, 1f / n, 1f));

            if (enPortada)
            {
                // Portada: solo el arte y el aviso para continuar
                if (Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.8f)
                {
                    GUI.color = new Color(0.5f, 1f, 0.5f);
                    GUI.Label(new Rect(0f, 610f, ancho, 50f), "Pulsa ESPACIO", estiloTitulo2());
                    GUI.color = Color.white;
                }
                return;
            }

            Caja(todo, new Color(0f, 0f, 0f, 0.75f));
        }

        if (paginaInicio == 1)
        {
            DibujarConoceBase(ancho);
            return;
        }

        int soles = GameManager.Instancia != null ? GameManager.Instancia.TotalSoles : 12;

        GUI.color = new Color(1f, 0.8f, 0.2f);
        GUI.Label(new Rect(0f, 70f, ancho, 80f), "MISIÓN: SOBREVIVIR EN MARTE", estiloTitulo);
        GUI.color = Color.white;

        float x = Mathf.Max(40f, (ancho - 900f) / 2f);
        float w = Mathf.Min(900f, ancho - 80f);

        GUI.Label(new Rect(x, 170f, w, 70f),
            "Eres parte de la primera tripulación en Marte. Mantén viva la base durante " + soles +
            " soles. Un sol es un día marciano: dura 24 horas con 39 minutos.", estiloCentro);

        GUI.Label(new Rect(x, 262f, w, 34f), "Flechas o A / D: caminar por la base", estiloCentro);
        GUI.Label(new Rect(x, 300f, w, 34f), "E frente a una puerta: entrar a la sala", estiloCentro);
        GUI.Label(new Rect(x, 338f, w, 34f), "E en el centro de una sala: encender o apagar su sistema", estiloCentro);

        GUI.Label(new Rect(x, 400f, w, 100f),
            "Vigila las 4 barras. Todos los sistemas gastan energía, así que a veces tendrás que " +
            "apagar uno para salvar otro. Si se acaba el aire o la comida, o la radiación llega al máximo, la misión termina.",
            estiloCentro);

        if (!string.IsNullOrEmpty(resumenDonki))
        {
            GUI.color = new Color(0.7f, 0.85f, 1f);
            GUI.Label(new Rect(x, 505f, w, 50f), resumenDonki, estiloTexto2());
            GUI.color = Color.white;
        }

        bool visible = Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.8f;
        if (visible)
        {
            GUI.color = new Color(0.5f, 1f, 0.5f);
            GUI.Label(new Rect(0f, 590f, ancho, 50f), "Pulsa ESPACIO para continuar", estiloTitulo2());
            GUI.color = Color.white;
        }
    }

    void DibujarConoceBase(float ancho)
    {
        GUI.color = new Color(1f, 0.8f, 0.2f);
        GUI.Label(new Rect(0f, 50f, ancho, 80f), "CONOCE TU BASE", estiloTitulo);
        GUI.color = Color.white;

        float x = Mathf.Max(40f, (ancho - 1000f) / 2f);
        float w = Mathf.Min(1000f, ancho - 80f);

        GUI.Label(new Rect(x, 140f, w, 40f), "La base tiene un pasillo con 4 puertas. Cada sala cuida una barra:", estiloCentro);

        FilaSala(x, w, 200f, new Color(1f, 0.85f, 0.3f), "ENERGÍA", "El generador da electricidad a toda la base.");
        FilaSala(x, w, 268f, new Color(0.45f, 0.8f, 1f), "SOPORTE VITAL", "Fabrica el aire para respirar y recicla el agua.");
        FilaSala(x, w, 336f, new Color(0.5f, 1f, 0.5f), "ALIMENTOS", "El invernadero donde crecen las plantas que come la tripulación.");
        FilaSala(x, w, 404f, new Color(1f, 0.6f, 0.4f), "RADIACIÓN", "Los escudos protegen a la tripulación de la radiación del espacio.");

        GUI.Label(new Rect(x, 480f, w, 80f),
            "Si apagas una sala, su barra empieza a bajar (o la radiación a subir). " +
            "Debajo de cada barra el juego te dice POR QUÉ sube o baja.", estiloCentro);

        if (Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.8f)
        {
            GUI.color = new Color(0.5f, 1f, 0.5f);
            GUI.Label(new Rect(0f, 600f, ancho, 50f),
                tutorialActivo ? "Pulsa ESPACIO para empezar el tutorial" : "Pulsa ESPACIO para empezar", estiloTitulo2());
            GUI.color = Color.white;
        }
    }

    void FilaSala(float x, float w, float y, Color color, string puerta, string texto)
    {
        Caja(new Rect(x, y, w, 58f), new Color(1f, 1f, 1f, 0.08f));
        Caja(new Rect(x, y, 8f, 58f), color);

        GUI.color = color;
        GUI.Label(new Rect(x + 24f, y, 230f, 58f), puerta, estiloPuerta);
        GUI.color = Color.white;
        GUI.Label(new Rect(x + 260f, y, w - 270f, 58f), texto, estiloFila);
    }

    GUIStyle estiloPuerta;
    GUIStyle estiloFila;

    GUIStyle estiloSub;
    GUIStyle estiloTitulo2()
    {
        if (estiloSub == null)
        {
            estiloSub = new GUIStyle(estiloCentro);
            estiloSub.fontSize = 30;
        }
        return estiloSub;
    }

    GUIStyle estiloResumen;
    GUIStyle estiloTexto2()
    {
        if (estiloResumen == null)
        {
            estiloResumen = new GUIStyle(estiloCentro);
            estiloResumen.fontSize = 18;
        }
        return estiloResumen;
    }

    static void Caja(Rect r, Color color)
    {
        Color anterior = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = anterior;
    }

    void CrearEstilos()
    {
        if (estiloTexto != null) return;

        estiloTexto = new GUIStyle(GUI.skin.label);
        estiloTexto.fontSize = 19;
        estiloTexto.wordWrap = true;
        estiloTexto.normal.textColor = Color.white;

        // Texto grande de las tarjetas de datos y del tutorial
        estiloDato = new GUIStyle(estiloTexto);
        estiloDato.fontSize = 25;

        estiloEtiqueta = new GUIStyle(estiloTexto);
        estiloEtiqueta.fontSize = 20;
        estiloEtiqueta.fontStyle = FontStyle.Bold;
        estiloEtiqueta.wordWrap = false;

        estiloFuente = new GUIStyle(estiloTexto);
        estiloFuente.fontSize = 16;
        estiloFuente.wordWrap = false;

        estiloCentro = new GUIStyle(estiloTexto);
        estiloCentro.fontSize = 24;
        estiloCentro.alignment = TextAnchor.MiddleCenter;

        estiloTitulo = new GUIStyle(estiloCentro);
        estiloTitulo.fontSize = 52;
        estiloTitulo.fontStyle = FontStyle.Bold;

        estiloPuerta = new GUIStyle(estiloTexto);
        estiloPuerta.fontSize = 26;
        estiloPuerta.fontStyle = FontStyle.Bold;
        estiloPuerta.alignment = TextAnchor.MiddleLeft;
        estiloPuerta.wordWrap = false;

        estiloFila = new GUIStyle(estiloTexto);
        estiloFila.fontSize = 22;
        estiloFila.alignment = TextAnchor.MiddleLeft;

        estiloNombrePuerta = new GUIStyle(estiloTexto);
        estiloNombrePuerta.fontSize = 22;
        estiloNombrePuerta.fontStyle = FontStyle.Bold;
        estiloNombrePuerta.alignment = TextAnchor.MiddleCenter;
        estiloNombrePuerta.wordWrap = false;
    }
}
