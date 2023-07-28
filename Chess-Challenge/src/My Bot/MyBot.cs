using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class MyBot : IChessBot
{
    #region variables
    // const == 1 token
    private const int infinity = 10000000;
    private const int immediateMateScore = 100000;
    private const int timerRestriction = 30;

    private readonly HashSet<ulong> _repetitions = new();

    // Values gathered from https://www.chessprogramming.org/Simplified_Evaluation_Function
    private readonly int[] _pieceValues = 
    {
        0,      // 0: null
        5,    // 1: pawn
        30,    // 2: knight
        35,    // 3: bishop
        50,    // 4: rook
        300,    // 5: queen
        20000   // 6: king
    };

    private Move bestMoveThisIteration;
    private int bestEvalThisIteration;
    private Move bestMove;
    private int bestEval;
    #endregion

    // Todo: Store board as variable to reduce tokens?
    public Move Think(Board board, Timer timer)
    {
        // Todo: storing bestEval and all might be more tokens than using out
        bestEvalThisIteration = bestEval = 0;
        bestMoveThisIteration = bestMove = Move.NullMove;

        int totalTime = timer.MillisecondsRemaining;

        _repetitions.Add(board.ZobristKey);

        // Iterative deepening
        for (int depth = 1; depth < 16; depth++)
        {
            Search(board, timer, totalTime, 0, depth, -infinity, infinity);

            // abort search if taking too long, do not trust result
            if (bestMove != Move.NullMove && timer.MillisecondsElapsedThisTurn * timerRestriction > totalTime)
                break;

            if (bestMoveThisIteration != Move.NullMove)
            {
                bestEval = bestEvalThisIteration;
                bestMove = bestMoveThisIteration;
            }

            Console.WriteLine($"{depth} {bestEval} {bestMove}");

            // Exit if found a mate
            if (Math.Abs(bestEval) > immediateMateScore - 1000)
                break;
        }

        return bestMove;
    }

    #region evaluation
    private float EndgamePhaseWeightMultiplier => 1 / (_pieceValues[1] * 2 + _pieceValues[3] + _pieceValues[2]);

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

    private float EndgamePhaseWeight(int materialCountWithoutPawns)
        => 1 - Math.Min(1, materialCountWithoutPawns * EndgamePhaseWeightMultiplier);

    private int CountMaterial(Board board, bool isWhite)
    {
        int material = 0;
        for (PieceType piece = PieceType.Pawn; piece <= PieceType.Queen; piece++)
            material += board.GetPieceList(piece, isWhite).Count * _pieceValues[(int)piece];

        return material;
    }

    private int EvaluatePieceSquareTable(Board board, bool isWhite, float endgamePhaseWeight)
    {

        int value = EvaluatePieceSquareTable(PieceSquareTable.pawnsSquareTable, board.GetPieceList(PieceType.Pawn, isWhite), isWhite);
        value += EvaluatePieceSquareTable(PieceSquareTable.knightsSquareTable, board.GetPieceList(PieceType.Knight, isWhite), isWhite);
        //value += EvaluatePieceSquareTable(PieceSquareTable.bishopsSquareTable, board.GetPieceList(PieceType.Bishop, isWhite), isWhite);
        //value += EvaluatePieceSquareTable(PieceSquareTable.rooksSquareTable, board.GetPieceList(PieceType.Rook, isWhite), isWhite);
        //value += EvaluatePieceSquareTable(PieceSquareTable.queenSquareTable, board.GetPieceList(PieceType.Queen, isWhite), isWhite);
        value += (int)(PieceSquareTable.Read(PieceSquareTable.kingMidGameSquareTable, board.GetKingSquare(isWhite), isWhite) * (1 - endgamePhaseWeight));

        return value;
    }

    private int EvaluatePieceSquareTable(int[] table, PieceList pieceList, bool isWhite)
    {
        int value = 0;

        for (int i = 0; i < pieceList.Count; i++)
            value += PieceSquareTable.Read(table, pieceList[i].Square, isWhite);

        return value;
    }

    #endregion

    #region search
    private int Search(Board board, Timer timer, int totalTime, int ply, int depth, int alpha, int beta)
    {
        if (ply > 0 && _repetitions.Contains(board.ZobristKey))
            return 0;

        if (depth == 0)
            return QuiescenceSearch(board, alpha, beta); // Important!

        var moves = board.GetLegalMoves().ToList();
        OrderMoves(board, moves);

        if (ply == 0 && bestMove != Move.NullMove)
            moves.Insert(0, bestMove);

        if (moves.Count == 0)
        {
            if (board.IsInCheck())
                return -infinity;

            // Stalemate
            return 0;
        }

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int evaluation = -Search(board, timer, totalTime, ply + 1, depth - 1, -beta, -alpha);
            board.UndoMove(move);

            if (depth > 2 && timer.MillisecondsElapsedThisTurn * timerRestriction > totalTime)
                return -infinity;

            if (evaluation >= beta)
                // Move was too good, opponent will avoid this position by playing a different move found earlier
                // therefore no need to continue the search here
                return beta;

            // Found new best move in this position
            if (evaluation > alpha)
            {
                alpha = evaluation;

                if (ply == 0)
                {
                    bestEvalThisIteration = evaluation;
                    bestMoveThisIteration = move;
                }
            }
        }

        return alpha;
    }

    /// <summary>
    /// Search capture moves until a 'quiet' position is reached
    /// </summary>
    /// <param name="board"></param>
    /// <param name="alpha"></param>
    /// <param name="beta"></param>
    /// <returns></returns>
    private int QuiescenceSearch(Board board, int alpha, int beta)
    {
        int evaluation = Evaluate(board);
        if (evaluation >= beta)
            return beta;

        alpha = Math.Max(alpha, evaluation);

        var captureMoves = board.GetLegalMoves(true).ToList();
        OrderMoves(board, captureMoves);

        foreach (Move move in captureMoves)
        {
            board.MakeMove(move);
            evaluation = -QuiescenceSearch(board, -beta, -alpha);
            board.UndoMove(move);

            if (evaluation >= beta)
            {
                return beta;
            }

            alpha = Math.Max(alpha, evaluation);
        }

        return alpha;
    }
    #endregion

    #region ordering
    private void OrderMoves(Board board, List<Move> moves)
    {
        //moves = moves.OrderBy(move => (move.IsCapture || move.IsPromotion) && !board.SquareIsAttackedByOpponent(move.TargetSquare)).ToList();
        //return;
        int[] moveScores = new int[256];

        for (int i = 0; i < moves.Count; i++)
        {
            Move move = moves[i];
            int moveScore = 0;

            // Prioritise capturing opponents most valuable pieces with our least valuable pieces
            if (move.IsCapture)
                moveScore = 10 * _pieceValues[(int)move.CapturePieceType] - _pieceValues[(int)move.MovePieceType];

            // Promoting a pawn is likely to be good
            if (move.IsPromotion)
                moveScore += _pieceValues[(int)move.PromotionPieceType];

            // Penalizing moving our pieces to a square attacked by an opponent pawn
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
                moveScore -= _pieceValues[(int)move.MovePieceType];

            moveScores[i] = moveScore;
        }

        Sort(moves, moveScores);
    }

    private void Sort(List<Move> moves, int[] moveScores)
    {
        for (int i = 0; i < moves.Count - 1; i++)
        {
            for (int j = i + 1; j > 0; j--)
            {
                int swapIndex = j - 1;
                if (moveScores[swapIndex] < moveScores[j])
                {
                    (moves[j], moves[swapIndex]) = (moves[swapIndex], moves[j]);
                    (moveScores[j], moveScores[swapIndex]) = (moveScores[swapIndex], moveScores[j]);
                }
            }
        }
    }
    #endregion
}