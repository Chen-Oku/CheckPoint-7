using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class MazeGenerator : MonoBehaviourPunCallbacks
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

    [Header("Multiplayer")]
    [Tooltip("Si >0 y 'expectedPlayers' está configurado, el MasterClient esperará a que se unan este número de jugadores antes de fijar la seed.")]
    public int expectedPlayers = 0;

    [Tooltip("Si true, el MasterClient esperará a que Player.CustomProperties[playerSpawnedPropKey] == true para todos los jugadores antes de iniciar la generación.")]
    public bool waitForPlayersSpawnProp = true;
    [Tooltip("Clave de la propiedad de jugador que indica que su avatar ya se instanció/spawneó (ej: 'spawned').")]
    public string playerSpawnedPropKey = "spawned";

    bool hasGenerated = false;

    void Start()
    {
        _mazeGrid = new MazeCell[_mazeWitdth, _mazeDepth];

        // Si no se ha configurado `expectedPlayers` en el inspector, usar el valor de la sala (MaxPlayers)
        if (PhotonNetwork.IsConnected && PhotonNetwork.CurrentRoom != null && expectedPlayers <= 0)
        {
            expectedPlayers = PhotonNetwork.CurrentRoom.MaxPlayers;
            Debug.Log($"MazeGenerator: expectedPlayers ajustado desde Room.MaxPlayers = {expectedPlayers}");
        }

        if (PhotonNetwork.IsConnected)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("mazeSeed", out object seedObj))
                {
                    int seed = (int)seedObj;
                    Debug.Log($"MazeGenerator (Master): found existing mazeSeed={seed}, generating...");
                    Random.InitState(seed);
                    DoGenerate();
                }
                else
                {
                    StartCoroutine(MasterSetupSeedAndGenerate());
                }
            }
            else
            {
                if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("mazeSeed", out object seedObj))
                {
                    int seed = (int)seedObj;
                    Debug.Log($"MazeGenerator (Client): received mazeSeed={seed}, generating...");
                    Random.InitState(seed);
                    DoGenerate();
                }
                else
                {
                    Debug.Log("MazeGenerator: esperando mazeSeed del MasterClient...");
                }
            }
        }
        else
        {
            int seedVal;
            if (_useSeed)
            {
                seedVal = _seed;
            }
            else
            {
                seedVal = Random.Range(1, 1000000);
                Debug.Log($"MazeGenerator (Offline): seed={seedVal}");
            }
            Random.InitState(seedVal);
            DoGenerate();
        }
    }

    IEnumerator MasterSetupSeedAndGenerate()
    {
        if (expectedPlayers > 0)
        {
            Debug.Log($"MazeGenerator (Master): esperando {expectedPlayers} players en la sala...");
            while (PhotonNetwork.CurrentRoom == null || PhotonNetwork.CurrentRoom.PlayerCount < expectedPlayers)
            {
                yield return new WaitForSeconds(0.5f);
            }
        }

        if (waitForPlayersSpawnProp)
        {
            Debug.Log("MazeGenerator (Master): esperando que todos los jugadores marquen su propiedad de spawn...");
            bool allSpawned = false;
            while (!allSpawned)
            {
                allSpawned = true;
                foreach (var p in PhotonNetwork.PlayerList)
                {
                    if (p.CustomProperties == null || !p.CustomProperties.TryGetValue(playerSpawnedPropKey, out object val) || !(val is bool) || !(bool)val)
                    {
                        allSpawned = false;
                        break;
                    }
                }
                if (!allSpawned) yield return new WaitForSeconds(0.5f);
            }
        }

        int seedVal;
        if (_useSeed)
        {
            seedVal = _seed;
        }
        else
        {
            seedVal = Random.Range(1, 1000000);
        }

        var props = new ExitGames.Client.Photon.Hashtable { { "mazeSeed", seedVal } };
        PhotonNetwork.CurrentRoom?.SetCustomProperties(props);
        Debug.Log($"MazeGenerator (Master): publicado mazeSeed={seedVal}");

        Random.InitState(seedVal);
        DoGenerate();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged)
    {
        base.OnRoomPropertiesUpdate(propertiesThatChanged);
        if (hasGenerated) return;
        if (propertiesThatChanged == null) return;
        if (propertiesThatChanged.ContainsKey("mazeSeed"))
        {
            object obj = null;
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("mazeSeed", out obj))
            {
                int seed = (int)obj;
                Debug.Log($"MazeGenerator: recibida mazeSeed={seed} por OnRoomPropertiesUpdate, generando...");
                Random.InitState(seed);
                DoGenerate();
            }
        }
    }

    void DoGenerate()
    {
        if (hasGenerated) return;
        hasGenerated = true;

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

            var placer = Object.FindAnyObjectByType<MazeGoalPlacer>();
            if (placer != null)
            {
                placer.PlaceGoal();
            }
            else
            {
                Debug.LogWarning("MazeGenerator: no MazeGoalPlacer encontrado en la escena. El goal no fue colocado.");
            }

            var collectiblePlacer = Object.FindAnyObjectByType<MazeCollectiblePlacer>();
            if (collectiblePlacer != null)
            {
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
