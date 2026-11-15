
using UnityEngine;
using UnityEngine.InputSystem;

public class MinijuegoOxigeno : MonoBehaviour
{
    const int AciertosNecesarios = 3;

    [SerializeField] float velocidadAguja = 0.75f;
    [SerializeField, Range(0.05f, 0.4f)]
    float anchoZonaVerde = 0.20f;

    [Header("Sprites del regulador")]
[SerializeField] Texture2D marcoOxigeno;
[SerializeField] Texture2D barraPresion;
[SerializeField] Texture2D zonaOptima;
[SerializeField] Texture2D agujaPresion;

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
                    gm.CompletarReparacionSoporte();
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

    // Oscurecer el juego detrás del regulador
    GUI.color = new Color(0f, 0f, 0f, 0.85f);
    GUI.DrawTexture(
        new Rect(0, 0, Screen.width, Screen.height),
        Texture2D.whiteTexture
    );

    GUI.color = Color.white;

    // Comprobar que los sprites estén asignados
    if (marcoOxigeno == null || barraPresion == null ||
        zonaOptima == null || agujaPresion == null)
        return;

    // Escala entera para conservar el pixel art
    float escala = Screen.width >= 640 && Screen.height >= 360
        ? 2f : 1f;

    float x = (Screen.width - 320f * escala) / 2f;
    float y = (Screen.height - 148f * escala) / 2f;

    // 1. Marco
    GUI.DrawTexture(
        new Rect(x, y, 320f * escala, 148f * escala),
        marcoOxigeno
    );

    // 2. Barra de presión
    float barraX = x + 19f * escala;
    float barraY = y + 78f * escala;
    float anchoBarra = 288f;

    GUI.DrawTexture(
        new Rect(barraX, barraY,
                 anchoBarra * escala, 20f * escala),
        barraPresion
    );

    // 3. Zona óptima
    float anchoVerde = anchoBarra * anchoZonaVerde;

    GUI.DrawTexture(
        new Rect(
            barraX + (anchoBarra - anchoVerde) * escala / 2f,
            barraY,
            anchoVerde * escala,
            20f * escala
        ),
        zonaOptima
    );

    // 4. Aguja móvil
    float agujaX = barraX +
        posicionAguja * (anchoBarra - 4f) * escala;

    GUI.DrawTexture(
        new Rect(
            agujaX,
            y + 72f * escala,
            4f * escala,
            28f * escala
        ),
        agujaPresion
    );

    // 5. Contador dinámico sobre el número del marco
    GUI.color = new Color(0.09f, 0.14f, 0.21f);
    GUI.DrawTexture(
        new Rect(x + 66f * escala, y + 112f * escala,
                 17f * escala, 9f * escala),
        Texture2D.whiteTexture
    );

    GUI.color = Color.white;

    GUIStyle estiloContador = new GUIStyle(GUI.skin.label);
    estiloContador.fontSize = Mathf.RoundToInt(7f * escala);
    estiloContador.fontStyle = FontStyle.Bold;
    estiloContador.normal.textColor = Color.white;

    GUI.Label(
        new Rect(x + 66f * escala, y + 107f * escala,
                 34f * escala, 19f * escala),
        aciertos + "/3",
        estiloContador
    );

    // Mensaje de calibración debajo del regulador
    GUI.Label(
        new Rect(x, y + 148f * escala + 8f,
                 320f * escala, 25f),
        mensaje + "  |  ESC para cerrar"
    );

    GUI.color = Color.white;
}
}
