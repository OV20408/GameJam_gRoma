using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCVida : MonoBehaviour
{
    [Header("Configuración")]
    public float vidaMaxima = 100f;
    public bool mostrarDebug = true;
    
    [Header("Efectos Visuales")]
    public bool cambiarColorAlGolpear = true;
    public Color colorGolpe = Color.red;
    public float duracionColorGolpe = 0.2f;
    
    [Header("Física del Golpe")]
    public bool aplicarFuerzaAlGolpear = true;
    public float multiplicadorFuerza = 5f;
    
    [Header("Protección contra Spam")]
    public float tiempoEntreGolpes = 0.3f;
    
    private float vidaActual;
    private Renderer objetoRenderer;
    private Color colorOriginal;
    private Rigidbody rb;
    private float tiempoUltimoGolpe;
    
    void Start()
    {
        vidaActual = vidaMaxima;
        objetoRenderer = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        
        if (objetoRenderer != null)
        {
            colorOriginal = objetoRenderer.material.color;
        }
        
        // Verificar que tenga el tag correcto
        if (!gameObject.CompareTag("NPC"))
        {
            Debug.LogWarning($"{gameObject.name} debería tener tag 'NPC'!");
        }
        
        // Añadir Rigidbody si no tiene para física de golpes
        if (rb == null && aplicarFuerzaAlGolpear)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 5f;
        }
    }
    
    public void RecibirDaño(float cantidad, Vector3 puntoImpacto, Vector3 direccion, GameObject objetoGolpeador, float velocidadImpacto)
    {
        // Protección contra múltiples golpes simultáneos
        if (Time.time - tiempoUltimoGolpe < tiempoEntreGolpes)
        {
            return;
        }
        tiempoUltimoGolpe = Time.time;
        
        vidaActual -= cantidad;
        
        if (mostrarDebug)
        {
            Debug.Log($"💔 {gameObject.name} recibió {cantidad:F1} de daño de {objetoGolpeador.name}. Vida: {vidaActual:F1}/{vidaMaxima}");
        }
        
        // Efecto visual de golpe
        if (cambiarColorAlGolpear && objetoRenderer != null)
        {
            StopAllCoroutines(); // Detener efectos anteriores
            StartCoroutine(EfectoColorGolpe());
        }
        
        // Aplicar fuerza física
        if (aplicarFuerzaAlGolpear && rb != null)
        {
            Vector3 fuerzaGolpe = direccion * velocidadImpacto * multiplicadorFuerza;
            rb.AddForce(fuerzaGolpe, ForceMode.Impulse);
        }
        
        // Verificar si murió
        if (vidaActual <= 0)
        {
            Morir(objetoGolpeador);
        }
    }
    
    System.Collections.IEnumerator EfectoColorGolpe()
    {
        if (objetoRenderer == null) yield break;
        
        objetoRenderer.material.color = colorGolpe;
        yield return new WaitForSeconds(duracionColorGolpe);
        objetoRenderer.material.color = colorOriginal;
    }
    
    void Morir(GameObject asesino)
    {
        if (mostrarDebug)
            Debug.Log($"💀 {gameObject.name} ha sido derrotado por {asesino.name}!");
        
        // Aquí puedes añadir efectos de muerte, animaciones, drops, etc.
        
        // Por ahora lo desactivamos después de un breve delay
        Invoke("DesactivarNPC", 1f);
    }
    
    void DesactivarNPC()
    {
        gameObject.SetActive(false);
        // O destruir: Destroy(gameObject);
    }
    
    // Métodos públicos de utilidad
    public void Curar(float cantidad)
    {
        vidaActual = Mathf.Min(vidaActual + cantidad, vidaMaxima);
        
        if (mostrarDebug)
            Debug.Log($"💚 {gameObject.name} curado {cantidad}. Vida: {vidaActual:F1}/{vidaMaxima}");
    }
    
    public float ObtenerVidaActual() => vidaActual;
    public float ObtenerPorcentajeVida() => (vidaActual / vidaMaxima) * 100f;
    public bool EstaVivo() => vidaActual > 0;
    public void ResetearVida() => vidaActual = vidaMaxima;
}
