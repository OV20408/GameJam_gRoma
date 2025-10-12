using UnityEngine;

// Script de movimiento para Meta Quest con Building Blocks
// Añade este script al objeto [BuildingBlocks] Camera Rig
public class MovimientoMetaQuest : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    public float velocidad = 3f;
    public float velocidadRotacion = 60f;
    public bool usarRotacionContinua = false; // false = snap rotation
    
    [Header("Referencias (se asignan automáticamente)")]
    public Transform camaraTransform;
    public OVRInput.Controller controladorMovimiento = OVRInput.Controller.LTouch;
    public OVRInput.Controller controladorRotacion = OVRInput.Controller.RTouch;
    
    private CharacterController characterController;
    
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
        }
        
        // Buscar la cámara automáticamente
        if (camaraTransform == null)
        {
            camaraTransform = Camera.main.transform;
        }
    }
    
    void Update()
    {
        ManejarMovimiento();
        ManejarRotacion();
        AplicarGravedad();
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
                transform.Rotate(0, rotacion, 0);
            }
        }
        else
        {
            // Rotación snap (45 grados)
            if (Mathf.Abs(inputRotacion.x) > 0.7f)
            {
                if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickRight, controladorRotacion))
                {
                    transform.Rotate(0, 45f, 0);
                }
                else if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstickLeft, controladorRotacion))
                {
                    transform.Rotate(0, -45f, 0);
                }
            }
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