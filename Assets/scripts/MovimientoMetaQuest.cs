using UnityEngine;

// Script de movimiento para Meta Quest con Building Blocks
// Añade este script al objeto [BuildingBlocks] Camera Rig
public class MovimientoMetaQuest : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidad = 3f;
    public float velocidadRotacion = 60f;
    public bool usarRotacionContinua = false; // false = snap rotation
    
    [Header("Referencias")]
    public Transform camaraTransform;
    public Transform trackingSpace; // El TrackingSpace
    public OVRInput.Controller controladorMovimiento = OVRInput.Controller.LTouch;
    public OVRInput.Controller controladorRotacion = OVRInput.Controller.RTouch;
    
    private CharacterController characterController;
    private float rotacionSnap = 0f;
    private bool puedeRotarSnap = true;
    
    void Start()
    {
        // Obtener o añadir Character Controller
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.3f;
            characterController.center = new Vector3(0, 0.9f, 0);
            characterController.skinWidth = 0.01f;
        }
        
        // Buscar la cámara y tracking space automáticamente si no están asignados
        if (camaraTransform == null)
        {
            camaraTransform = Camera.main.transform;
        }
        
        if (trackingSpace == null)
        {
            trackingSpace = transform.Find("TrackingSpace");
        }
    }
    
    void Update()
    {
        SincronizarCameraRig();
        ManejarMovimiento();
        ManejarRotacion();
        AplicarGravedad();
    }
    
    void SincronizarCameraRig()
    {
        // Calcular el offset de la cámara en el plano XZ
        if (trackingSpace != null && camaraTransform != null)
        {
            Vector3 offsetCamara = camaraTransform.localPosition;
            offsetCamara.y = 0; // Solo nos interesa el offset horizontal
            
            // Ajustar el centro del character controller para que coincida con la cámara
            characterController.center = new Vector3(offsetCamara.x, characterController.height / 2f, offsetCamara.z);
        }
    }
    
    void ManejarMovimiento()
    {
        // Obtener input del joystick izquierdo
        Vector2 inputJoystick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controladorMovimiento);
        
        if (inputJoystick.magnitude > 0.1f)
        {
            // Calcular dirección basada en donde mira la cámara
            Vector3 direccionAdelante = camaraTransform.forward;
            Vector3 direccionDerecha = camaraTransform.right;
            
            // Mantener movimiento en plano horizontal
            direccionAdelante.y = 0;
            direccionDerecha.y = 0;
            direccionAdelante.Normalize();
            direccionDerecha.Normalize();
            
            // Combinar direcciones según input
            Vector3 direccionMovimiento = (direccionAdelante * inputJoystick.y) + (direccionDerecha * inputJoystick.x);
            
            // Aplicar movimiento
            characterController.Move(direccionMovimiento * velocidad * Time.deltaTime);
        }
    }
    
    void ManejarRotacion()
    {
        // Obtener input del joystick derecho
        Vector2 inputRotacion = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, controladorRotacion);
        
        if (usarRotacionContinua)
        {
            // Rotación continua suave
            if (Mathf.Abs(inputRotacion.x) > 0.1f)
            {
                float rotacion = inputRotacion.x * velocidadRotacion * Time.deltaTime;
                RotarRig(rotacion);
            }
        }
        else
        {
            // Rotación snap (45 grados)
            if (Mathf.Abs(inputRotacion.x) > 0.7f && puedeRotarSnap)
            {
                puedeRotarSnap = false;
                float angulo = inputRotacion.x > 0 ? 45f : -45f;
                RotarRig(angulo);
            }
            else if (Mathf.Abs(inputRotacion.x) < 0.3f)
            {
                puedeRotarSnap = true;
            }
        }
    }
    
    void RotarRig(float angulo)
    {
        // Rotar alrededor de la posición de la cámara, no del centro del rig
        if (camaraTransform != null)
        {
            // Guardar posición de la cámara en el mundo
            Vector3 posicionCamara = camaraTransform.position;
            
            // Rotar el rig
            transform.RotateAround(posicionCamara, Vector3.up, angulo);
        }
        else
        {
            transform.Rotate(0, angulo, 0);
        }
    }
    
    void AplicarGravedad()
    {
        // Aplicar gravedad simple
        if (!characterController.isGrounded)
        {
            characterController.Move(Vector3.down * 9.81f * Time.deltaTime);
        }
    }
}