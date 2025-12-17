using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MazeGenerator : MonoBehaviour
{
    [SerializeField]
    private MazeCell _mazeCellPrefab;

    [SerializeField]
    private int _mazeWitdth;

    [SerializeField]
    private int _mazeDepth;

    [SerializeField]
    private int _seed;

    [SerializeField]
    private bool _useSeed;

    private MazeCell[,] _mazeGrid;

    void Start()
    {
        if (_useSeed)
        {
            Random.InitState(_seed);
        }
        else
        {
            int randomSeed = Random.Range(1, 1000000);
            Random.InitState(randomSeed);

            Debug.Log(randomSeed);
        }
        _mazeGrid = new MazeCell[_mazeWitdth, _mazeDepth];

        for (int x = 0; x < _mazeWitdth; x++)
        {
            for (int z = 0; z < _mazeDepth; z++)
            {
                _mazeGrid[x, z] = Instantiate(_mazeCellPrefab, new Vector3(x, 0, z), Quaternion.identity, transform);
                _mazeGrid[x, z].transform.localPosition = new Vector3(x, 0, z);
            }
        }

        // Start generation from (0,0)
        if (_mazeWitdth > 0 && _mazeDepth > 0)
        {
            GenerateMaze(null, _mazeGrid[0, 0]);

            // --- CAMBIO: colocar el goal AUTOMÁTICAMENTE al terminar la generación ---
            var placer = Object.FindAnyObjectByType<MazeGoalPlacer>();
            if (placer != null)
            {
                placer.PlaceGoal();
            }
            else
            {
                Debug.LogWarning("MazeGenerator: no MazeGoalPlacer encontrado en la escena. El goal no fue colocado.");
            }

            // Intentar colocar collectibles usando MazeCollectiblePlacer si existe
            var collectiblePlacer = Object.FindAnyObjectByType<MazeCollectiblePlacer>();
            if (collectiblePlacer != null)
            {
                // construir array plano de celdas desde la grilla
                var list = new List<MazeCell>();
                for (int x = 0; x < _mazeWitdth; x++)
                {
                    for (int z = 0; z < _mazeDepth; z++)
                    {
                        var c = _mazeGrid[x, z];
                        if (c != null) list.Add(c);
                    }
                }
                collectiblePlacer.PlaceCollectiblesDistributed(list.ToArray(), placer);
            }
            else
            {
                Debug.Log("MazeGenerator: no MazeCollectiblePlacer encontrado; no se colocaron collectibles automáticamente.");
            }
        }
    }

    private void GenerateMaze(MazeCell previousCell, MazeCell currentCell)
    {
        if (currentCell == null) return;

        currentCell.Visit();
        if (previousCell != null)
        {
            ClearWalls(previousCell, currentCell);
            // registrar vecinos: ClearWalls ahora también añade vecinos
        }

        // Continue exploring until there are no unvisited neighbors
        var neighbors = GetUnvisitedCells(currentCell).ToList();
        while (neighbors.Count > 0)
        {
            var next = neighbors[Random.Range(0, neighbors.Count)];
            GenerateMaze(currentCell, next);
            neighbors = GetUnvisitedCells(currentCell).ToList();
        }
    }
    private MazeCell GetNextUnvisitedCell(MazeCell currentCell)
    {
        var unvisitedCells = GetUnvisitedCells(currentCell).ToList();
        if (unvisitedCells.Count == 0) return null;
        return unvisitedCells[Random.Range(0, unvisitedCells.Count)];
    }

    private IEnumerable<MazeCell> GetUnvisitedCells(MazeCell currentCell)
    {
        int x = (int)currentCell.transform.localPosition.x;
        int z = (int)currentCell.transform.localPosition.z;
        var list = new List<MazeCell>();

        if (x + 1 < _mazeWitdth)
        {
            var cellToRight = _mazeGrid[x + 1, z];
            if (cellToRight != null && cellToRight.IsVisited == false)
            {
                list.Add(cellToRight);
            }
        }
        if (x - 1 >= 0)
        {
            var cellToLeft = _mazeGrid[x - 1, z];
            if (cellToLeft != null && cellToLeft.IsVisited == false)
            {
                list.Add(cellToLeft);
            }
        }
        if (z + 1 < _mazeDepth)
        {
            var cellToFront = _mazeGrid[x, z + 1];
            if (cellToFront != null && cellToFront.IsVisited == false)
            {
                list.Add(cellToFront);
            }
        }
        if (z - 1 >= 0)
        {
            var cellToBack = _mazeGrid[x, z - 1];
            if (cellToBack != null && cellToBack.IsVisited == false)
            {
                list.Add(cellToBack);
            }
        }

        return list;
    }


    public void ClearWalls(MazeCell previousCell, MazeCell currentCell)
    {
        if (previousCell == null)
        {
            return;
        }

        if (previousCell.transform.localPosition.x < currentCell.transform.localPosition.x)
        {
            previousCell.ClearRightWall();
            currentCell.ClearLeftWall();
            // registrar vecinos
            previousCell.AddNeighbor(currentCell);
            currentCell.AddNeighbor(previousCell);
            return;
        }

        if (previousCell.transform.localPosition.x > currentCell.transform.localPosition.x)
        {
            previousCell.ClearLeftWall();
            currentCell.ClearRightWall();
            previousCell.AddNeighbor(currentCell);
            currentCell.AddNeighbor(previousCell);
            return;
        }

        if (previousCell.transform.localPosition.z < currentCell.transform.localPosition.z)
        {
            previousCell.ClearFrontWall();
            currentCell.ClearBackWall();
            previousCell.AddNeighbor(currentCell);
            currentCell.AddNeighbor(previousCell);
            return;
        }

        if (previousCell.transform.localPosition.z > currentCell.transform.localPosition.z)
        {
            previousCell.ClearBackWall();
            currentCell.ClearFrontWall();
            previousCell.AddNeighbor(currentCell);
            currentCell.AddNeighbor(previousCell);
            return;
        }
        
    }
}
