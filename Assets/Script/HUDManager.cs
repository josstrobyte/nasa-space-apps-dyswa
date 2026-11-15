using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Barras de Recursos (0 a 100)")]
    [SerializeField] Slider barraEnergia;
    [SerializeField] Slider barraSoporte;
    [SerializeField] Slider barraComida;
    [SerializeField] Slider barraRadiacion;

    [Header("Textos y Pantallas")]
    [SerializeField] TextMeshProUGUI textoSol;
    [SerializeField] TextMeshProUGUI textoAlerta;
    [SerializeField] GameObject panelInicio;
    [SerializeField] GameObject panelFin;
    [SerializeField] TextMeshProUGUI textoResultadoFin;

    void Start()
    {
        if (GameManager.Instancia == null) return;

        GameManager.Instancia.SolCambiado += ActualizarSol;
        GameManager.Instancia.AlertaEmitida += ActualizarAlerta;
        GameManager.Instancia.PartidaTerminada += MostrarFinPartida;

        ActualizarSol(GameManager.Instancia.SolActual, GameManager.Instancia.TotalSoles);
        ActualizarAlerta(GameManager.Instancia.UltimaAlerta);
        if (panelFin != null) panelFin.SetActive(false);
    }

    void OnDestroy()
    {
        if (GameManager.Instancia == null) return;

        GameManager.Instancia.SolCambiado -= ActualizarSol;
        GameManager.Instancia.AlertaEmitida -= ActualizarAlerta;
        GameManager.Instancia.PartidaTerminada -= MostrarFinPartida;
    }

    void Update()
    {
        if (GameManager.Instancia == null) return;

        if (barraEnergia != null) barraEnergia.value = GameManager.Instancia.Energia;
        if (barraSoporte != null) barraSoporte.value = GameManager.Instancia.SoporteVital;
        if (barraComida != null) barraComida.value = GameManager.Instancia.Comida;
        if (barraRadiacion != null) barraRadiacion.value = GameManager.Instancia.Radiacion;
    }

    void ActualizarSol(int actual, int total)
    {
        if (textoSol != null) textoSol.text = $"Sol {actual} / {total}";
    }

    void ActualizarAlerta(string mensaje)
    {
        if (textoAlerta != null) textoAlerta.text = mensaje;
    }

    void MostrarFinPartida(bool gano, string razon)
    {
        if (panelFin != null) panelFin.SetActive(true);
        if (textoResultadoFin != null)
            textoResultadoFin.text = (gano ? "¡MISIÓN CUMPLIDA!\n" : "¡MISIÓN FALLIDA!\n") + razon;
    }

    public void BotonJugar()
    {
        if (panelInicio != null) panelInicio.SetActive(false);
        Time.timeScale = 1f;
    }

    public void BotonReintentar()
    {
        if (panelFin != null) panelFin.SetActive(false);
        if (GameManager.Instancia != null) GameManager.Instancia.Reiniciar();
    }
}