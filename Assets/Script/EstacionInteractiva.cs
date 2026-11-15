using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class EstacionInteractiva : MonoBehaviour
{
    [SerializeField] Sala salaControlada;
    [SerializeField] GameObject indicadorVisual;
    bool jugadorCerca;

    void Reset()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    void Start()
    {
        if (indicadorVisual != null) indicadorVisual.SetActive(false);
    }

    void Update()
    {
        if (!jugadorCerca || GameManager.Instancia == null) return;

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            GameManager.Instancia.AlternarSala(salaControlada);
        }
    }

    void OnTriggerEnter2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        jugadorCerca = true;
        if (indicadorVisual != null) indicadorVisual.SetActive(true);
    }

    void OnTriggerExit2D(Collider2D otro)
    {
        if (!otro.CompareTag("Player")) return;
        jugadorCerca = false;
        if (indicadorVisual != null) indicadorVisual.SetActive(false);
    }
}