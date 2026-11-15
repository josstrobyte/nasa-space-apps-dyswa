using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SalaInteractiva : MonoBehaviour
{
    [Header("Qué sala controla")]
    [SerializeField] Sala sala = Sala.Alimentos;

    [Header("Interacción")]
    [SerializeField] Key tecla = Key.E;
    [Tooltip("La E funciona en toda la sala: esta es la mitad de su ancho (en unidades).")]
    [SerializeField, Min(0.5f)] float mitadAnchoSala = 5.2f;
    [Tooltip("Altura del letrero sobre el astronauta.")]
    [SerializeField] float alturaLetrero = 1.6f;

[Header("Minijuego de energía")]
[SerializeField] MinijuegoPaneles limpiezaPaneles;

[Header("Minijuego de soporte vital")]
[SerializeField] MinijuegoOxigeno reparacionOxigeno;

    [Header("Arte (opcional)")]
    [SerializeField] GameObject capaNormal;
    [SerializeField] GameObject capaFalla;
    [SerializeField] GameObject capaAlarma;

    public static readonly List<SalaInteractiva> Todas = new List<SalaInteractiva>();
    public Sala SalaControlada => sala;

    void OnEnable() { if (!Todas.Contains(this)) Todas.Add(this); }
    void OnDisable() { Todas.Remove(this); }

    Transform jugador;
    Camera camara;
    GUIStyle estilo;
    bool cerca;
    bool avisoJugador;

    void Start()
    {
        // La consola se coloca sola en el centro de su sala, usando el arte como referencia.
        Transform referencia = null;
        if (capaNormal != null) referencia = capaNormal.transform;
        else if (capaFalla != null) referencia = capaFalla.transform;
        else if (transform.parent != null)
        {
            foreach (Transform hijo in transform.parent)
            {
                if (hijo != transform && hijo.name.Contains("FONDO_ESTATICO"))
                {
                    referencia = hijo;
                    break;
                }
            }
        }

        if (referencia != null)
        {
            Vector3 centro = referencia.position;
            centro.z = transform.position.z;
            transform.position = centro;
        }
        else
        {
            Debug.LogWarning("La consola '" + name + "' no encontró el arte de su sala. " +
                             "Arrastra la capa normal de la sala al campo Capa Normal.");
        }
    }

    void Update()
    {
        GameManager gm = GameManager.Instancia;
        if (gm == null) return;

        if (jugador == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("Player");
            if (obj != null) jugador = obj.transform;
            else if (!avisoJugador)
            {
                avisoJugador = true;
                Debug.LogWarning("SalaInteractiva no encuentra al astronauta: el objeto Astronauta debe tener la etiqueta (Tag) Player.");
            }
        }

        cerca = false;
        if (jugador != null)
        {
            float dx = Mathf.Abs(jugador.position.x - transform.position.x);
            float dy = Mathf.Abs(jugador.position.y - transform.position.y);
            cerca = dx <= mitadAnchoSala && dy <= 4f;
        }

        if (cerca && gm.JuegoActivo && Keyboard.current != null &&
            Keyboard.current[tecla].wasPressedThisFrame)
        {
if (sala == Sala.Energia &&
    limpiezaPaneles != null &&
    limpiezaPaneles.Sucios)
{
    if (!limpiezaPaneles.Abierto)
        limpiezaPaneles.Abrir();
}
else if (sala == Sala.Soporte &&
         reparacionOxigeno != null &&
         !gm.EstaActiva(Sala.Soporte))
{
    if (!reparacionOxigeno.Abierto)
        reparacionOxigeno.Abrir();
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

if (sala == Sala.Energia &&
    limpiezaPaneles != null &&
    limpiezaPaneles.Abierto)
    return;

string texto =
    sala == Sala.Energia &&
    limpiezaPaneles != null &&
    limpiezaPaneles.Sucios
    ? "[E] Limpiar paneles"
    : sala == Sala.Soporte &&
      reparacionOxigeno != null &&
      !gm.EstaActiva(Sala.Soporte)
      ? "[E] Reparar regulador"
      : (gm.EstaActiva(sala) ? "[E] Apagar " : "[E] Encender ")
        + NombreSistema();

        Vector3 basePos = jugador != null ? jugador.position : transform.position;
        Vector3 p = camara.WorldToScreenPoint(basePos + Vector3.up * alturaLetrero);
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
        Gizmos.DrawWireCube(transform.position, new Vector3(mitadAnchoSala * 2f, 8f, 0f));
    }
}