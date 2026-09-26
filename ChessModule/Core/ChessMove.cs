namespace ChessModule.Core
{
    public sealed class ChessMove
    {
        public int FromRow { get; set; }
        public int FromCol { get; set; }
        public int ToRow { get; set; }
        public int ToCol { get; set; }
        public PieceType? Promotion { get; set; }
        public bool IsEnPassant { get; set; }
        public bool IsCastling { get; set; }

        public ChessMove()
        {
        }

        public ChessMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            FromRow = fromRow;
            FromCol = fromCol;
            ToRow = toRow;
            ToCol = toCol;
        }

        public string GetShortText()
        {
            return ToSquareName(FromRow, FromCol) + "-" + ToSquareName(ToRow, ToCol);
        }

        public static string ToSquareName(int row, int col)
        {
            char file = (char)('a' + col);
            int rank = 8 - row;
            return file.ToString() + rank;
        }
    }
}
