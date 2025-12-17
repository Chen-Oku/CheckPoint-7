using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class MazeCell : MonoBehaviour
{
    [SerializeField]
    private GameObject _leftWall;

    [SerializeField]
    private GameObject _rightWall;

    [SerializeField]
    private GameObject _frontWall;

    [SerializeField]
    private GameObject _backWall;

    [SerializeField]
    private GameObject _unvisitedBlock;

    public bool IsVisited { get; private set; }

    // Nueva: lista de celdas conectadas (vecinos)
    public List<MazeCell> neighbors = new List<MazeCell>();

    public void AddNeighbor(MazeCell other)
    {
        if (other == null) return;
        if (!neighbors.Contains(other)) neighbors.Add(other);
    }

    public void Visit()
    {
        IsVisited = true;
        if (_unvisitedBlock != null) _unvisitedBlock.SetActive(false);
    }

    public void ClearLeftWall()
    {
        if (_leftWall != null) _leftWall.SetActive(false);
    }
    public void ClearRightWall()
    {
        if (_rightWall != null) _rightWall.SetActive(false);
    }
    public void ClearFrontWall()
    {
        if (_frontWall != null) _frontWall.SetActive(false);
    }
    public void ClearBackWall()
    {
        if (_backWall != null) _backWall.SetActive(false);
    }
}
