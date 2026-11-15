
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class MinijuegoPaneles : MonoBehaviour
{
    const int Filas = 3;
    const int Columnas = 4;

    [SerializeField] bool empezarConPolvo = true;

    bool[,] polvo = new bool[Filas, Columnas];
    int casillasLimpias;
    float tiempoAnterior;

    public bool Abierto { get; private set; }
    public bool Sucios { get; private set; }

    public event Action LimpiezaCompletada;

    void Awake()
    {
        if (empezarConPolvo)
            Ensuciar();
    }

    public void Ensuciar()
    {
        for (int f = 0; f < Filas; f++)
            for (int c = 0; c < Columnas; c++)
                polvo[f, c] = true;

        casillasLimpias = 0;
        Sucios = true;
    }

    public void Abrir()
    {
        if (!Sucios || Abierto) return;

        Abierto = true;
        tiempoAnterior = Time.timeScale;
        Time.timeScale = 0f;
    }

    public void Cerrar()
    {
        if (!Abierto) return;

        Abierto = false;
        Time.timeScale = tiempoAnterior;
    }

    void OnDisable()
    {
        Cerrar();
    }

    void Update()
    {
        if (Abierto && Keyboard.current != null &&
            Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Cerrar();
        }
    }

    void OnGUI()
    {
        if (!Abierto) return;

        GUI.depth = -50;

        GUI.color = new Color(0f, 0f, 0f, 0.85f);
        GUI.DrawTexture(
            new Rect(0, 0, Screen.width, Screen.height),
            Texture2D.whiteTexture
        );
        GUI.color = Color.white;

        float ancho = Mathf.Min(460f, Screen.width - 24f);
        float alto = Mathf.Min(340f, Screen.height - 24f);
        float x = (Screen.width - ancho) / 2f;
        float y = (Screen.height - alto) / 2f;

        GUI.Box(new Rect(x, y, ancho, alto),
                "LIMPIEZA DE PANELES SOLARES");

        GUI.Label(new Rect(x + 24, y + 35, ancho - 48, 30),
                  "Arrastra el mouse para retirar el polvo");

        float espacio = 8f;
        float celdaAncho =
            (ancho - 48f - 3f * espacio) / Columnas;
        float celdaAlto =
            (alto - 152f - 2f * espacio) / Filas;

        Event evento = Event.current;

        bool limpiando =
            (evento.type == EventType.MouseDown ||
             evento.type == EventType.MouseDrag) &&
            evento.button == 0;

        for (int f = 0; f < Filas; f++)
        {
            for (int c = 0; c < Columnas; c++)
            {
                Rect celda = new Rect(
                    x + 24 + c * (celdaAncho + espacio),
                    y + 76 + f * (celdaAlto + espacio),
                    celdaAncho,
                    celdaAlto
                );

                if (limpiando && polvo[f, c] &&
                    celda.Contains(evento.mousePosition))
                {
                    polvo[f, c] = false;
                    casillasLimpias++;
                    evento.Use();

                    if (casillasLimpias == Filas * Columnas)
                    {
                        Sucios = false;
                        LimpiezaCompletada?.Invoke();
                    }
                }

                GUI.color = polvo[f, c]
                    ? new Color(0.63f, 0.31f, 0.12f)
                    : new Color(0.10f, 0.55f, 0.78f);

                GUI.DrawTexture(celda, Texture2D.whiteTexture);
            }
        }

        GUI.color = Color.white;

        int porcentaje =
            casillasLimpias * 100 / (Filas * Columnas);

        string mensaje = Sucios
            ? "Limpieza: " + porcentaje + "%"
            : "¡PANELES LIMPIOS!";

        GUI.Label(
            new Rect(x + 24, y + alto - 76, ancho - 48, 28),
            mensaje
        );

        if (GUI.Button(
            new Rect(x + ancho - 126, y + alto - 42, 102, 30),
            "CERRAR"))
        {
            Cerrar();
        }
    }
}
