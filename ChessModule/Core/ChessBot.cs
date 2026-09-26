using System;
using System.Collections.Generic;

namespace ChessModule.Core
{
    public static class ChessBot
    {
        private static readonly Random Random = new Random();

        public static ChessMove ChooseMove(ChessGame game, BotDifficulty difficulty, PieceColor botColor)
        {
            List<ChessMove> moves = game.GetLegalMoves(botColor);
            if (moves.Count == 0)
                return null;

            if (difficulty == BotDifficulty.Random)
                return moves[Random.Next(moves.Count)];

            if (difficulty == BotDifficulty.Greedy)
                return ChooseGreedyMove(game, moves, botColor);

            return ChooseMiniMaxMove(game, moves, botColor);
        }

        private static ChessMove ChooseGreedyMove(ChessGame game, List<ChessMove> moves, PieceColor botColor)
        {
            int bestScore = int.MinValue;
            List<ChessMove> bestMoves = new List<ChessMove>();

            foreach (ChessMove move in moves)
            {
                int score = ScoreMove(game, move, botColor);
                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(move);
                }
            }

            return bestMoves[Random.Next(bestMoves.Count)];
        }

        private static ChessMove ChooseMiniMaxMove(ChessGame game, List<ChessMove> moves, PieceColor botColor)
        {
            int bestScore = int.MinValue;
            List<ChessMove> bestMoves = new List<ChessMove>();

            foreach (ChessMove move in moves)
            {
                ChessGame copy = game.Clone();
                copy.ApplyMoveUnchecked(move);
                int score = Minimax(copy, 2, false, botColor, int.MinValue + 1, int.MaxValue - 1);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(move);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(move);
                }
            }

            return bestMoves[Random.Next(bestMoves.Count)];
        }

        private static int Minimax(ChessGame game, int depth, bool maximizing, PieceColor botColor, int alpha, int beta)
        {
            PieceColor side = game.SideToMove;
            List<ChessMove> legal = game.GetLegalMoves(side);

            if (legal.Count == 0)
            {
                if (game.IsKingInCheck(side))
                    return side == botColor ? -100000 : 100000;
                return 0;
            }

            if (depth == 0)
                return EvaluateBoard(game, botColor);

            if (maximizing)
            {
                int best = int.MinValue + 1;
                foreach (ChessMove move in legal)
                {
                    ChessGame copy = game.Clone();
                    copy.ApplyMoveUnchecked(move);
                    best = Math.Max(best, Minimax(copy, depth - 1, false, botColor, alpha, beta));
                    alpha = Math.Max(alpha, best);
                    if (beta <= alpha)
                        break;
                }
                return best;
            }
            else
            {
                int best = int.MaxValue - 1;
                foreach (ChessMove move in legal)
                {
                    ChessGame copy = game.Clone();
                    copy.ApplyMoveUnchecked(move);
                    best = Math.Min(best, Minimax(copy, depth - 1, true, botColor, alpha, beta));
                    beta = Math.Min(beta, best);
                    if (beta <= alpha)
                        break;
                }
                return best;
            }
        }

        private static int ScoreMove(ChessGame game, ChessMove move, PieceColor botColor)
        {
            int score = 0;
            ChessPiece target = game.Board[move.ToRow, move.ToCol];
            ChessPiece mover = game.Board[move.FromRow, move.FromCol];

            if (target != null)
                score += PieceValue(target.Type) * 10 - PieceValue(mover.Type);

            if (move.IsEnPassant)
                score += PieceValue(PieceType.Pawn) * 10;

            if (move.Promotion.HasValue)
                score += PieceValue(move.Promotion.Value) * 3;

            ChessGame copy = game.Clone();
            copy.ApplyMoveUnchecked(move);
            if (copy.IsCheckmate(ChessGame.Opposite(botColor)))
                score += 100000;
            else if (copy.IsKingInCheck(ChessGame.Opposite(botColor)))
                score += 50;

            return score;
        }

        public static int EvaluateBoard(ChessGame game, PieceColor perspective)
        {
            int score = 0;
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece piece = game.Board[r, c];
                    if (piece == null)
                        continue;

                    int value = PieceValue(piece.Type);
                    score += piece.Color == perspective ? value : -value;
                }
            }

            return score;
        }

        public static int PieceValue(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn: return 100;
                case PieceType.Knight: return 320;
                case PieceType.Bishop: return 330;
                case PieceType.Rook: return 500;
                case PieceType.Queen: return 900;
                case PieceType.King: return 20000;
                default: return 0;
            }
        }
    }
}
