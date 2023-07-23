using ChessChallenge.API;

public class PieceSquareTable
{
    // Todo: remove static when moving into MyBot (static = 1 Token)

    public static int Read(Board board, int[] table, Square square, bool isWhite)
    {
        int index = square.Index;

        if (isWhite)
        {
            int file = square.File;
            int rank = square.Rank;
            rank = 7 - rank;
            index = rank * 8 + file;
        }

        return table[index];
    }

    // 76 Tokens
    private static readonly int[] pawnsSquareTable =
    {
         0,  0,  0,  0,  0,  0,  0,  0,
        50, 50, 50, 50, 50, 50, 50, 50,
        10, 10, 20, 30, 30, 20, 10, 10,     // provide bonus for advanced pawns
         5,  5, 10, 25, 25, 10,  5,  5,
         0,  0,  0, 20, 20,  0,  0,  0,
         5, -5,-10,  0,  0,-10, -5,  5,     // zeros value to prevent playing with pawns in front of king, negatives to prevent holes
         5, 10, 10,-20,-20, 10, 10,  5,     // positives to provide Shelter for castle on either side
         0,  0,  0,  0,  0,  0,  0,  0
    };

    // 102 Tokens
    private static readonly int[] knightsSquareTable =
    {
        -50,-40,-30,-30,-30,-30,-40,-50,
        -40,-20,  0,  0,  0,  0,-20,-40,
        -30,  0, 10, 15, 15, 10,  0,-30,
        -30,  5, 15, 20, 20, 15,  5,-30,    // encourage to go to center
        -30,  0, 15, 20, 20, 15,  0,-30,    // standing on the edge is bad
        -30,  5, 10, 15, 15, 19,  5,-30,
        -40,-20,  0,  5,  5,  0,-20,-40,
        -50,-40,-30,-30,-30,-30,-40,-50     // standing in the corner is terrible
    };

    // 98 Tokens
    private static readonly int[] bishopsSquareTable =
    {
        -20,-10,-10,-10,-10,-10,-10,-20,    // avoid corners
        -10,  0,  0,  0,  0,  0,  0,-10,    // avoid borders
        -10,  0,  5, 10, 10,  5,  0,-10,
        -10,  5,  5, 10, 10,  5,  5,-10,
        -10,  0, 10, 10, 10, 10,  0,-10,    // prefer center
        -10, 10, 10, 10, 10, 10, 10,-10,
        -10,  5,  0,  0,  0,  0,  5,-10,
        -20,-10,-10,-10,-10,-10,-10,-20
    };

    // most likely to remove due to very small impact / numbers to stay inside the rules boundaries
    // 80 Tokens
    private static readonly int[] rooksSquareTable =
    {
         0,  0,  0,  0,  0,  0,  0,  0,
         5, 10, 10, 10, 10, 10, 10,  5,     // centralize and occupy the 7th rank
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
        -5,  0,  0,  0,  0,  0,  0, -5,
         0,  0,  0,  5,  5,  0,  0,  0
    };

    // 97 Tokens
    // potentially remove as well
    private static readonly int[] queenSquareTable =
    {
        -20,-10,-10, -5, -5,-10,-10,-20,
        -10,  0,  0,  0,  0,  0,  0,-10,
        -10,  0,  5,  5,  5,  5,  0,-10,
         -5,  0,  5,  5,  5,  5,  0, -5,    // centralize
          0,  0,  5,  5,  5,  5,  0, -5,
        -10,  5,  5,  5,  5,  5,  0,-10,
        -10,  0,  5,  0,  0,  0,  0,-10,
        -20,-10,-10, -5, -5,-10,-10,-20
    };

    // 118 Tokens
    private readonly int[] kingMidGameSquareTable =
    {
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -30,-40,-40,-50,-50,-40,-40,-30,
        -20,-30,-30,-40,-40,-30,-30,-20,
        -10,-20,-20,-20,-20,-20,-20,-10,
         20, 20,  0,  0,  0,  0, 20, 20,    // get king to stand behind pawn shelter
         20, 30, 10,  0,  0, 10, 30, 20     // encourage to castle
    };

    // 112 Tokens
    // probably the most important ones
    private static readonly int[] kingEndGameSquareTable =
    {
        -50,-40,-30,-20,-20,-30,-40,-50,
        -30,-20,-10,  0,  0,-10,-20,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,    // try to get enemy king into corner by occupying the center
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 30, 40, 40, 30,-10,-30,
        -30,-10, 20, 30, 30, 20,-10,-30,
        -30,-30,  0,  0,  0,  0,-30,-30,
        -50,-30,-30,-30,-30,-30,-30,-50     // do not stay in the back
    };

    // Overall centrality is always prefered and border / corners are bad
    // Todo: only use boards for pawns and king mid/end game?

    // Possible to convert those into bitboards?
}
