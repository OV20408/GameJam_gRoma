using UnityEngine;

// Sistema de agarre físico con golpeo integrado para Meta Quest
public class AgarreFisicoQuest : MonoBehaviour
{
    [Header("Configuración")]
    public OVRInput.Controller controlador = OVRInput.Controller.RTouch;
    public float distanciaAgarre = 0.3f;
    
    [Header("Física del Agarre")]
    [Tooltip("Suavidad del seguimiento (0-1, menor = más balanceo)")]
    [Range(0.01f, 1f)]
    public float suavidadSeguimiento = 0.2f;
    [Tooltip("Suavidad de la rotación (0-1)")]
    [Range(0.01f, 1f)]
    public float suavidadRotacion = 0.3f;
    
    [Header("Lanzamiento")]
    public bool permitirLanzar = true;
    public float fuerzaLanzamiento = 1.5f;
    public float fuerzaMaximaLanzamiento = 8f;
    
    [Header("Sistema de Golpeo")]
    public bool activarGolpeo = true;
    public float dañoBase = 10f;
    public float multiplicadorVelocidad = 1f;
    [Tooltip("Velocidad mínima para hacer daño (m/s)")]
    public float velocidadMinimaGolpe = 1f;
    public float intensidadVibracionGolpe = 0.7f;
    
    [Header("Debug")]
    public bool mostrarDebug = false;
    
    private GameObject objetoAgarrado;
    private Rigidbody rbObjetoAgarrado;
    private Collider colliderObjetoAgarrado;
    private Vector3 offsetAgarre;
    private Quaternion offsetRotacion;
    
    // Para calcular velocidad al lanzar
    private Vector3[] posicionesAnteriores = new Vector3[5];
    private int indicePosicion = 0;
    private float tiempoUltimaPosicion;
    
    // Para sistema de golpeo
    private Vector3 velocidadObjeto;
    private Vector3 posicionAnteriorObjeto;
    
    // Para controlar el estado de suelta
    private bool estaSoltando = false;
    
    void Start()
    {
        Collider col = GetComponent<Collider>();
        if (col == null && activarGolpeo)
        {
            Debug.LogWarning($"{gameObject.name}: Se recomienda añadir un Collider para detectar golpes");
        }
    }
    
    void Update()
    {
        // Si está soltando, no procesar inputs
        if (estaSoltando) return;
        
        bool grip = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controlador);
        bool trigger = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controlador);
        bool botonAgarrar = grip || trigger;
        
        if (botonAgarrar && objetoAgarrado == null)
        {
            IntentarAgarrar();
        }
        else if (!botonAgarrar && objetoAgarrado != null)
        {
            Soltar();
        }
        
        if (objetoAgarrado != null)
        {
            ActualizarPosicionObjeto();
            GuardarPosicionParaVelocidad();
            CalcularVelocidadObjetoAgarrado();
        }
    }
    
    void IntentarAgarrar()
    {
        Collider[] objetosCercanos = Physics.OverlapSphere(transform.position, distanciaAgarre);
        
        if (mostrarDebug && objetosCercanos.Length > 0)
        {
            Debug.Log($"Objetos cercanos encontrados: {objetosCercanos.Length}");
        }
        
        foreach (Collider col in objetosCercanos)
        {
            if (col.CompareTag("Agarrable"))
            {
                objetoAgarrado = col.gameObject;
                rbObjetoAgarrado = objetoAgarrado.GetComponent<Rigidbody>();
                colliderObjetoAgarrado = col;
                
                if (rbObjetoAgarrado != null)
                {
                    // Guardar el estado original del collider ANTES de hacer cualquier cambio
                    bool estadoOriginalIsTrigger = colliderObjetoAgarrado.isTrigger;
                    
                    // Hacer kinematic para control total
                    rbObjetoAgarrado.isKinematic = true;
                    rbObjetoAgarrado.useGravity = false;
                    
                    // Calcular offset desde la mano al objeto
                    offsetAgarre = transform.InverseTransformPoint(objetoAgarrado.transform.position);
                    offsetRotacion = Quaternion.Inverse(transform.rotation) * objetoAgarrado.transform.rotation;
                    
                    // Inicializar posiciones para velocidad
                    for (int i = 0; i < posicionesAnteriores.Length; i++)
                    {
                        posicionesAnteriores[i] = transform.position;
                    }
                    tiempoUltimaPosicion = Time.time;
                    posicionAnteriorObjeto = objetoAgarrado.transform.position;
                    
                    // Añadir componente detector de golpes al objeto
                    if (activarGolpeo)
                    {
                        // Primero eliminar cualquier detector existente
                        DetectorGolpes detectorExistente = objetoAgarrado.GetComponent<DetectorGolpes>();
                        if (detectorExistente != null)
                        {
                            Destroy(detectorExistente);
                        }
                        
                        DetectorGolpes detector = objetoAgarrado.AddComponent<DetectorGolpes>();
                        detector.InicializarDetector(this, estadoOriginalIsTrigger);
                    }
                    
                    // Vibración al agarrar
                    OVRInput.SetControllerVibration(0.3f, 0.5f, controlador);
                    
                    if (mostrarDebug)
                        Debug.Log($"✓ Agarrado: {objetoAgarrado.name} | isTrigger original: {estadoOriginalIsTrigger}");
                }
                break;
            }
        }
    }
    
    void ActualizarPosicionObjeto()
    {
        if (objetoAgarrado == null) return;
        
        Vector3 posicionObjetivo = transform.TransformPoint(offsetAgarre);
        Quaternion rotacionObjetivo = transform.rotation * offsetRotacion;
        
        objetoAgarrado.transform.position = Vector3.Lerp(
            objetoAgarrado.transform.position,
            posicionObjetivo,
            suavidadSeguimiento
        );
        
        objetoAgarrado.transform.rotation = Quaternion.Slerp(
            objetoAgarrado.transform.rotation,
            rotacionObjetivo,
            suavidadRotacion
        );
        
        if (mostrarDebug)
        {
            Debug.DrawLine(transform.position, objetoAgarrado.transform.position, Color.green);
        }
    }
    
    void CalcularVelocidadObjetoAgarrado()
    {
        velocidadObjeto = (objetoAgarrado.transform.position - posicionAnteriorObjeto) / Time.deltaTime;
        posicionAnteriorObjeto = objetoAgarrado.transform.position;
    }
    
    void GuardarPosicionParaVelocidad()
    {
        if (Time.time - tiempoUltimaPosicion > 0.02f)
        {
            indicePosicion = (indicePosicion + 1) % posicionesAnteriores.Length;
            posicionesAnteriores[indicePosicion] = transform.position;
            tiempoUltimaPosicion = Time.time;
        }
    }
    
    Vector3 CalcularVelocidadPromedio()
    {
        Vector3 velocidadTotal = Vector3.zero;
        int muestras = 0;
        
        for (int i = 0; i < posicionesAnteriores.Length - 1; i++)
        {
            int indiceActual = (indicePosicion - i + posicionesAnteriores.Length) % posicionesAnteriores.Length;
            int indiceSiguiente = (indiceActual - 1 + posicionesAnteriores.Length) % posicionesAnteriores.Length;
            
            Vector3 velocidad = (posicionesAnteriores[indiceActual] - posicionesAnteriores[indiceSiguiente]) / 0.02f;
            velocidadTotal += velocidad;
            muestras++;
        }
        
        return muestras > 0 ? velocidadTotal / muestras : Vector3.zero;
    }
    
    public void NotificarGolpe(GameObject npc, Vector3 puntoImpacto)
    {
        if (!activarGolpeo || objetoAgarrado == null) return;
        ProcesarGolpe(npc, puntoImpacto);
    }
    
    public Vector3 ObtenerVelocidadObjeto()
    {
        return velocidadObjeto;
    }
    
    void ProcesarGolpe(GameObject npc, Vector3 puntoImpacto)
    {
        float velocidadImpacto = velocidadObjeto.magnitude;
        
        if (velocidadImpacto < velocidadMinimaGolpe)
        {
            if (mostrarDebug)
                Debug.Log($"Golpe muy débil: {velocidadImpacto:F2} m/s (mínimo: {velocidadMinimaGolpe})");
            return;
        }
        
        float dañoTotal = dañoBase + (velocidadImpacto * multiplicadorVelocidad);
        
        NPCVida npcVida = npc.GetComponent<NPCVida>();
        if (npcVida != null)
        {
            Vector3 direccion = (npc.transform.position - objetoAgarrado.transform.position).normalized;
            
            npcVida.RecibirDaño(dañoTotal, puntoImpacto, direccion, objetoAgarrado, velocidadImpacto);
            
            OVRInput.SetControllerVibration(intensidadVibracionGolpe, intensidadVibracionGolpe, controlador);
            
            if (mostrarDebug)
                Debug.Log($"💥 Golpe con {objetoAgarrado.name}! Daño: {dañoTotal:F1} | Velocidad: {velocidadImpacto:F2} m/s");
        }
        else if (mostrarDebug)
        {
            Debug.LogWarning($"El NPC {npc.name} no tiene el script NPCVida!");
        }
    }
    
    void Soltar()
    {
        if (objetoAgarrado != null && !estaSoltando)
        {
            estaSoltando = true;
            EjecutarSueltaCompleta();
        }
    }
    
    private void EjecutarSueltaCompleta()
    {
        // 1. Primero desactivar y eliminar el detector de golpes
        if (activarGolpeo)
        {
            DetectorGolpes detector = objetoAgarrado.GetComponent<DetectorGolpes>();
            if (detector != null)
            {
                detector.DesactivarYDestruir();
            }
        }
        
        // 2. Restaurar la física del objeto
        if (rbObjetoAgarrado != null)
        {
            // Asegurarse de que el collider NO sea trigger
            if (colliderObjetoAgarrado != null)
            {
                colliderObjetoAgarrado.isTrigger = false;
            }
            
            // Restaurar propiedades físicas
            rbObjetoAgarrado.isKinematic = false;
            rbObjetoAgarrado.useGravity = true;
            
            // Aplicar lanzamiento si está habilitado
            if (permitirLanzar)
            {
                Vector3 velocidadController = OVRInput.GetLocalControllerVelocity(controlador);
                Vector3 velocidadAngular = OVRInput.GetLocalControllerAngularVelocity(controlador);
                
                Vector3 velocidadFinal = velocidadController.magnitude > 0.5f ? 
                    velocidadController : CalcularVelocidadPromedio();
                
                velocidadFinal *= fuerzaLanzamiento;
                
                if (velocidadFinal.magnitude > fuerzaMaximaLanzamiento)
                {
                    velocidadFinal = velocidadFinal.normalized * fuerzaMaximaLanzamiento;
                }
                
                rbObjetoAgarrado.velocity = velocidadFinal;
                rbObjetoAgarrado.angularVelocity = velocidadAngular;
            }
            
            // Vibración al soltar
            OVRInput.SetControllerVibration(0.2f, 0.3f, controlador);
            
            if (mostrarDebug)
                Debug.Log($"✓ Objeto soltado: {objetoAgarrado.name}");
        }
        
        // 3. Limpiar referencias
        LimpiarReferencias();
        estaSoltando = false;
    }
    
    private void LimpiarReferencias()
    {
        objetoAgarrado = null;
        rbObjetoAgarrado = null;
        colliderObjetoAgarrado = null;
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaAgarre);
        
        if (objetoAgarrado != null && Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, objetoAgarrado.transform.position);
            
            Vector3 posicionObjetivo = transform.TransformPoint(offsetAgarre);
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(posicionObjetivo, 0.05f);
            
            if (activarGolpeo)
            {
                Gizmos.color = velocidadObjeto.magnitude > velocidadMinimaGolpe ? Color.red : Color.yellow;
                Gizmos.DrawRay(objetoAgarrado.transform.position, velocidadObjeto.normalized * 0.5f);
            }
        }
    }
}

// ====================================================================
// Componente auxiliar para detectar golpes - VERSIÓN DEFINITIVA
// ====================================================================

public class DetectorGolpes : MonoBehaviour
{
    private AgarreFisicoQuest sistemaAgarre;
    private bool activo = false;
    private bool estadoOriginalIsTrigger;
    private Collider colliderPrincipal;
    
    public void InicializarDetector(AgarreFisicoQuest agarre, bool isTriggerOriginal)
    {
        sistemaAgarre = agarre;
        activo = true;
        colliderPrincipal = GetComponent<Collider>();
        estadoOriginalIsTrigger = isTriggerOriginal;
        
        // Hacer el collider trigger para detectar colisiones
        if (colliderPrincipal != null)
        {
            colliderPrincipal.isTrigger = true;
        }
        
        if (sistemaAgarre.mostrarDebug)
            Debug.Log($"DetectorGolpes activado en {gameObject.name}, isTrigger: {colliderPrincipal.isTrigger}");
    }
    
    public void DesactivarYDestruir()
    {
        activo = false;
        RestaurarCollider();
        
        // Destruir inmediatamente sin usar Invoke
        DestroyImmediate(this, true);
    }
    
    private void RestaurarCollider()
    {
        if (colliderPrincipal != null)
        {
            colliderPrincipal.isTrigger = estadoOriginalIsTrigger;
            
            if (sistemaAgarre != null && sistemaAgarre.mostrarDebug)
                Debug.Log($"Collider restaurado en {gameObject.name}, isTrigger: {colliderPrincipal.isTrigger}");
        }
    }
    
    void OnTriggerEnter(Collider other)
    {
        if (!activo || sistemaAgarre == null) return;
        
        if (other.CompareTag("NPC") || other.gameObject.layer == LayerMask.NameToLayer("NPC"))
        {
            Vector3 puntoImpacto = other.ClosestPoint(transform.position);
            sistemaAgarre.NotificarGolpe(other.gameObject, puntoImpacto);
        }
    }
    
    void OnDestroy()
    {
        // Asegurarse de restaurar el collider al destruir
        if (activo)
        {
            RestaurarCollider();
        }
    }
}