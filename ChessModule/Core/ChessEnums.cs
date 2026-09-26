

namespace ChessModule.Core
{
    public enum PieceColor
    {
        White,
        Black
    }

    public enum PieceType
    {
        Pawn,
        Knight,
        Bishop,
        Rook,
        Queen,
        King
    }

    public enum GameMode
    {
        HumanVsHuman = 0,
        HumanVsBot = 1
    }

    public enum BotDifficulty
    {
        Random = 1,
        Greedy = 2,
        MiniMax = 3
    }

    public enum GameTimeMode
    {
        NoLimit = 0,
        Rapid10 = 1,
        Blitz5 = 2
    }

    public enum GameResult
    {
        InProgress,
        WhiteWin,
        BlackWin,
        Draw
    }
}
