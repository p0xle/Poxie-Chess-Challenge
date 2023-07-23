using ChessChallenge.API;
using System;

namespace Chess_Challenge.src.My_Bot;

public class Evaluation
{
    private static int[] _pieceValues =
    {
        0,      // 0: null
        100,    // 1: pawn
        320,    // 2: knight
        330,    // 3: bishop
        500,    // 4: rook
        900,    // 5: queen
        20000   // 6: king
    };

    private static float EndgamePhaseWeightMultiplier => 1 / (_pieceValues[1] * 2 + _pieceValues[3] + _pieceValues[2]);

    public int Evaluate(Board board)
    {
        int whiteMaterial = CountMaterial(board, true);
        int blackMaterial = CountMaterial(board, false);

        int whiteMaterialWithoutPawns = whiteMaterial - board.GetPieceList(PieceType.Pawn, true).Count * _pieceValues[1];
        int blackMaterialWithoutPawns = blackMaterial - board.GetPieceList(PieceType.Pawn, false).Count * _pieceValues[1];

        // MopUpEval not possible due to namespace restrictions

        whiteMaterial += EvaluatePieceSquareTable(board, true, EndgamePhaseWeight(blackMaterialWithoutPawns));
        blackMaterial += EvaluatePieceSquareTable(board, false, EndgamePhaseWeight(whiteMaterialWithoutPawns));

        int eval = whiteMaterial - blackMaterial;

        int perspective = board.IsWhiteToMove ? 1 : -1;
        return eval * perspective;
    }

    private static float EndgamePhaseWeight(int materialCountWithoutPawns)
        => 1 - Math.Min(1, materialCountWithoutPawns * EndgamePhaseWeightMultiplier);

    private static int CountMaterial(Board board, bool isWhite)
    {
        int material = 0;
        for (PieceType piece = PieceType.Pawn; piece <= PieceType.Queen; piece++)
            material += board.GetPieceList(piece, isWhite).Count * _pieceValues[(int)piece];

        return material;
    }

    private static int EvaluatePieceSquareTable(Board board, bool isWhite, float endgamePhaseWeight)
    {
        int value = EvaluatePieceSquareTable(PieceSquareTable.pawnsSquareTable, board.GetPieceList(PieceType.Pawn, isWhite), isWhite);
        value += EvaluatePieceSquareTable(PieceSquareTable.rooksSquareTable, board.GetPieceList(PieceType.Rook, isWhite), isWhite);
        value += EvaluatePieceSquareTable(PieceSquareTable.knightsSquareTable, board.GetPieceList(PieceType.Knight, isWhite), isWhite);
        value += EvaluatePieceSquareTable(PieceSquareTable.bishopsSquareTable, board.GetPieceList(PieceType.Bishop, isWhite), isWhite);
        value += EvaluatePieceSquareTable(PieceSquareTable.queenSquareTable, board.GetPieceList(PieceType.Queen, isWhite), isWhite);
        value += (int)(PieceSquareTable.Read(PieceSquareTable.kingMidGameSquareTable, board.GetKingSquare(isWhite), isWhite) * (1 - endgamePhaseWeight));

        return value;
    }

    private static int EvaluatePieceSquareTable(int[] table, PieceList pieceList, bool isWhite)
    {
        int value = 0;

        for (int i = 0; i < pieceList.Count; i++)
            value += PieceSquareTable.Read(table, pieceList[i].Square, isWhite);

        return value;
    }
}
