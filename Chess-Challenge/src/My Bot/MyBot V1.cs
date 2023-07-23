using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

public class MyBot : IChessBot
{
    #region variables
    private const int _negativeInfinity = -10000000;
    private const int immediateMateScore = 100000;
    private const int timerRestriction = 20;

    private readonly HashSet<ulong> _repetitions = new();

    private readonly int[] _pieceValues = 
    {
        0,      // 0: null
        100,    // 1: pawn
        300,    // 2: knight
        320,    // 3: bishop
        500,    // 4: rook
        900,    // 5: queen
        20000   // 6: king
    };

    private Move bestMoveThisIteration;
    private int bestEvalThisIteration;
    private Move bestMove;
    private int bestEval;
    #endregion

    public Move Think(Board board, Timer timer)
    {
        bestEvalThisIteration = bestEval = 0;
        bestMoveThisIteration = bestMove = Move.NullMove;

        int totalTime = timer.MillisecondsRemaining;

        _repetitions.Add(board.ZobristKey);
        HashSet<ulong> repetitionsCopy = _repetitions.ToHashSet();


        for (int depth = 1; depth < 128; depth++)
        {
            Search(board, timer, totalTime, 0, depth, _negativeInfinity, -_negativeInfinity, 0, repetitionsCopy);

            // abort search if taking too long
            if (bestMoveThisIteration != Move.NullMove && timer.MillisecondsElapsedThisTurn * timerRestriction > totalTime)
                break;

            if (bestMoveThisIteration != Move.NullMove)
            {
                bestEval = bestEvalThisIteration;
                bestMove = bestMoveThisIteration;
            }

            Console.WriteLine($"{bestEval} {bestMove}");

            // Exit if found a mate
            if (Math.Abs(bestEval) > immediateMateScore - 1000)
                break;
        }

        return bestMove;
    }

    #region evaluation
    private int Evaluate(Board board)
    {
        int score = 0;
        for (int color = 0; color < 2; color++)
        {
            for (PieceType piece = PieceType.Pawn; piece <= PieceType.King; piece++)
            {
                ulong bitboard = board.GetPieceBitboard(piece, color == 0);

                while (bitboard != 0)
                {
                    int sq = BitOperations.TrailingZeroCount(bitboard);
                    bitboard &= bitboard - 1;

                    // Material
                    score += _pieceValues[(int)piece];

                    // Centrality
                    int rank = sq >> 3;
                    int file = sq & 7;
                    int centrality = -Math.Abs(7 - rank - file) - Math.Abs(rank - file);
                    score += centrality * (6 - (int)piece);
                }
            }

            score = -score;
        }

        if (!board.IsWhiteToMove)
            score = -score;

        return score;
    }

    #endregion

    #region search
    private int Search(Board board, Timer timer, int totalTime, int ply, int depth, int alpha, int beta, int numExtensions, HashSet<ulong> repetitions)
    {
        if (depth > 2 && timer.MillisecondsElapsedThisTurn * timerRestriction > totalTime)
        {
            return 0;
        }

        if (ply > 0)
        {
            if (repetitions.Contains(board.ZobristKey))
                return 0;

            alpha = Math.Max(alpha, -immediateMateScore + ply);
            beta = Math.Min(beta, immediateMateScore - ply);

            if (alpha >= beta)
                return alpha;
        }

        if (depth == 0)
            return QuiescenceSearch(board, alpha, beta);

        var moves = board.GetLegalMoves().ToList();
        OrderMoves(board, moves.ToArray());

        if (ply == 0 && bestMove != Move.NullMove)
            moves.Insert(0, bestMove);

        if (moves.Count == 0)
        {
            if (board.IsInCheck())
                return _negativeInfinity;

            // Stalemate
            return 0;
        }

        foreach (Move move in moves)
        {
            board.MakeMove(move);
            int extension = CalculateExtensionDepth(board, move, numExtensions);
            int evaluation = -Search(board, timer, totalTime, ply + 1, depth - 1 + extension, -beta, -alpha, numExtensions + extension, repetitions);
            board.UndoMove(move);

            if (depth > 2 && timer.MillisecondsElapsedThisTurn * timerRestriction > totalTime)
            {
                return 0;
            }

            if (evaluation >= beta)
            {
                // Move was too good, opponent will avoid this position
                return beta;
            }

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

    private int CalculateExtensionDepth(Board board, Move move, int numExtensions)
    {
        return 0;

        int extension = 0;
        
        if (numExtensions >= 16)
        {
            return extension;
        }

        if (board.IsInCheck())
            extension = 1;
        else if (move.MovePieceType is PieceType.Pawn && (move.TargetSquare.Rank is 6 or 1))
            extension = 1;

        return extension;
    }

    // Search capture moves until a 'quiet' position is reached
    private int QuiescenceSearch(Board board, int alpha, int beta)
    {
        int evaluation = Evaluate(board);
        if (evaluation >= beta)
            return beta;

        alpha = Math.Max(alpha, evaluation);

        var captureMoves = board.GetLegalMoves(true);
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
    private void OrderMoves(Board board, Move[] moves)
    {
        int[] moveScores = new int[256];

        for (int i = 0; i < moves.Length; i++)
        {
            Move move = moves[i];
            int moveScore = 0;

            // Prioritise capturing opponents most valuable pieces with our least valuable pieces
            if (move.IsCapture)
            {
                moveScore = 10 * _pieceValues[(int)move.CapturePieceType] - _pieceValues[(int)move.MovePieceType];
            }

            // Promoting a pawn is likely to be good
            if (move.IsPromotion)
            {
                moveScore += _pieceValues[(int)move.PromotionPieceType];
            }

            // Penalizing moving our pieces to a square attacked by an opponent pawn
            if (board.SquareIsAttackedByOpponent(move.TargetSquare))
            {
                moveScore -= _pieceValues[(int)move.MovePieceType];
            }

            moveScores[i] = moveScore;
        }

        Sort(moves, moveScores);
    }

    private void Sort(Move[] moves, int[] moveScores)
    {
        for (int i = 0; i < moves.Length - 1; i++)
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