using System;

namespace ChessModule.Core
{
    public sealed class ChessPiece
    {
        public PieceColor Color { get; set; }
        public PieceType Type { get; set; }

        public ChessPiece(PieceColor color, PieceType type)
        {
            Color = color;
            Type = type;
        }

        public ChessPiece Clone()
        {
            return new ChessPiece(Color, Type);
        }

        public override string ToString()
        {
            return Type.ToString();
        }
    }
}