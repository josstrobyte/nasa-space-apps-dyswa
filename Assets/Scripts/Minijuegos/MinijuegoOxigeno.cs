
using UnityEngine;
using UnityEngine.InputSystem;

public class MinijuegoOxigeno : MonoBehaviour
{
    const int AciertosNecesarios = 3;

    [SerializeField] float velocidadAguja = 0.75f;
    [SerializeField, Range(0.05f, 0.4f)]
    float anchoZonaVerde = 0.20f;

    public bool Abierto { get; private set; }

    float posicionAguja;
    float direccion = 1f;
    int aciertos;
    string mensaje = "Presiona ESPACIO en la zona verde";

    public void Abrir()
    {
        if (Abierto) return;

        Abierto = true;
        posicionAguja = 0f;
        direccion = 1f;
        aciertos = 0;
        mensaje = "Presiona ESPACIO en la zona verde";
    }

    public void Cerrar()
    {
        Abierto = false;
    }

    void OnDisable()
    {
        Cerrar();
    }

    void Update()
    {
        if (!Abierto) return;

        GameManager gm = GameManager.Instancia;

        if (gm == null || !gm.JuegoActivo)
        {
            Cerrar();
            return;
        }

        posicionAguja += direccion *
            velocidadAguja * Time.unscaledDeltaTime;

        if (posicionAguja >= 1f)
        {
            posicionAguja = 1f;
            direccion = -1f;
        }
        else if (posicionAguja <= 0f)
        {
            posicionAguja = 0f;
            direccion = 1f;
        }

        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cerrar();
            return;
        }

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            bool correcto =
                Mathf.Abs(posicionAguja - 0.5f)
                <= anchoZonaVerde / 2f;

            if (correcto)
            {
                aciertos++;
                mensaje = "¡Presion estabilizada!";

                if (aciertos >= AciertosNecesarios)
                {
                    gm.FijarSala(Sala.Soporte, true);
                    Cerrar();
                }
            }
            else
            {
                aciertos = 0;
                mensaje = "¡Fallo! Reinicia la calibracion";
            }
        }
    }

    void OnGUI()
    {
        if (!Abierto) return;

        GUI.depth = -60;

        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            Texture2D.whiteTexture
        );

        float ancho = Mathf.Min(500f, Screen.width - 16f);
        float alto = Mathf.Min(230f, Screen.height - 16f);
        float x = (Screen.width - ancho) / 2f;
        float y = (Screen.height - alto) / 2f;

        GUI.color = Color.white;
        GUI.Box(
            new Rect(x, y, ancho, alto),
            "REPARACION DEL REGULADOR DE OXIGENO"
        );

        float barraX = x + 24f;
        float barraY = y + 68f;
        float barraAncho = ancho - 48f;

        GUI.color = new Color(0.2f, 0.25f, 0.3f);
        GUI.DrawTexture(
            new Rect(barraX, barraY, barraAncho, 30f),
            Texture2D.whiteTexture
        );

        GUI.color = Color.green;
        GUI.DrawTexture(
            new Rect(
                barraX + barraAncho * (0.5f - anchoZonaVerde / 2f),
                barraY,
                barraAncho * anchoZonaVerde,
                30f
            ),
            Texture2D.whiteTexture
        );

        GUI.color = Color.white;
        GUI.DrawTexture(
            new Rect(
                barraX + barraAncho * posicionAguja - 2f,
                barraY - 5f,
                4f,
                40f
            ),
            Texture2D.whiteTexture
        );

        GUI.Label(
            new Rect(x + 24f, y + 108f, ancho - 48f, 26f),
            "Calibraciones: " + aciertos + "/3"
        );

        GUI.Label(
            new Rect(x + 24f, y + 132f, ancho - 48f, 26f),
            mensaje
        );

        if (GUI.Button(
            new Rect(x + ancho - 115f, y + alto - 40f, 90f, 28f),
            "CERRAR"
        ))
        {
            Cerrar();
        }

        GUI.color = Color.white;
    }
}
