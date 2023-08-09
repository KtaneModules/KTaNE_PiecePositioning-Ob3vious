using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectableTile : MonoBehaviour
{
    public bool IsBox;

    public PuzzleBoard.Coordinate Coordinate { get; private set; }
    public KMSelectable Selectable { get; private set; }
    public SelectablePiece CoveringPiece { get; set; }

    void Awake()
    {
        Selectable = GetComponent<KMSelectable>();
        CoveringPiece = null;
        Coordinate = null;
    }

    public SelectableTile CreateCopy(PuzzleBoard.Coordinate coordinate)
    {
        SelectableTile copy = Instantiate(this, transform.parent);
        copy.Coordinate = coordinate;
        return copy;
    }
}
