using UnityEngine;
using UnityEngine.InputSystem;

// Ponlo en el objeto GameManager. Dibuja las 4 barras, el Sol actual,
// los avisos y la pantalla de victoria/derrota. No necesita Canvas.
public class HudSupervivencia : MonoBehaviour
{
    [SerializeField, Min(1f)] float duracionAviso = 8f;
    [SerializeField] Key teclaReintentar = Key.R;
    [Tooltip("Tamaño del panel de las 4 barras. 1 = grande, 0.8 = más chico.")]
    [SerializeField, Range(0.5f, 1.2f)] float tamanoBarras = 0.8f;
    [Tooltip("Tamaño de la tripulación para el informe final con datos reales.")]
    [SerializeField, Min(1)] int tripulacion = 4;

    // Datos reales por persona y por día terrestre
    const float OxigenoKgDia = 0.84f;      // NASA, soporte vital (ECLSS)
    const float Co2KgDia = 1.00f;          // NASA, soporte vital (ECLSS)
    const float ComidaSecaKgDia = 0.62f;   // NASA, soporte vital (ECLSS)
    const float RadiacionMsvDia = 0.67f;   // Curiosity, instrumento RAD
    const float LimiteCarreraMsv = 600f;   // Norma NASA-STD-3001
    const float DiasPorSol = 1.0275f;      // un sol = 24 h 39 min

    string avisoActual;
    float tiempoAviso;

    bool suscrito;
    bool terminada;
    bool gano;
    string razon = "";
    string leccion = "";
    int solesVividos = 1;

    Transform jugador;
    Vector3 posInicialJugador;
    Vector3 posInicialCamara;
    bool posicionesGuardadas;

    GUIStyle estiloTexto;
    GUIStyle estiloCentro;
    GUIStyle estiloTitulo;
    GUIStyle estiloPequeno;
    GUIStyle estiloNombre;
    GUIStyle estiloRazon;
    GUIStyle estiloAviso;

    void Start()
    {
        GameObject obj = GameObject.FindGameObjectWithTag("Player");
        if (obj != null)
        {
            jugador = obj.transform;
            posInicialJugador = jugador.position;
        }
        if (Camera.main != null) posInicialCamara = Camera.main.transform.position;
        posicionesGuardadas = true;
    }

    void Update()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null) return;

        if (!suscrito)
        {
            gm.PartidaTerminada += AlTerminar;
            suscrito = true;
        }

        if (gm.UltimaAlerta != avisoActual)
        {
            avisoActual = gm.UltimaAlerta;
            tiempoAviso = duracionAviso;
        }
        if (tiempoAviso > 0f) tiempoAviso -= Time.unscaledDeltaTime;

        if (terminada && Keyboard.current != null &&
            Keyboard.current[teclaReintentar].wasPressedThisFrame)
        {
            Reintentar(gm);
        }
    }

    void OnDestroy()
    {
        if (suscrito && GameManager.Instancia != null)
            GameManager.Instancia.PartidaTerminada -= AlTerminar;
    }

    void AlTerminar(bool victoria, string motivo)
    {
        terminada = true;
        gano = victoria;
        razon = motivo;

        GameManager gm = GameManager.Instancia;
        solesVividos = gm != null ? gm.SolActual : 1;

        if (victoria)
            leccion = "Lograste equilibrar energía, aire, comida y protección. Así se planea una misión real: ningún sistema funciona solo.";
        else if (gm != null && gm.SoporteVital <= 0f)
            leccion = "El soporte vital necesita energía todo el tiempo. En una base real, el aire y el agua se reciclan sin parar.";
        else if (gm != null && gm.Comida <= 0f)
            leccion = "Las plantas necesitan luz, agua y energía todos los días. Si apagas el invernadero mucho tiempo, la cosecha se pierde.";
        else
            leccion = "Sin escudos, la radiación se acumula. En una tormenta solar, los astronautas se refugian en la zona más protegida.";
    }

    void Reintentar(GameManager gm)
    {
        terminada = false;

        if (posicionesGuardadas)
        {
            if (jugador != null)
            {
                Rigidbody2D rb = jugador.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = Vector2.zero;
                    rb.position = posInicialJugador;
                }
                jugador.position = posInicialJugador;
                Physics2D.SyncTransforms();
            }
            if (Camera.main != null) Camera.main.transform.position = posInicialCamara;
        }

        gm.Reiniciar();
    }

    // ------------------------------------------------------------------
    void OnGUI()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null) return;

        CrearEstilos();

        // Todo se dibuja como si la pantalla midiera 720 de alto y luego se escala
        float escala = Screen.height / 720f;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(escala, escala, 1f));
        float ancho = Screen.width / escala;
        float alto = 720f;

        // ---- Panel de recursos (arriba a la izquierda), con su propio tamaño ----
        Matrix4x4 matrizNormal = GUI.matrix;
        float e = escala * tamanoBarras;
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(e, e, 1f));

        Caja(new Rect(12f, 12f, 440f, 300f), new Color(0f, 0f, 0f, 0.7f));
        Barra(20f, "Energía", gm.Energia, false, Sala.Energia, gm);
        Barra(92f, "Soporte vital (aire)", gm.SoporteVital, false, Sala.Soporte, gm);
        Barra(164f, "Comida", gm.Comida, false, Sala.Alimentos, gm);
        Barra(236f, "Radiación (peligro)", gm.Radiacion, true, Sala.Radiacion, gm);

        GUI.matrix = matrizNormal;

        // ---- Sol actual (arriba a la derecha) ----
        Rect cajaSol = new Rect(ancho - 252f, 12f, 240f, 84f);
        Caja(cajaSol, new Color(0f, 0f, 0f, 0.65f));
        GUI.Label(new Rect(cajaSol.x, cajaSol.y + 6f, cajaSol.width, 34f),
            "Sol " + gm.SolActual + " de " + gm.TotalSoles, estiloCentro);
        Rect fondoSol = new Rect(cajaSol.x + 16f, cajaSol.y + 50f, cajaSol.width - 32f, 16f);
        Caja(fondoSol, new Color(1f, 1f, 1f, 0.2f));
        Caja(new Rect(fondoSol.x, fondoSol.y, fondoSol.width * Mathf.Clamp01(gm.ProgresoSol), fondoSol.height),
            new Color(1f, 0.75f, 0.2f));

        // ---- Tormentas ----
        float yTormenta = 104f;
        if (gm.HayTormentaPolvo)
        {
            Etiqueta(new Rect(ancho - 252f, yTormenta, 240f, 34f), "Tormenta de polvo", new Color(0.9f, 0.6f, 0.3f));
            yTormenta += 40f;
        }
        if (gm.HayTormentaSolar)
        {
            Etiqueta(new Rect(ancho - 252f, yTormenta, 240f, 34f), "¡Tormenta solar!", new Color(1f, 0.35f, 0.3f));
        }

        // ---- Aviso (abajo al centro) ----
        if (!terminada && tiempoAviso > 0f && !string.IsNullOrEmpty(avisoActual))
        {
            float anchoAviso = Mathf.Min(ancho - 40f, 1000f);
            Rect r = new Rect((ancho - anchoAviso) / 2f, alto - 96f, anchoAviso, 80f);
            Caja(r, new Color(0f, 0f, 0f, 0.8f));
            GUI.Label(r, avisoActual, estiloAviso);
        }

        // ---- Pantalla final: resultado, lección y datos reales ----
        if (terminada)
        {
            Caja(new Rect(0f, 0f, ancho, alto), new Color(0f, 0f, 0f, 0.88f));

            GUI.color = gano ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f);
            GUI.Label(new Rect(0f, 40f, ancho, 80f), gano ? "¡MISIÓN CUMPLIDA!" : "MISIÓN FALLIDA", estiloTitulo);
            GUI.color = Color.white;

            float x = Mathf.Max(40f, (ancho - 940f) / 2f);
            float w = Mathf.Min(940f, ancho - 80f);

            GUI.Label(new Rect(x, 124f, w, 40f), razon, estiloCentro);
            GUI.Label(new Rect(x, 164f, w, 36f),
                "Sobreviviste " + solesVividos + " de " + gm.TotalSoles + " soles.", estiloCentro);

            GUI.color = new Color(1f, 0.85f, 0.4f);
            GUI.Label(new Rect(x, 210f, w, 70f), leccion, estiloCentro);
            GUI.color = Color.white;

            // Datos reales calculados para la misión que acaba de jugar
            float dias = solesVividos * DiasPorSol;
            float oxigeno = OxigenoKgDia * tripulacion * dias;
            float co2 = Co2KgDia * tripulacion * dias;
            float comida = ComidaSecaKgDia * tripulacion * dias;
            float dosis = RadiacionMsvDia * dias;

            Rect caja = new Rect(x, 300f, w, 250f);
            Caja(caja, new Color(0.05f, 0.1f, 0.2f, 0.95f));
            Caja(new Rect(caja.x, caja.y, 6f, caja.height), new Color(1f, 0.8f, 0.2f));

            GUI.color = new Color(1f, 0.8f, 0.2f);
            GUI.Label(new Rect(caja.x + 22f, caja.y + 10f, caja.width - 40f, 28f),
                "EN UNA MISIÓN REAL: " + tripulacion + " astronautas durante " + solesVividos + " soles necesitarían...", estiloTexto);
            GUI.color = Color.white;

            float y = caja.y + 48f;
            Linea(caja, ref y, "Oxígeno: " + oxigeno.ToString("0.0") + " kg  (0.84 kg por persona al día)");
            Linea(caja, ref y, "Dióxido de carbono que limpiar del aire: " + co2.ToString("0.0") + " kg  (1 kg por persona al día)");
            Linea(caja, ref y, "Comida seca: " + comida.ToString("0.0") + " kg  (0.62 kg por persona al día)");
            Linea(caja, ref y, "Radiación recibida en la superficie: " + dosis.ToString("0.0") +
                " milisieverts  (0.67 al día, medido por el rover Curiosity)");
            Linea(caja, ref y, "Límite de la NASA para toda la carrera de un astronauta: " +
                LimiteCarreraMsv.ToString("0") + " milisieverts");

            GUI.color = new Color(0.7f, 0.85f, 1f);
            GUI.Label(new Rect(caja.x + 22f, caja.y + caja.height - 30f, caja.width - 40f, 24f),
                "Fuentes: NASA, soporte vital (ECLSS) - Curiosity, instrumento RAD - Norma NASA-STD-3001", estiloPequeno);
            GUI.color = Color.white;

            GUI.Label(new Rect(0f, 580f, ancho, 50f),
                "Pulsa " + teclaReintentar + " para volver a jugar", estiloCentro);
        }

        GUI.matrix = Matrix4x4.identity;
    }

    void Barra(float y, string nombre, float valor, bool altoEsMalo, Sala sala, GameManager gm)
    {
        float x = 24f;
        float anchoBarra = 416f;
        bool encendida = gm.EstaActiva(sala);

        GUI.Label(new Rect(x, y, 270f, 26f), nombre, estiloNombre);

        GUI.color = encendida ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.4f);
        GUI.Label(new Rect(x + 270f, y, anchoBarra - 270f, 26f), encendida ? "ENCENDIDO" : "APAGADO", estiloNombre);
        GUI.color = Color.white;

        Rect fondo = new Rect(x, y + 27f, anchoBarra, 18f);
        Caja(fondo, new Color(1f, 1f, 1f, 0.2f));

        float peligro = altoEsMalo ? valor : 100f - valor;
        Color color = peligro > 80f ? new Color(0.95f, 0.25f, 0.2f)
                    : peligro > 60f ? new Color(1f, 0.8f, 0.2f)
                    : new Color(0.3f, 0.85f, 0.4f);
        Caja(new Rect(fondo.x, fondo.y, fondo.width * Mathf.Clamp01(valor / 100f), fondo.height), color);

        // Por qué sube o baja
        float t = gm.Tendencia(sala);
        bool enTope = (t > 0f && valor >= 99.9f) || (t < 0f && valor <= 0.1f);
        string texto;
        Color colorTexto;

        if (enTope || Mathf.Abs(t) < 0.001f)
        {
            texto = "ESTABLE: " + gm.Explicacion(sala);
            colorTexto = Color.white;
        }
        else
        {
            bool sube = t > 0f;
            bool esMalo = altoEsMalo ? sube : !sube;
            texto = (sube ? "SUBE: " : "BAJA: ") + gm.Explicacion(sala);
            colorTexto = esMalo ? new Color(1f, 0.6f, 0.45f) : new Color(0.6f, 1f, 0.6f);
        }

        GUI.color = colorTexto;
        GUI.Label(new Rect(x, y + 46f, anchoBarra, 22f), texto, estiloRazon);
        GUI.color = Color.white;
    }

    void Linea(Rect caja, ref float y, string texto)
    {
        GUI.Label(new Rect(caja.x + 22f, y, caja.width - 40f, 28f), texto, estiloTexto);
        y += 31f;
    }

    void Etiqueta(Rect r, string texto, Color color)
    {
        Caja(r, new Color(0f, 0f, 0f, 0.65f));
        GUI.color = color;
        GUI.Label(r, texto, estiloCentro);
        GUI.color = Color.white;
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
        estiloTexto.fontSize = 17;
        estiloTexto.fontStyle = FontStyle.Bold;
        estiloTexto.normal.textColor = Color.white;

        estiloCentro = new GUIStyle(estiloTexto);
        estiloCentro.fontSize = 22;
        estiloCentro.alignment = TextAnchor.MiddleCenter;
        estiloCentro.wordWrap = true;

        estiloTitulo = new GUIStyle(estiloCentro);
        estiloTitulo.fontSize = 60;

        estiloPequeno = new GUIStyle(estiloTexto);
        estiloPequeno.fontSize = 13;
        estiloPequeno.fontStyle = FontStyle.Normal;

        estiloNombre = new GUIStyle(estiloTexto);
        estiloNombre.fontSize = 19;

        estiloRazon = new GUIStyle(estiloTexto);
        estiloRazon.fontSize = 15;

        estiloAviso = new GUIStyle(estiloCentro);
        estiloAviso.fontSize = 26;
    }
}
