using System.Collections.Generic;
using UnityEngine;

public class MazeGoalPlacer : MonoBehaviour
{
    [Tooltip("Prefab del GoalTrigger (asignar el prefab con tu script GoalTrigger).")]
    public GameObject goalPrefab;

    [Tooltip("Transform del jugador local. Si no se asigna intentará buscar el tagged 'Player'.")]
    public Transform playerTransform;

    [Tooltip("Transform que representa el punto de inicio de la generación del maze (opcional).")]
    public Transform mazeStartTransform;

    [Tooltip("Padre que contiene las celdas/posiciones del laberinto (opcional).")]
    public Transform cellsParent;

    [Tooltip("Offset vertical al instanciar el goal (para que quede por encima del suelo).")]
    public float placeYOffset = 0.5f;

    [Tooltip("Si true intenta usar BFS sobre MazeCell; si false fuerza usar distancia euclidiana.")]
    public bool preferPathDistance = true;

    [Tooltip("Si true destruirá cualquier goal existente antes de crear uno nuevo.")]
    public bool destroyExisting = true;

    GameObject currentGoal;

    // Llamar desde el generador del laberinto cuando termine
    public void PlaceGoal()
    {
        if (goalPrefab == null)
        {
            Debug.LogWarning("MazeGoalPlacer: goalPrefab no asignado.");
            return;
        }

        if (playerTransform == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }

        // Primero intenta BFS sobre MazeCell si está disponible y preferido
        if (preferPathDistance)
        {
#if UNITY_2023_1_OR_NEWER
            MazeCell[] cells = UnityEngine.Object.FindObjectsByType<MazeCell>(FindObjectsSortMode.None);
#else
            MazeCell[] cells = Object.FindObjectsOfType<MazeCell>();
#endif
            if (cells != null && cells.Length > 0)
            {
                PlaceUsingBFS(cells);
                return;
            }
        }

        // Fallback: usar transform children de cellsParent o buscar objetos con tag "MazeCell"
        if (cellsParent != null && cellsParent.childCount > 0)
        {
            var list = new List<Transform>();
            for (int i = 0; i < cellsParent.childCount; i++) list.Add(cellsParent.GetChild(i));
            PlaceByEuclidean(list);
            return;
        }

        // Ultimo fallback: buscar por tag "MazeCell"
        var tagged = GameObject.FindGameObjectsWithTag("MazeCell");
        if (tagged != null && tagged.Length > 0)
        {
            var list = new List<Transform>();
            foreach (var go in tagged) list.Add(go.transform);
            PlaceByEuclidean(list);
            return;
        }

        Debug.LogWarning("MazeGoalPlacer: no se encontraron celdas para colocar el goal.");
    }

    void PlaceUsingBFS(MazeCell[] cells)
    {
        // construir mapa de celda -> vecinos (usar lista neighbors que ahora proviene del generador)
        var start = FindClosestCellToPlayer(cells);
        if (start == null)
        {
            Debug.LogWarning("MazeGoalPlacer: start cell no encontrada.");
            return;
        }

        var q = new Queue<MazeCell>();
        var visited = new HashSet<MazeCell>();
        var dist = new Dictionary<MazeCell, int>();

        q.Enqueue(start);
        visited.Add(start);
        dist[start] = 0;

        MazeCell farthest = start;
        int maxD = 0;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int dcur = dist[cur];
            if (dcur > maxD)
            {
                maxD = dcur;
                farthest = cur;
            }

            if (cur.neighbors == null || cur.neighbors.Count == 0) continue;
            foreach (var n in cur.neighbors)
            {
                if (n == null) continue;
                if (visited.Contains(n)) continue;
                visited.Add(n);
                dist[n] = dcur + 1;
                q.Enqueue(n);
            }
        }

        InstantiateGoalAt(farthest.transform.position);
    }

    void PlaceByEuclidean(List<Transform> cells)
    {
        if (cells == null || cells.Count == 0) return;

        // referencia: jugador si está asignado, si no usar mazeStartTransform, si no fallback a la primera celda
        Vector3 refPos;
        if (playerTransform != null) refPos = playerTransform.position;
        else if (mazeStartTransform != null) refPos = mazeStartTransform.position;
        else refPos = cells[0].position;

        Transform far = null;
        float bestDist = float.MinValue;
        foreach (var t in cells)
        {
            if (t == null) continue;
            float d = Vector3.SqrMagnitude(t.position - refPos);
            if (d > bestDist)
            {
                bestDist = d;
                far = t;
            }
        }

        if (far != null) InstantiateGoalAt(far.position);
    }

    MazeCell FindClosestCellToPlayer(MazeCell[] cells)
    {
        if (cells == null || cells.Length == 0) return null;

        // referencia: jugador si está asignado, si no usar mazeStartTransform, si no fallback a la primera celda
        Vector3 refPos;
        if (playerTransform != null) refPos = playerTransform.position;
        else if (mazeStartTransform != null) refPos = mazeStartTransform.position;
        else return cells[0];

        MazeCell best = null;
        float bestD = float.MaxValue;
        foreach (var c in cells)
        {
            float d = Vector3.SqrMagnitude(c.transform.position - refPos);
            if (d < bestD)
            {
                bestD = d;
                best = c;
            }
        }
        return best;
    }

    void InstantiateGoalAt(Vector3 worldPos)
    {
        if (destroyExisting && currentGoal != null) Destroy(currentGoal);

        Vector3 spawnPos = worldPos + Vector3.up * placeYOffset;
        currentGoal = Instantiate(goalPrefab, spawnPos, Quaternion.identity, transform);
    }
    // Devuelve la celda más lejana (en número de pasos) partiendo desde la celda
    // más cercana al jugador; útil para que otros componentes (p. ej. placer de collectibles)
    // reusen la misma métrica sin instanciar nada aquí.
    public MazeCell FindFarthestCellByBFS(MazeCell[] cells)
    {
        if (cells == null || cells.Length == 0) return null;

        var start = FindClosestCellToPlayer(cells);
        if (start == null) return null;

        var q = new Queue<MazeCell>();
        var visited = new HashSet<MazeCell>();
        var dist = new Dictionary<MazeCell, int>();

        q.Enqueue(start);
        visited.Add(start);
        dist[start] = 0;

        MazeCell farthest = start;
        int maxD = 0;

        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            int dcur = dist[cur];
            if (dcur > maxD)
            {
                maxD = dcur;
                farthest = cur;
            }

            if (cur.neighbors == null || cur.neighbors.Count == 0) continue;
            foreach (var n in cur.neighbors)
            {
                if (n == null) continue;
                if (visited.Contains(n)) continue;
                visited.Add(n);
                dist[n] = dcur + 1;
                q.Enqueue(n);
            }
        }

        return farthest;
    }
}