using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ChessModule.Core
{
    public sealed class ChessGame
    {
        public ChessPiece[,] Board { get; private set; }
        public PieceColor SideToMove { get; private set; }
        public bool WhiteCastleKingSide { get; private set; }
        public bool WhiteCastleQueenSide { get; private set; }
        public bool BlackCastleKingSide { get; private set; }
        public bool BlackCastleQueenSide { get; private set; }
        public int EnPassantRow { get; private set; }
        public int EnPassantCol { get; private set; }
        public int HalfMoveClock { get; private set; }
        public int FullMoveNumber { get; private set; }
        public int MoveCount { get; private set; }
        public List<string> MoveLog { get; private set; }

        public ChessGame()
        {
            Board = new ChessPiece[8, 8];
            MoveLog = new List<string>();
            NewGame();
        }

        public void NewGame()
        {
            LoadFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
            MoveCount = 0;
            MoveLog.Clear();
        }

        public ChessGame Clone()
        {
            ChessGame copy = new ChessGame();
            copy.Board = new ChessPiece[8, 8];
            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    copy.Board[r, c] = Board[r, c] == null ? null : Board[r, c].Clone();
                }
            }

            copy.SideToMove = SideToMove;
            copy.WhiteCastleKingSide = WhiteCastleKingSide;
            copy.WhiteCastleQueenSide = WhiteCastleQueenSide;
            copy.BlackCastleKingSide = BlackCastleKingSide;
            copy.BlackCastleQueenSide = BlackCastleQueenSide;
            copy.EnPassantRow = EnPassantRow;
            copy.EnPassantCol = EnPassantCol;
            copy.HalfMoveClock = HalfMoveClock;
            copy.FullMoveNumber = FullMoveNumber;
            copy.MoveCount = MoveCount;
            copy.MoveLog = new List<string>(MoveLog);
            return copy;
        }

        public bool IsInside(int row, int col)
        {
            return row >= 0 && row < 8 && col >= 0 && col < 8;
        }

        public List<ChessMove> GetLegalMoves(PieceColor color)
        {
            List<ChessMove> result = new List<ChessMove>();
            List<ChessMove> pseudo = GetPseudoMoves(color, true);

            foreach (ChessMove move in pseudo)
            {
                ChessGame copy = Clone();
                copy.ApplyMoveUnchecked(move);
                if (!copy.IsKingInCheck(color))
                    result.Add(move);
            }

            return result;
        }

        public List<ChessMove> GetLegalMovesForSquare(int row, int col)
        {
            ChessPiece piece = Board[row, col];
            if (piece == null || piece.Color != SideToMove)
                return new List<ChessMove>();

            return GetLegalMoves(piece.Color)
                .Where(m => m.FromRow == row && m.FromCol == col)
                .ToList();
        }

        public bool TryMakeMove(ChessMove move, out string error)
        {
            error = string.Empty;

            List<ChessMove> legalMoves = GetLegalMoves(SideToMove);
            ChessMove legalMove = legalMoves.FirstOrDefault(m =>
                m.FromRow == move.FromRow &&
                m.FromCol == move.FromCol &&
                m.ToRow == move.ToRow &&
                m.ToCol == move.ToCol);

            if (legalMove == null)
            {
                error = "Недопустимый ход.";
                return false;
            }

            ApplyMoveUnchecked(legalMove);
            MoveLog.Add(legalMove.GetShortText());
            return true;
        }

        public void ApplyMoveUnchecked(ChessMove move)
        {
            ChessPiece piece = Board[move.FromRow, move.FromCol];
            ChessPiece captured = Board[move.ToRow, move.ToCol];

            bool wasPawnMove = piece != null && piece.Type == PieceType.Pawn;
            bool wasCapture = captured != null || move.IsEnPassant;

            if (piece != null && piece.Type == PieceType.King)
            {
                if (piece.Color == PieceColor.White)
                {
                    WhiteCastleKingSide = false;
                    WhiteCastleQueenSide = false;
                }
                else
                {
                    BlackCastleKingSide = false;
                    BlackCastleQueenSide = false;
                }
            }

            if (piece != null && piece.Type == PieceType.Rook)
                DisableRookCastleRight(move.FromRow, move.FromCol);

            if (captured != null && captured.Type == PieceType.Rook)
                DisableRookCastleRight(move.ToRow, move.ToCol);

            Board[move.FromRow, move.FromCol] = null;

            if (move.IsEnPassant)
            {
                int capturedPawnRow = move.FromRow;
                Board[capturedPawnRow, move.ToCol] = null;
            }

            if (move.IsCastling && piece != null)
            {
                if (move.ToCol == 6)
                {
                    Board[move.ToRow, 5] = Board[move.ToRow, 7];
                    Board[move.ToRow, 7] = null;
                }
                else if (move.ToCol == 2)
                {
                    Board[move.ToRow, 3] = Board[move.ToRow, 0];
                    Board[move.ToRow, 0] = null;
                }
            }

            if (piece != null && piece.Type == PieceType.Pawn && (move.ToRow == 0 || move.ToRow == 7))
                piece = new ChessPiece(piece.Color, move.Promotion ?? PieceType.Queen);

            Board[move.ToRow, move.ToCol] = piece;

            EnPassantRow = -1;
            EnPassantCol = -1;
            if (piece != null && piece.Type == PieceType.Pawn && Math.Abs(move.ToRow - move.FromRow) == 2)
            {
                EnPassantRow = (move.ToRow + move.FromRow) / 2;
                EnPassantCol = move.FromCol;
            }

            HalfMoveClock = wasPawnMove || wasCapture ? 0 : HalfMoveClock + 1;

            if (SideToMove == PieceColor.Black)
                FullMoveNumber++;

            SideToMove = Opposite(SideToMove);
            MoveCount++;
        }

        private void DisableRookCastleRight(int row, int col)
        {
            if (row == 7 && col == 0) WhiteCastleQueenSide = false;
            if (row == 7 && col == 7) WhiteCastleKingSide = false;
            if (row == 0 && col == 0) BlackCastleQueenSide = false;
            if (row == 0 && col == 7) BlackCastleKingSide = false;
        }

        public bool IsKingInCheck(PieceColor color)
        {
            int kingRow = -1;
            int kingCol = -1;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece piece = Board[r, c];
                    if (piece != null && piece.Color == color && piece.Type == PieceType.King)
                    {
                        kingRow = r;
                        kingCol = c;
                        break;
                    }
                }
            }

            if (kingRow < 0)
                return true;

            return IsSquareAttacked(kingRow, kingCol, Opposite(color));
        }

        public bool IsCheckmate(PieceColor color)
        {
            return IsKingInCheck(color) && GetLegalMoves(color).Count == 0;
        }

        public bool IsStalemate(PieceColor color)
        {
            return !IsKingInCheck(color) && GetLegalMoves(color).Count == 0;
        }

        public bool IsSquareAttacked(int row, int col, PieceColor byColor)
        {
            int pawnDirection = byColor == PieceColor.White ? -1 : 1;
            int pawnRow = row - pawnDirection;
            if (IsInside(pawnRow, col - 1))
            {
                ChessPiece p = Board[pawnRow, col - 1];
                if (p != null && p.Color == byColor && p.Type == PieceType.Pawn)
                    return true;
            }
            if (IsInside(pawnRow, col + 1))
            {
                ChessPiece p = Board[pawnRow, col + 1];
                if (p != null && p.Color == byColor && p.Type == PieceType.Pawn)
                    return true;
            }

            int[,] knight = new int[,]
            {
                { -2, -1 }, { -2, 1 }, { -1, -2 }, { -1, 2 },
                { 1, -2 }, { 1, 2 }, { 2, -1 }, { 2, 1 }
            };
            for (int i = 0; i < 8; i++)
            {
                int nr = row + knight[i, 0];
                int nc = col + knight[i, 1];
                if (IsInside(nr, nc))
                {
                    ChessPiece p = Board[nr, nc];
                    if (p != null && p.Color == byColor && p.Type == PieceType.Knight)
                        return true;
                }
            }

            if (IsAttackedBySliding(row, col, byColor, new int[,] { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } }, PieceType.Rook, PieceType.Queen))
                return true;

            if (IsAttackedBySliding(row, col, byColor, new int[,] { { -1, -1 }, { -1, 1 }, { 1, -1 }, { 1, 1 } }, PieceType.Bishop, PieceType.Queen))
                return true;

            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0)
                        continue;

                    int nr = row + dr;
                    int nc = col + dc;
                    if (IsInside(nr, nc))
                    {
                        ChessPiece p = Board[nr, nc];
                        if (p != null && p.Color == byColor && p.Type == PieceType.King)
                            return true;
                    }
                }
            }

            return false;
        }

        private bool IsAttackedBySliding(int row, int col, PieceColor byColor, int[,] dirs, PieceType typeA, PieceType typeB)
        {
            int count = dirs.GetLength(0);
            for (int i = 0; i < count; i++)
            {
                int dr = dirs[i, 0];
                int dc = dirs[i, 1];
                int nr = row + dr;
                int nc = col + dc;
                while (IsInside(nr, nc))
                {
                    ChessPiece p = Board[nr, nc];
                    if (p != null)
                    {
                        if (p.Color == byColor && (p.Type == typeA || p.Type == typeB))
                            return true;
                        break;
                    }

                    nr += dr;
                    nc += dc;
                }
            }

            return false;
        }

        private List<ChessMove> GetPseudoMoves(PieceColor color, bool includeCastling)
        {
            List<ChessMove> result = new List<ChessMove>();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece piece = Board[r, c];
                    if (piece == null || piece.Color != color)
                        continue;

                    switch (piece.Type)
                    {
                        case PieceType.Pawn:
                            AddPawnMoves(result, r, c, color);
                            break;
                        case PieceType.Knight:
                            AddKnightMoves(result, r, c, color);
                            break;
                        case PieceType.Bishop:
                            AddSlidingMoves(result, r, c, color, new int[,] { { -1, -1 }, { -1, 1 }, { 1, -1 }, { 1, 1 } });
                            break;
                        case PieceType.Rook:
                            AddSlidingMoves(result, r, c, color, new int[,] { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } });
                            break;
                        case PieceType.Queen:
                            AddSlidingMoves(result, r, c, color, new int[,] { { -1, -1 }, { -1, 1 }, { 1, -1 }, { 1, 1 }, { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 } });
                            break;
                        case PieceType.King:
                            AddKingMoves(result, r, c, color, includeCastling);
                            break;
                    }
                }
            }

            return result;
        }

        private void AddPawnMoves(List<ChessMove> result, int row, int col, PieceColor color)
        {
            int direction = color == PieceColor.White ? -1 : 1;
            int startRow = color == PieceColor.White ? 6 : 1;
            int promotionRow = color == PieceColor.White ? 0 : 7;

            int oneRow = row + direction;
            if (IsInside(oneRow, col) && Board[oneRow, col] == null)
            {
                ChessMove m = new ChessMove(row, col, oneRow, col);
                if (oneRow == promotionRow)
                    m.Promotion = PieceType.Queen;
                result.Add(m);

                int twoRow = row + direction * 2;
                if (row == startRow && IsInside(twoRow, col) && Board[twoRow, col] == null)
                    result.Add(new ChessMove(row, col, twoRow, col));
            }

            for (int dc = -1; dc <= 1; dc += 2)
            {
                int nr = row + direction;
                int nc = col + dc;
                if (!IsInside(nr, nc))
                    continue;

                ChessPiece target = Board[nr, nc];
                if (target != null && target.Color != color)
                {
                    ChessMove capture = new ChessMove(row, col, nr, nc);
                    if (nr == promotionRow)
                        capture.Promotion = PieceType.Queen;
                    result.Add(capture);
                }

                if (nr == EnPassantRow && nc == EnPassantCol)
                {
                    result.Add(new ChessMove(row, col, nr, nc) { IsEnPassant = true });
                }
            }
        }

        private void AddKnightMoves(List<ChessMove> result, int row, int col, PieceColor color)
        {
            int[,] moves = new int[,]
            {
                { -2, -1 }, { -2, 1 }, { -1, -2 }, { -1, 2 },
                { 1, -2 }, { 1, 2 }, { 2, -1 }, { 2, 1 }
            };

            for (int i = 0; i < 8; i++)
                AddMoveIfFreeOrEnemy(result, row, col, row + moves[i, 0], col + moves[i, 1], color);
        }

        private void AddSlidingMoves(List<ChessMove> result, int row, int col, PieceColor color, int[,] dirs)
        {
            int count = dirs.GetLength(0);
            for (int i = 0; i < count; i++)
            {
                int dr = dirs[i, 0];
                int dc = dirs[i, 1];
                int nr = row + dr;
                int nc = col + dc;

                while (IsInside(nr, nc))
                {
                    ChessPiece target = Board[nr, nc];
                    if (target == null)
                    {
                        result.Add(new ChessMove(row, col, nr, nc));
                    }
                    else
                    {
                        if (target.Color != color)
                            result.Add(new ChessMove(row, col, nr, nc));
                        break;
                    }

                    nr += dr;
                    nc += dc;
                }
            }
        }

        private void AddKingMoves(List<ChessMove> result, int row, int col, PieceColor color, bool includeCastling)
        {
            for (int dr = -1; dr <= 1; dr++)
            {
                for (int dc = -1; dc <= 1; dc++)
                {
                    if (dr == 0 && dc == 0)
                        continue;
                    AddMoveIfFreeOrEnemy(result, row, col, row + dr, col + dc, color);
                }
            }

            if (!includeCastling || IsKingInCheck(color))
                return;

            if (color == PieceColor.White && row == 7 && col == 4)
            {
                if (WhiteCastleKingSide && IsRookAt(7, 7, PieceColor.White) && Board[7, 5] == null && Board[7, 6] == null &&
                    !IsSquareAttacked(7, 5, PieceColor.Black) && !IsSquareAttacked(7, 6, PieceColor.Black))
                    result.Add(new ChessMove(7, 4, 7, 6) { IsCastling = true });

                if (WhiteCastleQueenSide && IsRookAt(7, 0, PieceColor.White) && Board[7, 1] == null && Board[7, 2] == null && Board[7, 3] == null &&
                    !IsSquareAttacked(7, 3, PieceColor.Black) && !IsSquareAttacked(7, 2, PieceColor.Black))
                    result.Add(new ChessMove(7, 4, 7, 2) { IsCastling = true });
            }

            if (color == PieceColor.Black && row == 0 && col == 4)
            {
                if (BlackCastleKingSide && IsRookAt(0, 7, PieceColor.Black) && Board[0, 5] == null && Board[0, 6] == null &&
                    !IsSquareAttacked(0, 5, PieceColor.White) && !IsSquareAttacked(0, 6, PieceColor.White))
                    result.Add(new ChessMove(0, 4, 0, 6) { IsCastling = true });

                if (BlackCastleQueenSide && IsRookAt(0, 0, PieceColor.Black) && Board[0, 1] == null && Board[0, 2] == null && Board[0, 3] == null &&
                    !IsSquareAttacked(0, 3, PieceColor.White) && !IsSquareAttacked(0, 2, PieceColor.White))
                    result.Add(new ChessMove(0, 4, 0, 2) { IsCastling = true });
            }
        }

        private bool IsRookAt(int row, int col, PieceColor color)
        {
            ChessPiece rook = Board[row, col];
            return rook != null && rook.Color == color && rook.Type == PieceType.Rook;
        }

        private void AddMoveIfFreeOrEnemy(List<ChessMove> result, int row, int col, int toRow, int toCol, PieceColor color)
        {
            if (!IsInside(toRow, toCol))
                return;

            ChessPiece target = Board[toRow, toCol];
            if (target == null || target.Color != color)
                result.Add(new ChessMove(row, col, toRow, toCol));
        }

        public void SetMoveCount(int moveCount)
        {
            MoveCount = moveCount < 0 ? 0 : moveCount;
        }

        public void SetMoveLog(string moveLogText)
        {
            MoveLog.Clear();

            if (string.IsNullOrWhiteSpace(moveLogText))
                return;

            string[] moves = moveLogText.Split('|');

            foreach (string move in moves)
            {
                if (!string.IsNullOrWhiteSpace(move))
                    MoveLog.Add(move);
            }

        }

        public string ToFen()
        {
            StringBuilder sb = new StringBuilder();

            for (int r = 0; r < 8; r++)
            {
                int empty = 0;
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece piece = Board[r, c];
                    if (piece == null)
                    {
                        empty++;
                        continue;
                    }

                    if (empty > 0)
                    {
                        sb.Append(empty);
                        empty = 0;
                    }
                    sb.Append(GetFenChar(piece));
                }

                if (empty > 0)
                    sb.Append(empty);

                if (r < 7)
                    sb.Append('/');
            }

            sb.Append(SideToMove == PieceColor.White ? " w " : " b ");

            string castles = string.Empty;
            if (WhiteCastleKingSide) castles += "K";
            if (WhiteCastleQueenSide) castles += "Q";
            if (BlackCastleKingSide) castles += "k";
            if (BlackCastleQueenSide) castles += "q";
            sb.Append(string.IsNullOrEmpty(castles) ? "-" : castles);
            sb.Append(' ');

            sb.Append(EnPassantRow >= 0 ? ChessMove.ToSquareName(EnPassantRow, EnPassantCol) : "-");
            sb.Append(' ');
            sb.Append(HalfMoveClock);
            sb.Append(' ');
            sb.Append(FullMoveNumber);

            return sb.ToString();
        }

        public void LoadFen(string fen)
        {
            if (string.IsNullOrWhiteSpace(fen))
                throw new ArgumentException("FEN не заполнен.");

            string[] parts = fen.Trim().Split(' ');
            if (parts.Length < 4)
                throw new ArgumentException("Некорректный FEN.");

            Board = new ChessPiece[8, 8];
            string[] rows = parts[0].Split('/');
            if (rows.Length != 8)
                throw new ArgumentException("Некорректная доска в FEN.");

            for (int r = 0; r < 8; r++)
            {
                int col = 0;
                foreach (char ch in rows[r])
                {
                    if (char.IsDigit(ch))
                    {
                        col += ch - '0';
                    }
                    else
                    {
                        Board[r, col++] = PieceFromFen(ch);
                    }
                }
            }

            SideToMove = parts[1] == "b" ? PieceColor.Black : PieceColor.White;
            WhiteCastleKingSide = parts[2].Contains("K");
            WhiteCastleQueenSide = parts[2].Contains("Q");
            BlackCastleKingSide = parts[2].Contains("k");
            BlackCastleQueenSide = parts[2].Contains("q");

            EnPassantRow = -1;
            EnPassantCol = -1;
            if (parts[3] != "-")
            {
                EnPassantCol = parts[3][0] - 'a';
                EnPassantRow = 8 - int.Parse(parts[3][1].ToString());
            }

            HalfMoveClock = parts.Length > 4 ? int.Parse(parts[4]) : 0;
            FullMoveNumber = parts.Length > 5 ? int.Parse(parts[5]) : 1;
        }

        private char GetFenChar(ChessPiece piece)
        {
            char ch = 'p';
            switch (piece.Type)
            {
                case PieceType.King: ch = 'k'; break;
                case PieceType.Queen: ch = 'q'; break;
                case PieceType.Rook: ch = 'r'; break;
                case PieceType.Bishop: ch = 'b'; break;
                case PieceType.Knight: ch = 'n'; break;
                case PieceType.Pawn: ch = 'p'; break;
            }
            return piece.Color == PieceColor.White ? char.ToUpperInvariant(ch) : ch;
        }

        private ChessPiece PieceFromFen(char ch)
        {
            PieceColor color = char.IsUpper(ch) ? PieceColor.White : PieceColor.Black;
            switch (char.ToLowerInvariant(ch))
            {
                case 'k': return new ChessPiece(color, PieceType.King);
                case 'q': return new ChessPiece(color, PieceType.Queen);
                case 'r': return new ChessPiece(color, PieceType.Rook);
                case 'b': return new ChessPiece(color, PieceType.Bishop);
                case 'n': return new ChessPiece(color, PieceType.Knight);
                case 'p': return new ChessPiece(color, PieceType.Pawn);
                default: throw new ArgumentException("Некорректная фигура в FEN.");
            }
        }

        public static PieceColor Opposite(PieceColor color)
        {
            return color == PieceColor.White ? PieceColor.Black : PieceColor.White;
        }
    }
}
