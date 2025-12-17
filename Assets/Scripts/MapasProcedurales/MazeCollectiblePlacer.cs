using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MazeCollectiblePlacer : MonoBehaviour
{
    [Tooltip("Prefab del collectible (si usas Photon debe estar en Resources).")]
    public GameObject collectiblePrefab;

    [Range(0f, 1f)]
    public float collectibleSpawnProbability = 0.02f;
    public int maxCollectibles = 3;
    public float placeYOffset = 0.5f;
    [Tooltip("Distancia mínima (unidades mundo) desde el jugador para permitir spawn de collectibles.")]
    public float minDistanceFromPlayer = 5f;

    void Start()
    {
        if (collectiblePrefab == null) return;

        if (PhotonNetwork.IsConnected)
        {
            // Verificar que el prefab tenga PhotonView si vamos a instanciar por red
            var pv = collectiblePrefab.GetComponent<PhotonView>();
            if (pv == null)
            {
                Debug.LogWarning($"MazeCollectiblePlacer: collectiblePrefab '{collectiblePrefab.name}' no tiene PhotonView. Si usas Photon, añade PhotonView al prefab o la instancia fallará en red.");
            }

            // Verificar si existe en Resources (requisito para PhotonNetwork.InstantiateRoomObject)
            var res = Resources.Load<GameObject>(collectiblePrefab.name);
            if (res == null)
            {
                Debug.LogWarning($"MazeCollectiblePlacer: no se encontró '{collectiblePrefab.name}' en Resources. Si usas Photon room instantiation, coloca el prefab dentro de Assets/Resources.");
            }
        }
    }

    // Coloca collectibles distribuídos por las celdas
    public void PlaceCollectiblesDistributed(MazeCell[] cells, MazeGoalPlacer goalPlacer = null)
    {
        if (collectiblePrefab == null) return;
        if (cells == null || cells.Length == 0) return;

        // Si estamos conectados a Photon, queremos que SOLO el MasterClient cree los objetos de sala.
        // Por eso pasamos "spawnInNetwork = PhotonNetwork.IsConnected" y el método
        // SpawnCollectibleAtCell internamente evita la creación cuando no es MasterClient.
        bool spawnInNetwork = PhotonNetwork.IsConnected;

        // Construir lista candidata: filtrar por distancia euclidiana mínima al jugador si es posible
        Vector3 playerPos = Vector3.zero;
        bool havePlayer = false;
        if (goalPlacer != null && goalPlacer.playerTransform != null)
        {
            playerPos = goalPlacer.playerTransform.position;
            havePlayer = true;
        }

        var pool = new List<MazeCell>();
        float minDistSq = minDistanceFromPlayer * minDistanceFromPlayer;
        foreach (var c in cells)
        {
            if (c == null) continue;
            if (havePlayer)
            {
                if ((c.transform.position - playerPos).sqrMagnitude >= minDistSq)
                    pool.Add(c);
            }
            else
            {
                pool.Add(c);
            }
        }

        // Si el filtrado dejó vacío el pool, usar todas las celdas como fallback
        if (pool.Count == 0)
        {
            foreach (var c in cells) if (c != null) pool.Add(c);
        }

        // Mezclar el pool para no favorecer siempre las primeras celdas
        for (int i = 0; i < pool.Count; i++)
        {
            int j = Random.Range(i, pool.Count);
            var tmp = pool[i]; pool[i] = pool[j]; pool[j] = tmp;
        }

        int spawned = 0;
        foreach (var c in pool)
        {
            if (c == null) continue;
            if (spawned >= maxCollectibles) break;
            if (Random.value <= collectibleSpawnProbability)
            {
                // Debug: indicar intención de spawn y si será por red o local
                Debug.Log($"MazeCollectiblePlacer: intentando spawn en celda {c.transform.position} (network={spawnInNetwork})");
                SpawnCollectibleAtCell(c, spawnInNetwork);
                spawned++;
            }
        }

        if (spawned == 0)
        {
            MazeCell chosen = null;
            if (goalPlacer != null)
            {
                chosen = goalPlacer.FindFarthestCellByBFS(cells);
            }
            if (chosen == null)
            {
                chosen = cells[Random.Range(0, cells.Length)];
            }
            SpawnCollectibleAtCell(chosen, spawnInNetwork);
        }
    }

    // Colocar UN collectible en el punto medio del camino entre playerCell y goalCell
    public void SpawnOneBetweenPlayerAndGoal(MazeCell playerCell, MazeCell goalCell)
    {
        if (collectiblePrefab == null || playerCell == null || goalCell == null) return;

        var q = new Queue<MazeCell>();
        var parent = new Dictionary<MazeCell, MazeCell>();
        var visited = new HashSet<MazeCell>();
        q.Enqueue(playerCell);
        visited.Add(playerCell);
        parent[playerCell] = null;

        bool found = false;
        while (q.Count > 0 && !found)
        {
            var cur = q.Dequeue();
            if (cur == goalCell) { found = true; break; }
            if (cur.neighbors == null) continue;
            foreach (var n in cur.neighbors)
            {
                if (n == null || visited.Contains(n)) continue;
                visited.Add(n);
                parent[n] = cur;
                q.Enqueue(n);
            }
        }

        bool spawnInNetwork = PhotonNetwork.IsConnected;

        if (!found)
        {
            SpawnCollectibleAtCell(goalCell, spawnInNetwork);
            return;
        }

        var path = new List<MazeCell>();
        MazeCell p = goalCell;
        while (p != null)
        {
            path.Add(p);
            parent.TryGetValue(p, out p);
        }
        path.Reverse();

        int midIndex = Mathf.Clamp(path.Count / 2, 0, path.Count - 1);
        var midCell = path[midIndex];
        SpawnCollectibleAtCell(midCell, spawnInNetwork);
    }

    void SpawnCollectibleAtCell(MazeCell cell, bool spawnInNetwork)
    {
        if (cell == null) return;
        Vector3 spawnPos = cell.transform.position + Vector3.up * placeYOffset;

        if (spawnInNetwork)
        {
            if (!PhotonNetwork.IsMasterClient) return;
            PhotonNetwork.InstantiateRoomObject(collectiblePrefab.name, spawnPos, Quaternion.identity);
        }
        else
        {
            Instantiate(collectiblePrefab, spawnPos, Quaternion.identity, transform);
        }
    }
}
