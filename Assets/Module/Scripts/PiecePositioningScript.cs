using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;

public class PiecePositioningScript : MonoBehaviour
{
    bool TwitchPlaysActive;
    private KMModSettings _settings;

    //public static List<Color> Colours = new List<Color> { new Color32(0xe0, 0x40, 0x40, 0xff), new Color32(0x40, 0x40, 0xe0, 0xff), new Color32(0x40, 0xe0, 0x40, 0xff), new Color32(0xe0, 0xc0, 0x40, 0xff) };
    public static readonly List<Color> Colours = new List<Color> { new Color32(0x00, 0xaa, 0x55, 0xff), new Color32(0x00, 0x44, 0xaa, 0xff), new Color32(0xaa, 0x00, 0xaa, 0xff), new Color32(0x55, 0x44, 0x55, 0xff) };

    private PuzzlePlacer _placer;
    private SelectableManager _manager;

    private PuzzleBoard _board;

    [SerializeField]
    private Transform _lid;
    private Vector3 _lidOpen;
    private float _lidOpenAngle;

    private bool _solved = false;
    private bool _passed = false;
    private bool _open = false;

    private KMAudio _audio;

    private int _moduleId;
    private static int _moduleIdCounter = 1;
    private readonly string _moduleName = "Piece Positioning";

    private static readonly int _pieceCount = 6;

    void Awake()
    {
        _moduleId = _moduleIdCounter++;
    }

    void Start()
    {
        _audio = GetComponent<KMAudio>();
        _settings = GetComponent<KMModSettings>();

        SelectableManager.SelectionToggle = GetSelectionMode();

        _board = new PuzzleBoard(3, 3, _pieceCount).GeneratePuzzle();

        Log("The board is set up as follows: {0} (slashes are newlines, skew right columns upwards).", _board);
        Log("The colour order is {0}.", _board.GetColourSequence().Join("-"));
        Log("The pieces (and one possible solution) are: {0}. Moves are ordered north going clockwise.", _board.GetAllPieces().Join(", "));

        _placer = new PuzzlePlacer(GetComponentsInChildren<SelectableTile>().First(x => x.IsBox), GetComponentsInChildren<SelectableTile>().First(x => !x.IsBox), GetComponentInChildren<SelectablePiece>(), _board);
        GetComponentInChildren<IndicatorPanel>().SetColourSequence(_board.GetColourSequence());
        _placer.GenerateVisuals();

        _placer.Manager.ModuleSelectable = GetComponent<KMSelectable>();
        _placer.Manager.UpdateSelectables();

        _manager = _placer.Manager;
        _manager.PieceHighlight = GetComponentInChildren<PieceHighlight>();
        _manager.Module = this;

        _lidOpen = _lid.localPosition;
        _lidOpenAngle = 120; //I would retrieve this, but localEulerAngles gives 60 180 180 rather than 120 0 0

        _lid.transform.localPosition = Vector3.zero;
        _lid.transform.localEulerAngles = Vector3.zero;

        _manager.ModuleSelectable.OnInteract += () =>
        {
            if (!_open)
                StartCoroutine(Open());

            return true;
        };

        _manager.Pieces.ForEach(x => x.Audio = _audio);
    }

    void Update()
    {
        if (TwitchPlaysActive && !_open)
        {
            StartCoroutine(Open());
        }
    }

    public bool CheckSolve()
    {
        if (_manager.Pieces.Any(x => x.Target == null))
            return false;

        PuzzleBoard board = new PuzzleBoard(_board);
        board.ReplacePieces(_manager.Pieces.Select(x => new PuzzleBoard.SetPiece(x.Data, x.Target.Coordinate)).ToList());

        Queue<PuzzleBoard.MoveSequence> solutions = new Queue<PuzzleBoard.MoveSequence>();
        solutions.Enqueue(new PuzzleBoard.MoveSequence(new List<int>(), new List<int>(), new List<PieceData.Move>(), board));

        while (solutions.Count > 0 && solutions.Peek().Pieces.Count < _pieceCount - 1)
        {
            IEnumerable<PuzzleBoard.MoveSequence> newSolutions = PuzzleBoard.CapturePiece(solutions.Dequeue());
            foreach (PuzzleBoard.MoveSequence sequence in newSolutions)
                solutions.Enqueue(sequence);
        }

        if (solutions.Count > 0)
        {
            Log("A solution has been found. Module solved!");
            _solved = true;
            StartCoroutine(Solve(solutions.PickRandom()));
            return true;
        }

        return false;
    }

    private IEnumerator Open()
    {
        _open = true;

        for (float t = 0; t < 1; t += Time.deltaTime * 2)
        {
            _lid.transform.localPosition = Vector3.Lerp(Vector3.zero, _lidOpen, t);
            _lid.transform.localEulerAngles = Vector3.right * _lidOpenAngle * t;
            yield return null;
        }

        _lid.transform.localPosition = _lidOpen;
        _lid.transform.localEulerAngles = Vector3.right * _lidOpenAngle;
    }

    private IEnumerator Solve(PuzzleBoard.MoveSequence solution)
    {
        List<SelectablePiece> pieces = _manager.Pieces;

        yield return new WaitForSeconds(0.5f);

        List<PuzzleBoard.Coordinate> positions = pieces.Select(x => x.Target.Coordinate).ToList();
        for (int i = 0; i < solution.Moves.Count; i++)
        {
            PieceData.Move move = solution.Moves[i];
            positions[solution.Pieces[i]] = solution.Board.Step(solution.Board.Step(positions[solution.Pieces[i]], move), move);
            pieces[solution.Pieces[i]].TargetCoordinate = _placer.MapCoordinate(positions[solution.Pieces[i]]);
            yield return new WaitForSeconds(0.2f);
            pieces[solution.Victims[i]].TargetCoordinate = _placer.BoxCoordinate(i, _pieceCount);
            yield return new WaitForSeconds(0.4f);
        }

        pieces[solution.Pieces.Last()].TargetCoordinate = _placer.BoxCoordinate(_pieceCount - 1, _pieceCount);
        yield return new WaitForSeconds(0.5f);

        for (float t = 0; t < 1; t += Time.deltaTime * 2)
        {
            _lid.transform.localPosition = Vector3.Lerp(_lidOpen, Vector3.zero, t);
            _lid.transform.localEulerAngles = Vector3.right * _lidOpenAngle * (1 - t);
            yield return null;
        }

        _lid.transform.localPosition = Vector3.zero;
        _lid.transform.localEulerAngles = Vector3.zero;

        _audio.PlaySoundAtTransform("Magnet", _lid.transform);

        _passed = true;
        GetComponent<KMBombModule>().HandlePass();
    }

#pragma warning disable 414
    private string TwitchHelpMessage = "'!{0} move 1 A1' to move piece 1 to A1. '!{0} move 1 box' to move piece 1 back to the box. '!{0} piece A1' to ask the number of the piece on A1.";
#pragma warning restore 414
    IEnumerator ProcessTwitchCommand(string command)
    {
        yield return null;

        command = command.ToLowerInvariant();
        string[] commands = command.Split(' ');
        if (commands.Length == 3 && commands[0] == "move" && commands[1].Length == 1 && commands[2].Length == 2)
        {
            if (!"abcde".Contains(commands[2][0]) || !"12345".Contains(commands[2][1]) || !"123456".Contains(commands[1][0]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
            int tileIndex = _manager.Tiles.IndexOf(x => !x.IsBox && x.Coordinate.X == commands[2][0] - 'a' && (x.Coordinate.Y - (x.Coordinate.X >= 3 ? x.Coordinate.X - 2 : 0)) == commands[2][1] - '1');
            if (tileIndex < 0)
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }

            KMSelectable tile = _manager.Tiles[tileIndex].Selectable;
            KMSelectable piece = _manager.Pieces[commands[1][0] - '1'].Selectable;

            bool rememberedSelectionType = SelectableManager.SelectionToggle;
            SelectableManager.SelectionToggle = false;

            piece.OnHighlight();
            yield return new WaitForSeconds(0.1f);
            piece.OnInteract();
            yield return new WaitForSeconds(0.1f);
            piece.OnHighlightEnded();
            if (GetComponent<KMSelectable>().Children.Contains(tile))
                tile.OnHighlight();
            yield return new WaitForSeconds(0.1f);
            piece.OnInteractEnded();

            SelectableManager.SelectionToggle = rememberedSelectionType;
        }
        else if (commands.Length == 3 && commands[0] == "move" && commands[1].Length == 1 && commands[2] == "box")
        {
            if (!"123456".Contains(commands[1][0]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
            if (_placer.OccupiedSlots.Contains(_manager.Pieces[commands[1][0] - '1']))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }

            KMSelectable tile = _manager.Tiles.First(x => x.IsBox).Selectable;
            KMSelectable piece = _manager.Pieces[commands[1][0] - '1'].Selectable;

            bool rememberedSelectionType = SelectableManager.SelectionToggle;
            SelectableManager.SelectionToggle = false;

            piece.OnHighlight();
            yield return new WaitForSeconds(0.1f);
            piece.OnInteract();
            yield return new WaitForSeconds(0.1f);
            piece.OnHighlightEnded();
            if (GetComponent<KMSelectable>().Children.Contains(tile))
                tile.OnHighlight();
            yield return new WaitForSeconds(0.1f);
            piece.OnInteractEnded();

            SelectableManager.SelectionToggle = rememberedSelectionType;
        }
        else if (commands.Length == 2 && commands[0] == "piece" && commands[1].Length == 2)
        {
            if (!"abcde".Contains(commands[1][0]) || !"12345".Contains(commands[1][1]))
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
            int pieceIndex = _manager.Pieces.IndexOf(x => x.Target != null && !x.Target.IsBox && x.Target.Coordinate.X == commands[1][0] - 'a' && (x.Target.Coordinate.Y - (x.Target.Coordinate.X >= 3 ? x.Target.Coordinate.X - 2 : 0)) == commands[1][1] - '1');
            if (pieceIndex < 0)
            {
                yield return "sendtochaterror Invalid command.";
                yield break;
            }
            else
            {
                yield return "sendtochat The piece on " + commands[1].ToUpperInvariant() + " is piece " + (pieceIndex + 1);
            }
        }
        else
        {
            yield return "sendtochaterror Invalid command.";
            yield break;
        }
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        if (!_solved)
        {
            PuzzleBoard board = new PuzzleBoard(_board);
            board.ReplacePieces(_manager.Pieces.Select(x => new PuzzleBoard.SetPiece(x.Data, x.Target == null ? null : x.Target.Coordinate)).ToList());
            board = board.GetAllSolutions().PickRandom();

            while (!_solved)
            {
                KMSelectable piece;
                KMSelectable tile;

                int pieceIndex = Enumerable.Range(0, _pieceCount).IndexOf(x => _manager.Tiles.Any(y => !y.IsBox && y.Coordinate.Equals(board.GetAllPieces().ToList()[x].Coordinate) && y.CoveringPiece == null));
                if (pieceIndex != -1)
                {
                    //optimal
                    piece = _manager.Pieces[pieceIndex].Selectable;
                    tile = _manager.Tiles.First(y => y.Coordinate.Equals(board.GetAllPieces().ToList()[pieceIndex].Coordinate) && y.CoveringPiece == null).Selectable;
                }
                else
                {
                    //buffering if optimal is impossible
                    pieceIndex = Enumerable.Range(0, _pieceCount).IndexOf(x => _manager.Tiles.Any(y => !y.IsBox && y.Coordinate.Equals(board.GetAllPieces().ToList()[x].Coordinate) && y.CoveringPiece != null));

                    piece = _manager.Pieces[pieceIndex].Selectable;
                    tile = _manager.Tiles.First(y => y.IsBox).Selectable;
                }

                bool rememberedSelectionType = SelectableManager.SelectionToggle;
                SelectableManager.SelectionToggle = false;

                piece.OnHighlight();
                yield return new WaitForSeconds(0.1f);
                piece.OnInteract();
                yield return new WaitForSeconds(0.1f);
                piece.OnHighlightEnded();
                if (GetComponent<KMSelectable>().Children.Contains(tile))
                    tile.OnHighlight();
                yield return new WaitForSeconds(0.1f);
                piece.OnInteractEnded();
                yield return new WaitForSeconds(0.1f);

                SelectableManager.SelectionToggle = rememberedSelectionType;
            }
        }

        while (!_passed)
        {
            yield return true;
        }
    }

    public class ModSettings
    {
        public bool ToggleSelection = false;
    }

    public bool GetSelectionMode()
    {
        try
        {
            ModSettings settings = JsonConvert.DeserializeObject<ModSettings>(_settings.Settings);

            return settings.ToggleSelection;
        }
        catch
        {
            ModSettings settings = new ModSettings();
            File.WriteAllText(_settings.SettingsPath, JsonConvert.SerializeObject(settings));

            return false;
        }
    }

    private void Log(string format, params object[] args)
    {
        Debug.LogFormat("[{0} #{1}] {2}", _moduleName, _moduleId, string.Format(format, args));
    }
}
