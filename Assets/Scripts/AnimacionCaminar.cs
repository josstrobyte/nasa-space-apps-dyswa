using UnityEngine;

// Ponlo en el Astronauta. Cambia el sprite según si camina o está quieto.
// No necesita Animator.
public class AnimacionCaminar : MonoBehaviour
{
    [Header("Cuadros de caminar (en orden)")]
    [SerializeField] Sprite[] cuadrosCaminar;
    [SerializeField, Min(1f)] float cuadrosPorSegundoCaminar = 8f;

    [Header("Cuadros de estar quieto (opcional)")]
    [SerializeField] Sprite[] cuadrosQuieto;
    [SerializeField, Min(1f)] float cuadrosPorSegundoQuieto = 4f;

    SpriteRenderer sr;
    Rigidbody2D rb;
    float tiempo;
    bool caminaba;

    void Awake()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        if (sr == null || rb == null) return;

        bool camina = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        if (camina != caminaba)
        {
            caminaba = camina;
            tiempo = 0f;
        }

        if (camina)
        {
            Mostrar(cuadrosCaminar, cuadrosPorSegundoCaminar);
        }
        else if (cuadrosQuieto != null && cuadrosQuieto.Length > 0)
        {
            Mostrar(cuadrosQuieto, cuadrosPorSegundoQuieto);
        }
        else if (cuadrosCaminar != null && cuadrosCaminar.Length > 1)
        {
            sr.sprite = cuadrosCaminar[1];
        }
    }

    void Mostrar(Sprite[] cuadros, float porSegundo)
    {
        if (cuadros == null || cuadros.Length == 0) return;
        tiempo += Time.deltaTime * porSegundo;
        int indice = (int)tiempo % cuadros.Length;
        if (cuadros[indice] != null) sr.sprite = cuadros[indice];
    }
}
