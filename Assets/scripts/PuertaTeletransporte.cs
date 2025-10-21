using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

// Script para puertas que teletransportan
// Añade este script a un objeto que será la puerta
public class PuertaTeletransporte : MonoBehaviour
{
    [Header("Tipo de Teletransporte")]
    public bool cambiarEscena = false; // true = cambiar escena, false = mover en la misma escena
    
    [Header("Configuración de Escena")]
    [Tooltip("Nombre de la escena a cargar (debe estar en Build Settings)")]
    public string nombreEscena;
    
    [Header("Configuración de Posición")]
    [Tooltip("Punto de destino en la misma escena")]
    public Transform puntoDestino;
    
    [Header("Interacción")]
    public bool requiereBoton = true; // true = presionar botón, false = al tocar
    public OVRInput.Button botonInteraccion = OVRInput.Button.PrimaryIndexTrigger;
    
    [Header("Efectos")]
    public bool usarFadeOut = true;
    public float duracionFade = 0.5f;
    
    [Header("Distancia")]
    public float distanciaActivacion = 2f;
    
    private Transform jugador;
    private bool jugadorCerca = false;
    private bool puedeInteractuar = true;
    private Material fadeMaterial;
    private bool fadeActivo = false;
    
    void Start()
    {
        // Buscar al jugador (Camera Rig)
        GameObject cameraRig = GameObject.Find("[BuildingBlocks] Camera Rig");
        if (cameraRig == null)
        {
            cameraRig = GameObject.FindGameObjectWithTag("Player");
        }
        
        if (cameraRig != null)
        {
            jugador = cameraRig.transform;
        }
        
        // Asegurarse de que tenga trigger collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }
    
    void Update()
    {
        if (jugador == null || !puedeInteractuar) return;
        
        // Verificar distancia al jugador
        float distancia = Vector3.Distance(transform.position, jugador.position);
        jugadorCerca = distancia <= distanciaActivacion;
        
        // Detectar interacción
        if (jugadorCerca)
        {
            if (requiereBoton)
            {
                // Requiere presionar botón
                if (OVRInput.GetDown(botonInteraccion))
                {
                    Teletransportar();
                }
            }
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        // Si no requiere botón, teletransporta al entrar
        if (!requiereBoton && puedeInteractuar)
        {
            if (other.CompareTag("Player") || other.transform.root.name.Contains("Camera Rig"))
            {
                Teletransportar();
            }
        }
    }
    
    void Teletransportar()
    {
        if (!puedeInteractuar) return;
        
        puedeInteractuar = false;
        
        if (usarFadeOut)
        {
            StartCoroutine(TeletransportarConFade());
        }
        else
        {
            EjecutarTeletransporte();
        }
    }
    
    IEnumerator TeletransportarConFade()
    {
        // Fade out simple
        float tiempo = 0;
        Camera cam = Camera.main;
        
        // Crear un plano negro frente a la cámara
        GameObject fadeObj = GameObject.CreatePrimitive(PrimitiveType.Quad);
        fadeObj.name = "FadeScreen";
        fadeObj.transform.parent = cam.transform;
        fadeObj.transform.localPosition = new Vector3(0, 0, 0.5f);
        fadeObj.transform.localRotation = Quaternion.identity;
        fadeObj.transform.localScale = new Vector3(2f, 2f, 1f);
        
        Renderer fadeRenderer = fadeObj.GetComponent<Renderer>();
        Material fadeMat = new Material(Shader.Find("Unlit/Color"));
        fadeMat.color = new Color(0, 0, 0, 0);
        fadeRenderer.material = fadeMat;
        fadeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        fadeRenderer.receiveShadows = false;
        
        Destroy(fadeObj.GetComponent<Collider>());
        
        // Fade out
        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            float alpha = Mathf.Lerp(0, 1, tiempo / duracionFade);
            fadeMat.color = new Color(0, 0, 0, alpha);
            yield return null;
        }
        
        // Teletransportar
        EjecutarTeletransporte();
        
        // Fade in (si es en la misma escena)
        if (!cambiarEscena)
        {
            yield return new WaitForSeconds(0.1f);
            
            tiempo = 0;
            while (tiempo < duracionFade)
            {
                tiempo += Time.deltaTime;
                float alpha = Mathf.Lerp(1, 0, tiempo / duracionFade);
                fadeMat.color = new Color(0, 0, 0, alpha);
                yield return null;
            }
            
            Destroy(fadeObj);
            puedeInteractuar = true;
        }
        else
        {
            // La escena se cargará, el objeto se destruirá automáticamente
        }
    }
    
    void EjecutarTeletransporte()
    {
        if (cambiarEscena)
        {
            // Cargar otra escena
            if (!string.IsNullOrEmpty(nombreEscena))
            {
                SceneManager.LoadScene(nombreEscena);
            }
            else
            {
                Debug.LogError("¡Nombre de escena vacío! Configura 'nombreEscena' en el Inspector");
            }
        }
        else
        {
            // Mover a punto de destino en la misma escena
            if (puntoDestino != null && jugador != null)
            {
                CharacterController cc = jugador.GetComponent<CharacterController>();
                if (cc != null)
                {
                    cc.enabled = false;
                    jugador.position = puntoDestino.position;
                    jugador.rotation = puntoDestino.rotation;
                    cc.enabled = true;
                }
                else
                {
                    jugador.position = puntoDestino.position;
                    jugador.rotation = puntoDestino.rotation;
                }
                
                Debug.Log("Teletransportado a: " + puntoDestino.name);
            }
            else
            {
                Debug.LogError("¡Punto de destino no asignado! Arrastra un objeto vacío al campo 'puntoDestino'");
            }
        }
    }
    
    // Visualizar en el editor
    void OnDrawGizmosSelected()
    {
        // Dibujar área de activación
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaActivacion);
        
        // Dibujar línea al punto de destino
        if (!cambiarEscena && puntoDestino != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, puntoDestino.position);
            Gizmos.DrawWireSphere(puntoDestino.position, 0.5f);
        }
    }
}