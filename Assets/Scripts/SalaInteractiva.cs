using UnityEngine;
using UnityEngine.InputSystem;

// Ponlo en un objeto vacío colocado frente a la consola de cada sala.
// Al acercarse y pulsar E, enciende o apaga el sistema de esa sala.
public class SalaInteractiva : MonoBehaviour
{
    [Header("Qué sala controla")]
    [SerializeField] Sala sala = Sala.Alimentos;

    [Header("Interacción")]
    [SerializeField] Key tecla = Key.E;
    [Tooltip("Qué tan cerca debe estar el astronauta (en unidades).")]
    [SerializeField, Min(0.1f)] float radio = 2f;
    [Tooltip("Altura del letrero sobre este objeto.")]
    [SerializeField] float alturaLetrero = 1.6f;

    [Header("Minijuego de energía")]
[SerializeField] MinijuegoPaneles limpiezaPaneles;

    [Header("Arte (opcional)")]
    [SerializeField] GameObject capaNormal;
    [SerializeField] GameObject capaFalla;
    [SerializeField] GameObject capaAlarma;

    Transform jugador;
    Camera camara;
    GUIStyle estilo;
    bool cerca;

    void Update()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null) return;

        if (jugador == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) jugador = obj.transform;
        }

        cerca = jugador != null &&
                Vector2.Distance(jugador.position, transform.position) <= radio;

        if (cerca && gm.JuegoActivo && Keyboard.current != null &&
            Keyboard.current[tecla].wasPressedThisFrame)
        {
            if (sala == Sala.Energia && limpiezaPaneles != null)
{
    if (!limpiezaPaneles.Abierto)
    {
        if (limpiezaPaneles.Sucios)
            limpiezaPaneles.Abrir();
        else
            gm.AlternarSala(sala);
    }
}
else
{
    gm.AlternarSala(sala);
}
        }

        ActualizarArte(gm);
    }

    void ActualizarArte(GameManager gm)
    {
        bool activa = gm.EstaActiva(sala);
        if (capaNormal != null) capaNormal.SetActive(activa);
        if (capaFalla != null) capaFalla.SetActive(!activa);

        if (capaAlarma != null)
        {
            bool parpadeo = Mathf.Repeat(Time.time * 2f, 2f) < 1f;
            capaAlarma.SetActive(EnPeligro(gm) && parpadeo);
        }
    }

    bool EnPeligro(GameManager gm)
    {
        switch (sala)
        {
            case Sala.Energia: return gm.Energia < 20f;
            case Sala.Soporte: return gm.SoporteVital < 20f;
            case Sala.Alimentos: return gm.Comida < 20f;
            default: return gm.Radiacion > 80f;
        }
    }

    string NombreSistema()
    {
        switch (sala)
        {
            case Sala.Energia: return "el generador";
            case Sala.Soporte: return "el soporte vital";
            case Sala.Alimentos: return "el invernadero";
            default: return "los escudos";
        }
    }

    void OnGUI()
    {
        GameManager gm = GameManager.Instancia;
        if (!cerca || gm == null || !gm.JuegoActivo) return;

        if (camara == null) camara = Camera.main;
        if (camara == null) return;

        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label);
            estilo.alignment = TextAnchor.MiddleCenter;
            estilo.fontStyle = FontStyle.Bold;
        }
        float escala = Screen.height / 720f;
        estilo.fontSize = Mathf.RoundToInt(24f * escala);

        string texto =
    sala == Sala.Energia &&
    limpiezaPaneles != null &&
    limpiezaPaneles.Sucios
    ? "[E] Limpiar paneles"
    : (gm.EstaActiva(sala) ? "[E] Apagar " : "[E] Encender ")
      + NombreSistema();

        Vector3 p = camara.WorldToScreenPoint(transform.position + Vector3.up * alturaLetrero);
        float ancho = 420f * escala;
        float alto = 44f * escala;
        Rect r = new Rect(p.x - ancho / 2f, Screen.height - p.y - alto / 2f, ancho, alto);

        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = gm.EstaActiva(sala) ? new Color(0.5f, 1f, 0.5f) : new Color(1f, 0.8f, 0.3f);
        GUI.Label(r, texto, estilo);
        GUI.color = Color.white;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
