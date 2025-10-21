using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorInteraction : MonoBehaviour
{
    [Header("Configuración de Puerta")]
    public string doorID;     
    public string sceneToLoad; 
    public string targetSpawnID; 
    public float interactionDistance = 3f;

    private Transform player;

    // Start is called before the first frame update
    void Start()
    {
        // Priorizar buscar el jugador por tag "Player" (útil para rigs de Quest)
        GameObject encontrado = GameObject.FindWithTag("Player");
        if (encontrado != null)
        {
            player = encontrado.transform;
            if (Debug.isDebugBuild) Debug.Log($"DoorInteraction('{doorID}'): jugador encontrado por tag Player: {encontrado.name}");
        }
        else
        {
            player = GameManager.Instance != null ? GameManager.Instance.currentPlayer?.transform : null;
            if (player != null && Debug.isDebugBuild) Debug.Log($"DoorInteraction('{doorID}'): jugador obtenido desde GameManager: {player.name}");
        }

        // Registrar spawn en GameManager cuando esté disponible para evitar NullReference si
        // este script se ejecuta antes que el GameManager (orden de ejecución en la escena).
        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSpawnPoint(doorID, transform.position);
        }
        else
        {
            // Si GameManager aún no existe, esperar a que se cree y registrar después
            StartCoroutine(RegisterSpawnWhenGameManagerReady());
        }
    }

    private System.Collections.IEnumerator RegisterSpawnWhenGameManagerReady()
    {
        // Esperar hasta que GameManager.Instance sea distinto de null (con un timeout razonable)
        float timeout = 5f; // segundos
        float start = Time.time;
        while (GameManager.Instance == null && Time.time - start < timeout)
        {
            yield return null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterSpawnPoint(doorID, transform.position);
        }
        else
        {
            Debug.LogWarning($"DoorInteraction: GameManager no disponible para registrar spawn '{doorID}' después de {timeout} segundos.");
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Refrescar referencia al jugador si es necesario, priorizando tag "Player"
        if (player == null)
        {
            GameObject encontrado2 = GameObject.FindWithTag("Player");
            if (encontrado2 != null)
            {
                player = encontrado2.transform;
                if (Debug.isDebugBuild) Debug.Log($"DoorInteraction('{doorID}'): jugador actualizado por tag Player en Update: {encontrado2.name}");
            }
            else if (GameManager.Instance != null && GameManager.Instance.currentPlayer != null)
            {
                player = GameManager.Instance.currentPlayer.transform;
                if (Debug.isDebugBuild) Debug.Log($"DoorInteraction('{doorID}'): jugador actualizado desde GameManager en Update: {player.name}");
            }
        }

        if (player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);

        // Soporte para Meta Quest (OVRInput) - usar A, trigger o grip como interacción, fallback a tecla E
        bool ovrInteract = false;
#if OVR
        ovrInteract = OVRInput.GetDown(OVRInput.Button.One) ||
                     OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger) ||
                     OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger);
#endif

        if (distance <= interactionDistance && (ovrInteract || Input.GetKeyDown(KeyCode.E)))
        {
            // Si GameManager está listo, usar su API; si no, encolar la solicitud y esperar
            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadScene(sceneToLoad, targetSpawnID);
            }
            else
            {
                // Evitar múltiples encolados
                StartCoroutine(WaitForGameManagerAndLoad(sceneToLoad, targetSpawnID));
            }
        }
    }

    private System.Collections.IEnumerator WaitForGameManagerAndLoad(string sceneName, string doorID)
    {
        float timeout = 5f;
        float start = Time.time;

        while (GameManager.Instance == null && Time.time - start < timeout)
        {
            yield return null;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadScene(sceneName, doorID);
        }
        else
        {
            // Como última opción, forzar la carga de escena sin GameManager para evitar bloqueo de pantalla.
            Debug.LogWarning($"DoorInteraction: GameManager no disponible para LoadScene('{sceneName}'), usando SceneManager.LoadScene como fallback.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
 