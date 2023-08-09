using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class PuzzleBoard
{
    public enum TileState
    {
        Empty,
        Included,
        Undetermined
    }

    public class Coordinate
    {
        public int X { get; private set; }
        public int Y { get; private set; }

        public Coordinate(int x, int y)
        {
            X = x;
            Y = y;
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Coordinate))
                return false;

            Coordinate other = (Coordinate)obj;

            return other.X == X && other.Y == Y;
        }

        //no clue what this thing is supposed to be but VS suggested it, so let's let it be
        public override int GetHashCode()
        {
            int hashCode = 1861411795;
            hashCode = hashCode * -1521134295 + X.GetHashCode();
            hashCode = hashCode * -1521134295 + Y.GetHashCode();
            return hashCode;
        }

        public string ToString(int gridSize)
        {
            return "ABCDEFGHIJKLMNOPQRSTUVWXYZ"[X].ToString() + (Y + 1 - (X >= gridSize ? X - gridSize + 1 : 0));
        }
    }

    public class SetPiece
    {
        public PieceData Piece;
        public Coordinate Coordinate;

        public SetPiece(PieceData piece, Coordinate coordinate)
        {
            Piece = piece;
            Coordinate = coordinate;
        }

        public SetPiece(SetPiece old)
        {
            Piece = new PieceData(old.Piece);
            Coordinate = old.Coordinate;
        }

        public override string ToString()
        {
            string piece = "{Colour:";
            piece += Piece.Colour;
            piece += ", Moves:";
            piece += Piece.Moves.Select(x => x == PieceData.MoveState.Allowed ? '1' : (x == PieceData.MoveState.Undetermined ? '?' : '0')).Join("");
            if (Coordinate != null)
                piece += ", Coordinate:" + Coordinate.ToString(3);

            piece += "}";

            return piece;
        }
    }

    public class MoveSequence
    {
        public List<int> Pieces { get; private set; }
        public List<int> Victims { get; private set; }
        public List<PieceData.Move> Moves { get; private set; }
        public PuzzleBoard Board { get; private set; }

        public MoveSequence(List<int> pieces, List<int> victims, List<PieceData.Move> moves, PuzzleBoard board)
        {
            Pieces = pieces;
            Victims = victims;
            Moves = moves;
            Board = board;
        }
    }

    private int _size;
    public int GridDimensions { get { return _size * 2 - 1; } }
    private TileState[,] _tiles;

    private List<int> _colours;

    private List<SetPiece> _pieces;

    public PuzzleBoard(int size, int colourCount, int pieceCount)
    {
        if (colourCount > pieceCount)
            throw new Exception("There cannot be more colours than pieces");

        _size = size;

        _tiles = new TileState[GridDimensions, GridDimensions];
        for (int i = 0; i < GridDimensions; i++)
            for (int j = 0; j < GridDimensions; j++)
            {
                if (i - j >= size || j - i >= size)
                    _tiles[i, j] = TileState.Empty;
                else
                    _tiles[i, j] = TileState.Undetermined;
            }

        _colours = new List<int>();
        for (int i = 0; i < colourCount && i < pieceCount - 1; i++)
            _colours.Add(i);
        while (_colours.Count < pieceCount - 1)
            _colours.Add(Random(colourCount));
        _colours = Shuffle(_colours).ToList();

        _pieces = new List<SetPiece>();

        for (int i = 0; i < colourCount; i++)
            _pieces.Add(new SetPiece(new PieceData(i), null));
        while (_pieces.Count < pieceCount)
            _pieces.Add(new SetPiece(new PieceData(Random(colourCount)), null));
    }

    public PuzzleBoard(PuzzleBoard old)
    {
        _size = old._size;
        _tiles = new TileState[GridDimensions, GridDimensions];
        for (int i = 0; i < GridDimensions; i++)
            for (int j = 0; j < GridDimensions; j++)
            {
                _tiles[i, j] = old._tiles[i, j];
            }
        _colours = new List<int>(old._colours);
        _pieces = old._pieces.Select(x => new SetPiece(x)).ToList();
    }

    public void ReplacePieces(List<SetPiece> pieces)
    {
        _pieces = pieces;
    }

    public IEnumerable<PuzzleBoard> PlacePiece(bool solutionPath = false)
    {
        List<PuzzleBoard> setups = new List<PuzzleBoard>();

        //first piece
        if (_pieces.All(x => x.Coordinate == null))
        {
            for (int k = 0; k < _pieces.Count; k++)
                if (_pieces[k].Piece.Colour == _colours.Last())
                {
                    for (int i = 0; i < GridDimensions; i++)
                        for (int j = 0; j < GridDimensions; j++)
                            if (_tiles[i, j] == TileState.Undetermined)
                            {
                                PuzzleBoard board = new PuzzleBoard(this);
                                board._pieces[k].Coordinate = new Coordinate(j, i);
                                board._tiles[i, j] = TileState.Included;
                                setups.Add(board);
                            }
                    if (!solutionPath)
                        break;
                }

            return setups;
        }

        //subsequent pieces
        int movingColour = _colours[_colours.Count - _pieces.Count(x => x.Coordinate != null)];
        int targetColour = -1;
        if (_pieces.Count(x => x.Coordinate == null) > 1)
            targetColour = _colours[_colours.Count - 1 - _pieces.Count(x => x.Coordinate != null)];

        List<int> captureColours;

        if (targetColour == -1 || _pieces.Any(x => x.Piece.Colour == targetColour && x.Coordinate != null))
            captureColours = _pieces.Where(x => x.Coordinate == null).Select(x => x.Piece.Colour).Distinct().ToList();
        else
            captureColours = new List<int> { targetColour };

        for (int i = 0; i < _pieces.Count; i++)
        {
            SetPiece piece = _pieces[i];

            if (piece.Piece.Colour != movingColour || piece.Coordinate == null)
                continue;

            for (int j = 0; j < 6; j++)
            {
                Coordinate step = Step(piece.Coordinate, (PieceData.Move)j);
                if (step == null)
                    continue;

                Coordinate step2 = Step(step, (PieceData.Move)j);
                if (step2 == null)
                    continue;

                //undetermined means untrespassable in solving, must be able to move there in the first place too
                if (solutionPath && !piece.Piece.CanMove((PieceData.Move)((j + 3) % 6)))
                    continue;

                if (_pieces.Any(x => x.Coordinate != null && x.Coordinate.EqualsAny(step, step2)))
                    continue;

                for (int k = 0; k < _pieces.Count; k++)
                {
                    if (_pieces[k].Coordinate != null || !captureColours.Contains(_pieces[k].Piece.Colour))
                        continue;

                    PuzzleBoard board = new PuzzleBoard(this);

                    board._pieces[k].Coordinate = step;
                    board._pieces[i].Coordinate = step2;

                    //adding moves should not happen during solving
                    if (!solutionPath)
                    {
                        board._pieces[i].Piece.AddMove((PieceData.Move)((j + 3) % 6));
                        captureColours.Remove(_pieces[k].Piece.Colour);
                    }

                    board._tiles[step.Y, step.X] = TileState.Included;
                    board._tiles[step2.Y, step2.X] = TileState.Included;

                    setups.Add(board);
                }
            }
        }

        return setups;
    }

    public static IEnumerable<MoveSequence> CapturePiece(MoveSequence sequence)
    {
        List<MoveSequence> setups = new List<MoveSequence>();

        int move = sequence.Board._pieces.Count(x => x.Coordinate == null);

        for (int i = 0; i < sequence.Board._pieces.Count; i++)
            if (sequence.Board._pieces[i].Piece.Colour == sequence.Board._colours[move] && sequence.Board._pieces[i].Coordinate != null)
                for (int j = 0; j < 6; j++)
                {
                    if (!sequence.Board._pieces[i].Piece.CanMove((PieceData.Move)j))
                        continue;

                    Coordinate step = sequence.Board.Step(sequence.Board._pieces[i].Coordinate, (PieceData.Move)j);
                    if (step == null)
                        continue;

                    Coordinate step2 = sequence.Board.Step(step, (PieceData.Move)j);
                    if (step2 == null || sequence.Board._pieces.Any(x => x.Coordinate != null && x.Coordinate.Equals(step2)))
                        continue;

                    int victim = sequence.Board._pieces.IndexOf(x => x.Coordinate != null && x.Coordinate.Equals(step));

                    if (victim == -1)
                        continue;

                    PuzzleBoard board = new PuzzleBoard(sequence.Board);
                    board._pieces[i].Coordinate = step2;
                    board._pieces[victim].Coordinate = null;

                    MoveSequence newSequence = new MoveSequence(
                        sequence.Pieces.Concat(new int[] { i }).ToList(),
                        sequence.Victims.Concat(new int[] { victim }).ToList(),
                        sequence.Moves.Concat(new PieceData.Move[] { (PieceData.Move)j }).ToList(),
                        board);

                    setups.Add(newSequence);
                }

        return setups;
    }

    public IEnumerable<PuzzleBoard> GetAllSolutions()
    {
        Queue<PuzzleBoard> solutions = new Queue<PuzzleBoard>();
        solutions.Enqueue(GetSolutionPuzzle());

        while (solutions.Peek()._pieces.Any(x => x.Coordinate == null))
        {
            IEnumerable<PuzzleBoard> newBoards = solutions.Dequeue().PlacePiece(true);
            foreach (PuzzleBoard board in newBoards)
                solutions.Enqueue(board);
        }

        return solutions;
    }

    public PuzzleBoard GetSolutionPuzzle()
    {
        PuzzleBoard thisCopy = new PuzzleBoard(this);
        thisCopy._pieces.ForEach(x => x.Coordinate = null);
        for (int i = 0; i < GridDimensions; i++)
            for (int j = 0; j < GridDimensions; j++)
                if (thisCopy._tiles[i, j] == TileState.Included)
                    thisCopy._tiles[i, j] = TileState.Undetermined;
                else
                    thisCopy._tiles[i, j] = TileState.Empty;

        return thisCopy;
    }

    public bool FitsWith(PuzzleBoard constraint)
    {
        if (constraint._pieces.Any(x => x.Coordinate == null) || _pieces.Any(x => x.Coordinate == null))
            return false;

        foreach (SetPiece targetPiece in constraint._pieces)
        {
            bool foundPiece = false;
            foreach (SetPiece piece in _pieces)
            {
                if (!piece.Coordinate.Equals(targetPiece.Coordinate))
                    continue;

                foundPiece = true;

                if (targetPiece.Piece.Moves.All(x => x != PieceData.MoveState.Allowed))
                    continue;

                if (targetPiece.Piece.Colour != piece.Piece.Colour)
                    continue;

                for (int i = 0; i < piece.Piece.Moves.Length; i++)
                    if (targetPiece.Piece.Moves[i] == PieceData.MoveState.Allowed && piece.Piece.Moves[i] != PieceData.MoveState.Allowed)
                        return false;
            }
            if (!foundPiece)
                return false;
        }

        return true;
    }

    public bool HasUniqueishSolution(PuzzleBoard compare = null)
    {
        if (compare == null)
            compare = this;

        int arrangementCount = 0;
        Stack<PuzzleBoard> solutions = new Stack<PuzzleBoard>();
        solutions.Push(GetSolutionPuzzle());

        while (solutions.Count > 0)
        {
            PuzzleBoard currentBoard = solutions.Pop();
            if (currentBoard._pieces.All(x => x.Coordinate != null))
            {
                if (!currentBoard.FitsWith(compare))
                    return false;

                arrangementCount++;
            }
            else
            {
                IEnumerable<PuzzleBoard> newBoards = currentBoard.PlacePiece(true);
                foreach (PuzzleBoard board in newBoards)
                    solutions.Push(board);
            }
        }

        if (arrangementCount == 0)
            throw new Exception("Whatever we did, it made the puzzle unsolvable?");

        return true;
    }

    public IEnumerable<PuzzleBoard> GenerateTileExtensions(PuzzleBoard original)
    {
        List<PuzzleBoard> extensions = new List<PuzzleBoard>();

        for (int i = 0; i < GridDimensions; i++)
            for (int j = 0; j < GridDimensions; j++)
            {
                if (_tiles[i, j] != TileState.Undetermined)
                    continue;

                bool isNeighboured = false;
                for (int k = 0; k < 6; k++)
                {
                    Coordinate neighbour = Step(new Coordinate(j, i), (PieceData.Move)k);
                    if (neighbour != null && _tiles[neighbour.Y, neighbour.X] == TileState.Included)
                    {
                        isNeighboured = true;
                        break;
                    }
                }

                if (!isNeighboured)
                    continue;

                PuzzleBoard board = new PuzzleBoard(this);
                board._tiles[i, j] = TileState.Included;
                if (!board.HasUniqueishSolution(original))
                {
                    _tiles[i, j] = TileState.Empty;
                    foreach (PuzzleBoard extension in extensions)
                        extension._tiles[i, j] = TileState.Empty;
                }
                else
                    extensions.Add(board);
            }

        return extensions;
    }

    public IEnumerable<PuzzleBoard> GenerateDummyPieceMoveExtensions(PuzzleBoard original)
    {
        List<PuzzleBoard> extensions = new List<PuzzleBoard>();

        for (int i = 0; i < _pieces.Count; i++)
        {
            if (_pieces[i].Piece.Moves.Any(x => x == PieceData.MoveState.Allowed))
                continue;

            for (int j = 0; j < 6; j++)
                for (int k = 0; k <= _colours.Max(); k++)
                {
                    if (_pieces.Count(x => x.Piece.Colour == k && x.Piece.Moves[j] == PieceData.MoveState.Allowed) >= _colours.Count(x => x == k))
                        continue;

                    PuzzleBoard board = new PuzzleBoard(this);

                    board._pieces[i].Piece.SetColour(k);
                    board._pieces[i].Piece.AddMove((PieceData.Move)j);

                    if (board.HasUniqueishSolution(original))
                        extensions.Add(board);
                }

            break;
        }
        return extensions;
    }

    public IEnumerable<PuzzleBoard> GeneratePieceMoveExtensions(PuzzleBoard original)
    {
        List<PuzzleBoard> extensions = new List<PuzzleBoard>();

        for (int i = 0; i < _pieces.Count; i++)
            for (int j = 0; j < 6; j++)
            {
                if (_pieces[i].Piece.Moves[j] != PieceData.MoveState.Undetermined || _pieces.Count(x => x.Piece.Colour == _pieces[i].Piece.Colour && x.Piece.Moves[j] == PieceData.MoveState.Allowed) >= _colours.Count(x => x == _pieces[i].Piece.Colour))
                    continue;

                PuzzleBoard board = new PuzzleBoard(this);
                board._pieces[i].Piece.AddMove((PieceData.Move)j);

                if (board.HasUniqueishSolution(original))
                    extensions.Add(board);
            }

        return extensions;
    }

    public PuzzleBoard GeneratePuzzle()
    {
        Stack<PuzzleBoard> attempts = new Stack<PuzzleBoard>();
        attempts.Push(this);

        PuzzleBoard currentAttempt = null;
        PuzzleBoard runningAttempt = null;

        while (attempts.Count > 0)
        {
            currentAttempt = attempts.Pop();

            if (currentAttempt._pieces.Any(x => x.Coordinate == null))
            {
                IEnumerable<PuzzleBoard> continuations = Shuffle(currentAttempt.PlacePiece());
                foreach (PuzzleBoard continuation in continuations)
                    attempts.Push(continuation);
                continue;
            }

            if (!currentAttempt.HasUniqueishSolution())
            {
                continue;
            }

            runningAttempt = currentAttempt;
            break;
        }

        if (runningAttempt == null)
            return null;

        while (true)
        {

            IEnumerable<PuzzleBoard> tileContinuations = currentAttempt.GenerateTileExtensions(runningAttempt);

            if (tileContinuations.Any())
            {
                currentAttempt = tileContinuations.PickRandom();
                continue;
            }

            IEnumerable<PuzzleBoard> dummyMoveContinuations = currentAttempt.GenerateDummyPieceMoveExtensions(runningAttempt);
            if (dummyMoveContinuations.Any())
            {
                currentAttempt = dummyMoveContinuations.PickRandom();
                continue;
            }

            IEnumerable<PuzzleBoard> moveContinuations = currentAttempt.GeneratePieceMoveExtensions(runningAttempt);
            if (moveContinuations.Any())
            {
                currentAttempt = moveContinuations.PickRandom();
                continue;
            }

            currentAttempt.FixDummyPieces();

            return currentAttempt;
        }
    }

    public IEnumerable<Coordinate> GetAllTileCoordinates()
    {
        List<Coordinate> coordinates = new List<Coordinate>();

        for (int i = 0; i < GridDimensions; i++)
            for (int j = 0; j < GridDimensions; j++)
                if (_tiles[i, j] == TileState.Included)
                    coordinates.Add(new Coordinate(j, i));

        return coordinates;
    }

    public IEnumerable<SetPiece> GetAllPieces()
    {
        return _pieces;
    }

    public List<int> GetColourSequence()
    {
        return _colours.ToList();
    }

    public Coordinate Step(Coordinate coordinate, PieceData.Move move)
    {
        int x = coordinate.X;
        int y = coordinate.Y;

        switch (move)
        {
            case PieceData.Move.North:
                y--;
                break;
            case PieceData.Move.Northeast:
                x++;
                break;
            case PieceData.Move.Southeast:
                x++;
                y++;
                break;
            case PieceData.Move.South:
                y++;
                break;
            case PieceData.Move.Southwest:
                x--;
                break;
            case PieceData.Move.Northwest:
                x--;
                y--;
                break;
            default:
                break;
        }

        //Out of bounds
        if (x < 0 || x >= GridDimensions || y < 0 || y >= GridDimensions || _tiles[y, x] == TileState.Empty)
        {
            return null;
        }

        return new Coordinate(x, y);
    }

    public void FixDummyPieces()
    {
        _pieces.ForEach(x => x.Piece.DisallowRest());
        int dummyColour = _pieces.Max(x => x.Piece.Colour) + 1;
        for (int i = 0; i < _pieces.Count; i++)
            if (_pieces[i].Piece.Moves.All(x => x != PieceData.MoveState.Allowed))
                _pieces[i] = new SetPiece(new PieceData(dummyColour), _pieces[i].Coordinate);
    }

    public int Random(int upper)
    {
        return UnityEngine.Random.Range(0, upper);
    }

    public IEnumerable<T> Shuffle<T>(IEnumerable<T> items)
    {
        List<T> result = new List<T>(items);
        result.Shuffle();
        return result;
    }

    public override string ToString()
    {
        string grid = "/";
        for (int i = 0; i < GridDimensions; i++)
        {
            for (int j = 0; j < GridDimensions; j++)
            {
                int piece = _pieces.IndexOf(x => x.Coordinate != null && x.Coordinate.X == j && x.Coordinate.Y == i);
                if (piece != -1)
                    grid += (piece + 1);
                else
                    grid += ".#?"[(int)_tiles[i, j]];
            }
            grid += "/";
        }
        return grid;
    }
}
