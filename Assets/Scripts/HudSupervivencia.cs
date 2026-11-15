using UnityEngine;
using UnityEngine.InputSystem;

// Ponlo en el objeto GameManager. Dibuja las 4 barras, el Sol actual,
// los avisos y la pantalla de victoria/derrota. No necesita Canvas.
public class HudSupervivencia : MonoBehaviour
{
    [SerializeField, Min(1f)] float segundosAviso = 5f;
    [SerializeField] Key teclaReintentar = Key.R;

    string avisoActual;
    float tiempoAviso;

    bool suscrito;
    bool terminada;
    bool gano;
    string razon = "";

    Transform jugador;
    Vector3 posInicialJugador;
    Vector3 posInicialCamara;
    bool posicionesGuardadas;

    GUIStyle estiloTexto;
    GUIStyle estiloCentro;
    GUIStyle estiloTitulo;

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
            tiempoAviso = segundosAviso;
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

        // ---- Panel de recursos (arriba a la izquierda) ----
        Caja(new Rect(12f, 12f, 360f, 196f), new Color(0f, 0f, 0f, 0.65f));
        Barra(22f, "Energía", gm.Energia, false, gm.EstaActiva(Sala.Energia));
        Barra(68f, "Soporte vital", gm.SoporteVital, false, gm.EstaActiva(Sala.Soporte));
        Barra(114f, "Comida", gm.Comida, false, gm.EstaActiva(Sala.Alimentos));
        Barra(160f, "Radiación", gm.Radiacion, true, gm.EstaActiva(Sala.Radiacion));

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
            float anchoAviso = Mathf.Min(ancho - 40f, 900f);
            Rect r = new Rect((ancho - anchoAviso) / 2f, alto - 86f, anchoAviso, 64f);
            Caja(r, new Color(0f, 0f, 0f, 0.75f));
            GUI.Label(r, avisoActual, estiloCentro);
        }

        // ---- Pantalla final ----
        if (terminada)
        {
            Caja(new Rect(0f, 0f, ancho, alto), new Color(0f, 0f, 0f, 0.8f));

            GUI.color = gano ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.45f, 0.4f);
            GUI.Label(new Rect(0f, 220f, ancho, 90f), gano ? "¡MISIÓN CUMPLIDA!" : "MISIÓN FALLIDA", estiloTitulo);
            GUI.color = Color.white;

            GUI.Label(new Rect(40f, 320f, ancho - 80f, 80f), razon, estiloCentro);
            GUI.Label(new Rect(0f, 430f, ancho, 50f),
                "Pulsa " + teclaReintentar + " para volver a jugar", estiloCentro);
        }

        GUI.matrix = Matrix4x4.identity;
    }

    void Barra(float y, string nombre, float valor, bool altoEsMalo, bool encendida)
    {
        float x = 24f;
        GUI.Label(new Rect(x, y, 200f, 22f), nombre, estiloTexto);

        GUI.color = encendida ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.5f, 0.4f);
        GUI.Label(new Rect(x + 200f, y, 136f, 22f), encendida ? "ENCENDIDO" : "APAGADO", estiloTexto);
        GUI.color = Color.white;

        Rect fondo = new Rect(x, y + 22f, 336f, 16f);
        Caja(fondo, new Color(1f, 1f, 1f, 0.2f));

        float peligro = altoEsMalo ? valor : 100f - valor;
        Color color = peligro > 80f ? new Color(0.95f, 0.25f, 0.2f)
                    : peligro > 60f ? new Color(1f, 0.8f, 0.2f)
                    : new Color(0.3f, 0.85f, 0.4f);
        Caja(new Rect(fondo.x, fondo.y, fondo.width * Mathf.Clamp01(valor / 100f), fondo.height), color);
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
    }
}
