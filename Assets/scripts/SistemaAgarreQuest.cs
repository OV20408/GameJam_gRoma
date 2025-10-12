using UnityEngine;

// Script para agarrar objetos con Meta Quest
// Añade este script a cada controlador (mano)
public class SistemaAgarreQuest : MonoBehaviour
{
    [Header("Configuración")]
    public OVRInput.Controller controlador = OVRInput.Controller.RTouch;
    public float distanciaAgarre = 0.3f;
    public LayerMask capasAgarrables;
    
    private GameObject objetoAgarrado;
    private Rigidbody rbObjetoAgarrado;
    
    void Update()
    {
        // Detectar cuando se presiona el gatillo o grip
        bool botonAgarrar = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, controlador) || 
                            OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, controlador);
        
        if (botonAgarrar && objetoAgarrado == null)
        {
            IntentarAgarrar();
        }
        else if (!botonAgarrar && objetoAgarrado != null)
        {
            Soltar();
        }
        
        // Si hay un objeto agarrado, seguir la mano
        if (objetoAgarrado != null)
        {
            objetoAgarrado.transform.position = transform.position;
            objetoAgarrado.transform.rotation = transform.rotation;
        }
    }
    
    void IntentarAgarrar()
    {
        // Buscar objetos cercanos con tag "Agarrable"
        Collider[] objetosCercanos = Physics.OverlapSphere(transform.position, distanciaAgarre);
        
        foreach (Collider col in objetosCercanos)
        {
            if (col.CompareTag("Agarrable"))
            {
                objetoAgarrado = col.gameObject;
                rbObjetoAgarrado = objetoAgarrado.GetComponent<Rigidbody>();
                
                if (rbObjetoAgarrado != null)
                {
                    rbObjetoAgarrado.isKinematic = true;
                    rbObjetoAgarrado.useGravity = false;
                }
                
                break;
            }
        }
    }
    
    void Soltar()
    {
        if (rbObjetoAgarrado != null)
        {
            rbObjetoAgarrado.isKinematic = false;
            rbObjetoAgarrado.useGravity = true;
            
            // Opcional: dar velocidad al soltar
            rbObjetoAgarrado.velocity = OVRInput.GetLocalControllerVelocity(controlador);
            rbObjetoAgarrado.angularVelocity = OVRInput.GetLocalControllerAngularVelocity(controlador);
        }
        
        objetoAgarrado = null;
        rbObjetoAgarrado = null;
    }
    
    // Visualizar el rango de agarre en el editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaAgarre);
    }
}