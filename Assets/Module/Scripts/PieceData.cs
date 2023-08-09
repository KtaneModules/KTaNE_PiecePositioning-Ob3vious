using System;
using System.Linq;

public class PieceData
{
    public enum Move
    {
        North,
        Northeast,
        Southeast,
        South,
        Southwest,
        Northwest
    }

    public enum MoveState
    {
        Disallowed,
        Allowed,
        Undetermined
    }

    public MoveState[] Moves { get; private set; }
    public int Colour { get; private set; }

    public PieceData(int colour)
    {
        Colour = colour;
        Moves = Enumerable.Repeat(MoveState.Undetermined, 6).ToArray();
    }

    public PieceData(PieceData old)
    {
        Moves = old.Moves.ToArray();
        Colour = old.Colour;
    }

    public void SetColour(int colour)
    {
        if (Moves.Any(x => x == MoveState.Allowed))
            throw new InvalidOperationException("Piece can not be recoloured if it's able to move");

        Colour = colour;
    }

    public void AddMove(Move move)
    {
        Moves[(int)move] = MoveState.Allowed;
    }

    public bool CanMove(Move move)
    {
        return Moves[(int)move] == MoveState.Allowed;
    }

    public void DisallowRest()
    {
        for (int i = 0; i < Moves.Length; i++)
            if (Moves[i] == MoveState.Undetermined)
                Moves[i] = MoveState.Disallowed;
    }
}
