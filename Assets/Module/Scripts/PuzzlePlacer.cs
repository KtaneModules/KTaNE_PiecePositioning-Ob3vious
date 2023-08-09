using System.Collections.Generic;
using UnityEngine;

public class PuzzlePlacer
{
    private SelectableTile _box;
    private SelectableTile _referenceTile;
    private SelectablePiece _referencePiece;

    private PuzzleBoard _board;

    public SelectableManager Manager;

    public List<SelectablePiece> OccupiedSlots = new List<SelectablePiece>();

    public PuzzlePlacer(SelectableTile box, SelectableTile referenceTile, SelectablePiece referencePiece, PuzzleBoard board)
    {
        _box = box;

        _referenceTile = referenceTile;
        _referenceTile.GetComponent<MeshRenderer>().enabled = false;

        _referencePiece = referencePiece;
        _referencePiece.GetComponent<MeshRenderer>().enabled = false;
        foreach (MeshRenderer renderer in _referencePiece.GetComponentsInChildren<MeshRenderer>())
            renderer.enabled = false;

        _board = board;
    }

    public void GenerateVisuals()
    {
        List<SelectableTile> tiles = new List<SelectableTile>();
        List<SelectablePiece> pieces = new List<SelectablePiece>();

        foreach (PuzzleBoard.Coordinate coordinate in _board.GetAllTileCoordinates())
        {
            tiles.Add(AddTile(coordinate));
        }

        int i = 0;
        List<PuzzleBoard.SetPiece> pieceCopy = new List<PuzzleBoard.SetPiece>(_board.GetAllPieces());
        pieceCopy.Shuffle();
        foreach (PuzzleBoard.SetPiece piece in pieceCopy)
        {
            pieces.Add(AddPiece(piece, i, pieceCopy.Count));
            //make sure the piece is set to be on the tile

            /*
            if (tiles.Any(x => x.Coordinate.Equals(piece.Coordinate)))
                tiles.First(x => x.Coordinate.Equals(piece.Coordinate)).CoveringPiece = pieces.Last();
            */
            i++;
        }

        Manager = new SelectableManager(pieces, tiles, _box);
        Manager.Placer = this;
    }


    public SelectableTile AddTile(PuzzleBoard.Coordinate coordinate)
    {
        SelectableTile tile = _referenceTile.CreateCopy(coordinate);
        tile.GetComponent<MeshRenderer>().enabled = true;

        tile.transform.localPosition = MapCoordinate(coordinate) + _referenceTile.transform.localPosition;

        return tile;
    }

    public SelectablePiece AddPiece(PuzzleBoard.SetPiece setPiece, int index, int total)
    {
        SelectablePiece piece = _referencePiece.CreateCopy();
        piece.GetComponent<MeshRenderer>().enabled = true;
        foreach (MeshRenderer renderer in piece.GetComponentsInChildren<MeshRenderer>())
            renderer.enabled = true;

        piece.AssignPiece(setPiece.Piece);

        //piece.transform.localPosition = MapCoordinate(setPiece.Coordinate) + _referencePiece.transform.localPosition;
        piece.transform.localPosition = BoxCoordinate(index, total);
        piece.TargetCoordinate = piece.transform.localPosition;

        OccupiedSlots.Add(piece);

        return piece;
    }

    public Vector3 MapCoordinate(PuzzleBoard.Coordinate coordinate)
    {
        int middle = _board.GridDimensions / 2;

        float x = coordinate.X - middle;
        float y = middle - coordinate.Y;
        y += x * 0.5f;

        y *= 0.02f;
        x *= 0.01f * Mathf.Sqrt(3);

        return new Vector3(x, 0, y);
    }

    public Vector3 BoxCoordinate(int index, int total)
    {
        return new Vector3((index % 2) * 0.005f + 0.0615f, 0.001f, Mathf.Lerp(0.0225f, -0.0545f, index / (total - 1f)));
    }

    public KMSelectable GetDummy()
    {
        return _referencePiece.Selectable;
    }
}