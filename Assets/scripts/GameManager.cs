using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Referencias Globales")]
    public GameObject playerPrefab;
    [HideInInspector] public GameObject currentPlayer;

    [Header("Sistema de Spawns")]
    public string lastDoorUsed;
    public Dictionary<string, Vector3> spawnPoints = new Dictionary<string, Vector3>();


    void Awake()
    {
        // Singleton: asegura una sola instancia del GameManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Escuchar evento de carga de escenas
        SceneManager.sceneLoaded += OnSceneLoaded;
    }


    void Start()
    {
        // Manejo especial para Meta Quest: si ya existe un Player en la escena (por ejemplo OVRCameraRig), reutilizarlo
        if (currentPlayer == null)
        {
            // Buscar un objeto existente con la tag "Player" (útil para rigs/prefabs de Quest)
            GameObject encontrado = GameObject.FindWithTag("Player");
            if (encontrado != null)
            {
                currentPlayer = encontrado;
                DontDestroyOnLoad(currentPlayer);
            }
            else if (playerPrefab != null)
            {
                // Instanciar el prefab si no se encontró ningún Player en la escena
                currentPlayer = Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
                // Asegurar que tenga la tag Player para futuras búsquedas
                try { currentPlayer.tag = "Player"; } catch { }
                DontDestroyOnLoad(currentPlayer);
            }
        }
    }


    /// <summary>
    /// Registra una posición de spawn asociada a una puerta.
    /// </summary>
    public void RegisterSpawnPoint(string doorID, Vector3 position)
    {
        if (!spawnPoints.ContainsKey(doorID))
            spawnPoints.Add(doorID, position);
    }


    /// <summary>
    /// Guarda la última puerta utilizada para posicionar al jugador en la siguiente escena.
    /// </summary>
    public void SetLastDoor(string doorID)
    {
        lastDoorUsed = doorID;
    }


    /// <summary>
    /// Coloca al jugador en el punto de spawn correcto al cargar una nueva escena.
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (currentPlayer == null) return;

        // Evitar duplicados: después de cargar la escena, si hay otros GameObjects con tag "Player"
        // destruirlos si no son la referencia persistente currentPlayer.
        RemoveDuplicatePlayersInScene();

        if (!string.IsNullOrEmpty(lastDoorUsed) && spawnPoints.ContainsKey(lastDoorUsed))
        {
            currentPlayer.transform.position = spawnPoints[lastDoorUsed];
        }
        else
        {
            // Posición por defecto si no se registró ninguna puerta
            currentPlayer.transform.position = Vector3.zero;
        }
    }

    // Busca objetos con la tag "Player" en la escena y destruye los que no coincidan con currentPlayer
    private void RemoveDuplicatePlayersInScene()
    {
        if (currentPlayer == null) return;

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            if (p == currentPlayer) continue;

            // Asegurarse de no destruir objetos que estén en escenas marcadas para persistir
            // Si el objeto está en la misma escena que el currentPlayer o es una instancia temporal, destruir.
            // Usar Destroy instead of DestroyImmediate to be safe en runtime.
            Destroy(p);
        }
    }


    /// <summary>
    /// Cambia la escena y guarda qué puerta se usó.
    /// </summary>
    public void LoadScene(string sceneName, string doorID)
    {
        SetLastDoor(doorID);
        SceneManager.LoadScene(sceneName);
    }
}
 