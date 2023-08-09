using System.Collections.Generic;
using System.Linq;

public class SelectableManager
{
    public static bool SelectionToggle = true;

    public List<SelectablePiece> Pieces { get; private set; }
    public List<SelectableTile> Tiles { get; private set; }
    public PuzzlePlacer Placer { get; internal set; }

    private SelectablePiece _selectedPiece = null;

    public KMSelectable ModuleSelectable;

    public PieceHighlight PieceHighlight;

    public PiecePositioningScript Module;

    public SelectableManager(List<SelectablePiece> pieces, List<SelectableTile> tiles, SelectableTile box)
    {
        Pieces = pieces;
        Tiles = tiles;
        Tiles.Add(box);

        foreach (SelectablePiece piece in pieces)
        {
            SelectablePiece current = piece;

            current.Selectable.OnHighlight += () =>
            {
                HighlightPiece(current);
            };

            current.Selectable.OnHighlightEnded += () =>
            {
                if (_selectedPiece == null && PieceHighlight != null)
                    PieceHighlight.SetVisibility(false);
            };

            current.Selectable.OnInteract += () =>
            {
                if (_selectedPiece == null)
                    SelectPiece(current);

                return false;
            };

            current.Selectable.OnInteractEnded += () =>
            {
                if (!SelectionToggle)
                    SelectPiece(null);
            };
        }

        foreach (SelectableTile tile in tiles)
        {
            SelectableTile current = tile;

            current.Selectable.OnHighlight += () =>
            {
                _selectedPiece.SetTarget(current);

                if (PieceHighlight != null)
                {
                    PieceHighlight.SetPosition(_selectedPiece.TargetCoordinate);
                    PieceHighlight.SetVisibility(true);
                }
            };

            current.Selectable.OnInteractEnded += () =>
            {
                if (SelectionToggle)
                    SelectPiece(null);
            };
        }

        box.Selectable.OnHighlight += () =>
        {
            _selectedPiece.SetTarget(null);
            int slot = Placer.OccupiedSlots.IndexOf(x => x == null);
            _selectedPiece.TargetCoordinate = Placer.BoxCoordinate(slot, Placer.OccupiedSlots.Count);

            if (PieceHighlight != null)
            {
                PieceHighlight.SetPosition(_selectedPiece.TargetCoordinate);
                PieceHighlight.SetVisibility(true);
            }
        };

        box.Selectable.OnInteractEnded += () =>
        {
            if (SelectionToggle)
                SelectPiece(null);
        };
    }


    public void SelectPiece(SelectablePiece piece = null)
    {
        if (piece != null)
        {
            _selectedPiece = piece;
            _selectedPiece.AllowLowering = false;

            int slot = Placer.OccupiedSlots.IndexOf(_selectedPiece);
            if (slot != -1)
            {
                Placer.OccupiedSlots[slot] = null;
                slot = Placer.OccupiedSlots.IndexOf(x => x == null);
                _selectedPiece.TargetCoordinate = Placer.BoxCoordinate(slot, Placer.OccupiedSlots.Count);

                if (PieceHighlight != null)
                {
                    PieceHighlight.SetPosition(_selectedPiece.TargetCoordinate);
                    PieceHighlight.SetVisibility(true);
                }
            }
        }
        else
        {
            if (_selectedPiece.Target == null)
            {
                int spot = Placer.OccupiedSlots.IndexOf(x => x == null);
                Placer.OccupiedSlots[spot] = _selectedPiece;
            }

            _selectedPiece.AllowLowering = true;
            _selectedPiece = null;

            if (Module.CheckSolve())
            {
                Pieces = new List<SelectablePiece>();
                Tiles = new List<SelectableTile>();
                PieceHighlight.SetVisibility(false);
            }
        }
        UpdateSelectables();
    }

    private void HighlightPiece(SelectablePiece piece)
    {
        if (PieceHighlight != null)
        {
            PieceHighlight.SetPosition(piece.TargetCoordinate);
            PieceHighlight.SetVisibility(true);
        }
    }

    public void UpdateSelectables()
    {
        if (_selectedPiece == null)
        {
            ModuleSelectable.Children = Pieces.Select(x => x.Selectable).ToArray();
            if (PieceHighlight != null)
                PieceHighlight.SetVisibility(false);
        }
        else
        {
            List<KMSelectable> selectables = Tiles.Where(x => x.IsBox || x.CoveringPiece == _selectedPiece || x.CoveringPiece == null).Select(x => x.Selectable).ToList();
            if (!SelectionToggle)
                selectables.Add(_selectedPiece.Selectable);
            ModuleSelectable.Children = selectables.ToArray();

            if (PieceHighlight != null)
            {
                PieceHighlight.SetPosition(_selectedPiece.TargetCoordinate);
                PieceHighlight.SetVisibility(true);
            }
        }

        if (ModuleSelectable.Children.Length == 0)
        {
            ModuleSelectable.Children = new KMSelectable[] { Placer.GetDummy() };
        }

        ModuleSelectable.UpdateChildren();
    }
}
