using ChessModule.Core;
using ChessModule.Models;
using ChessModule.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using FontAwesome.Sharp;

namespace ChessModule
{
    public partial class MainWindow : Window
    {
        private readonly Button[,] squareButtons = new Button[8, 8];
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private ChessGame game;
        private GameOptions options;
        private PlayerSettings playerSettings;
        private VisualCustomization visualCustomization = new VisualCustomization();
        private int? selectedRow;
        private int? selectedCol;
        private List<ChessMove> selectedLegalMoves = new List<ChessMove>();
        private int whiteTimeSeconds;
        private int blackTimeSeconds;
        private DateTime startedAt;
        private bool gameFinished;
        private MediaPlayer movePlayer;

        public MainWindow()
            : this(new GameOptions())
        {
        }

        public MainWindow(GameOptions options)
        {
            InitializeComponent();
            this.options = options ?? new GameOptions();
            playerSettings = SettingsService.GetSettings();
            LoadMoveSound();

            game = new ChessGame();

            if (!string.IsNullOrWhiteSpace(this.options.LoadedFen))
            {
                game.LoadFen(this.options.LoadedFen);
                game.SetMoveCount(this.options.MoveCount);
                game.SetMoveLog(this.options.LoadedMoveLog);
            }
            whiteTimeSeconds = this.options.WhiteTimeSeconds > 0
                ? this.options.WhiteTimeSeconds
                : GameOptions.GetStartSeconds(this.options.TimeMode);

            blackTimeSeconds = this.options.BlackTimeSeconds > 0
                ? this.options.BlackTimeSeconds
                : GameOptions.GetStartSeconds(this.options.TimeMode);

            startedAt = DateTime.Now;
            BuildBoard();
            ApplyVisualSettings();
            RefreshBoard();
            RefreshInfo();

            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += Timer_Tick;
            if (this.options.TimeMode != GameTimeMode.NoLimit)
                timer.Start();

            if (!IsHumanTurn())
                Dispatcher.BeginInvoke(new Action(MakeBotMove), DispatcherPriority.Background);
        }

        private void LoadMoveSound()
        {
            try
            {
                string path = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Sounds",
                    "move.mp3"
                );

                if (!System.IO.File.Exists(path))
                    return;

                movePlayer = new MediaPlayer();
                movePlayer.Open(new Uri(path, UriKind.Absolute));
                movePlayer.Volume = 0.7;
            }
            catch
            {
                movePlayer = null;
            }
        }
        private void BuildBoard()
        {
            BoardGrid.Children.Clear();

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Button button = new Button();
                    button.Tag = r + ";" + c;
                    button.FontSize = 52;
                    button.FontWeight = FontWeights.Bold;
                    button.Margin = new Thickness(0);
                    button.Padding = new Thickness(0);
                    button.BorderThickness = new Thickness(0);
                    button.HorizontalContentAlignment = HorizontalAlignment.Center;
                    button.VerticalContentAlignment = VerticalAlignment.Center;
                    button.Focusable = false;
                    button.Click += Square_Click;

                    squareButtons[r, c] = button;
                    BoardGrid.Children.Add(button);
                }
            }
        }
        private void ApplyVisualSettings()
        {
            visualCustomization = RewardService.GetVisualCustomization();
            RootGrid.LayoutTransform = null;
            ThemeService.ApplyTheme(this, RootGrid);
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (gameFinished || options.TimeMode == GameTimeMode.NoLimit)
                return;

            if (game.SideToMove == PieceColor.White)
                whiteTimeSeconds--;
            else
                blackTimeSeconds--;

            if (whiteTimeSeconds <= 0)
            {
                FinishGame(GameResult.BlackWin, "У белых закончилось время");
                return;
            }

            if (blackTimeSeconds <= 0)
            {
                FinishGame(GameResult.WhiteWin, "У чёрных закончилось время");
                return;
            }

            RefreshInfo();
        }

        private void Square_Click(object sender, RoutedEventArgs e)
        {
            if (gameFinished || !IsHumanTurn())
                return;

            Button button = (Button)sender;
            string[] parts = button.Tag.ToString().Split(';');
            int row = int.Parse(parts[0]);
            int col = int.Parse(parts[1]);

            ChessPiece piece = game.Board[row, col];

            if (!selectedRow.HasValue)
            {
                SelectSquare(row, col, piece);
                return;
            }

            if (piece != null && piece.Color == game.SideToMove)
            {
                SelectSquare(row, col, piece);
                return;
            }

            ChessMove move = selectedLegalMoves.Find(m => m.ToRow == row && m.ToCol == col);
            if (move == null)
            {
                ClearSelection();
                RefreshBoard();
                return;
            }

            string error;
            if (game.TryMakeMove(move, out error))
            {
                PlayMoveSound();
                AfterMove();
            }
            else
            {
                MessageBox.Show(error);
            }
        }

        private void SelectSquare(int row, int col, ChessPiece piece)
        {
            if (piece == null || piece.Color != game.SideToMove)
                return;

            selectedRow = row;
            selectedCol = col;
            selectedLegalMoves = game.GetLegalMovesForSquare(row, col);
            RefreshBoard();
        }

        private void ClearSelection()
        {
            selectedRow = null;
            selectedCol = null;
            selectedLegalMoves.Clear();
        }

        private void AfterMove()
        {
            ClearSelection();
            RefreshBoard();
            RefreshInfo();

            if (CheckGameEnd())
                return;

            if (!IsHumanTurn())
                Dispatcher.BeginInvoke(new Action(MakeBotMove), DispatcherPriority.Background);
        }

        private void MakeBotMove()
        {
            if (gameFinished || IsHumanTurn())
                return;

            ChessMove botMove = ChessBot.ChooseMove(game, options.BotDifficulty, game.SideToMove);
            if (botMove == null)
            {
                CheckGameEnd();
                return;
            }

            string error;
            if (game.TryMakeMove(botMove, out error))
            {
                PlayMoveSound();
                AfterMove();
            }
        }

        private bool IsHumanTurn()
        {
            if (options.Mode == GameMode.HumanVsHuman)
                return true;

            return game.SideToMove == PieceColor.White;
        }

        private void RefreshBoard()
        {
            Brush light;
            Brush dark;
            GetBoardBrushes(out light, out dark);

            Brush selected = Brushes.Gold;
            Brush legal = Brushes.LightGreen;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    Button button = squareButtons[r, c];
                    button.Background = (r + c) % 2 == 0 ? light : dark;

                    if (selectedRow == r && selectedCol == c)
                        button.Background = selected;
                    else if (selectedLegalMoves.Exists(m => m.ToRow == r && m.ToCol == c))
                        button.Background = legal;

                    ChessPiece piece = game.Board[r, c];
                    button.Content = piece == null ? string.Empty : BuildPieceContent(piece);
                    button.Foreground = piece != null ? GetPieceBrush(piece.Color) : Brushes.Black;
                    button.FontSize = GetPieceFontSize();
                    button.FontFamily = new FontFamily("Segoe UI");
                    button.ToolTip = playerSettings.ShowCoordinates ? ChessMove.ToSquareName(r, c) : null;
                }
            }

            MovesList.ItemsSource = null;
            MovesList.ItemsSource = game.MoveLog;
        }

        private void GetBoardBrushes(out Brush light, out Brush dark)
        {
            light = new SolidColorBrush(Color.FromRgb(245, 235, 215));
            dark = new SolidColorBrush(Color.FromRgb(105, 70, 45));

            string boardColor = visualCustomization == null ? string.Empty : visualCustomization.BoardColor;

            switch (boardColor)
            {
                case "Зелёная доска":
                    light = new SolidColorBrush(Color.FromRgb(232, 245, 220));
                    dark = new SolidColorBrush(Color.FromRgb(60, 120, 70));
                    return;

                case "Синяя доска":
                    light = new SolidColorBrush(Color.FromRgb(220, 235, 250));
                    dark = new SolidColorBrush(Color.FromRgb(45, 85, 145));
                    return;

                case "Золотая доска":
                    light = new SolidColorBrush(Color.FromRgb(255, 238, 170));
                    dark = new SolidColorBrush(Color.FromRgb(150, 95, 25));
                    return;

                case "Турнирная доска":
                    light = new SolidColorBrush(Color.FromRgb(235, 238, 230));
                    dark = new SolidColorBrush(Color.FromRgb(80, 95, 105));
                    return;
            }
        }
        private object BuildPieceContent(ChessPiece piece)
        {
            string shape = visualCustomization == null ? string.Empty : visualCustomization.PieceShape;

            if (shape == "Буквенная форма")
                return BuildTextPiece(GetPieceLetter(piece), piece, 54, true);

            if (shape == "Круглая форма")
                return BuildRoundIconPiece(piece);

            return BuildFontAwesomePiece(piece, 64, true);
        }

        private IconBlock BuildFontAwesomePiece(ChessPiece piece, double size, bool withShadow)
        {
            IconBlock icon = new IconBlock();
            icon.Icon = GetPieceIcon(piece.Type);
            icon.FontSize = size;
            icon.Width = size + 8;
            icon.Height = size + 8;
            icon.Foreground = GetPieceBrush(piece.Color);
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.TextAlignment = TextAlignment.Center;

            if (withShadow)
            {
                icon.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = piece.Color == PieceColor.White ? Colors.Black : Colors.White,
                    BlurRadius = 4,
                    ShadowDepth = 0,
                    Opacity = 0.65
                };
            }

            return icon;
        }

        private Grid BuildRoundIconPiece(ChessPiece piece)
        {
            Grid grid = new Grid();
            grid.Width = 64;
            grid.Height = 64;
            grid.HorizontalAlignment = HorizontalAlignment.Center;
            grid.VerticalAlignment = VerticalAlignment.Center;
            grid.ClipToBounds = false;

            Ellipse ellipse = new Ellipse();
            ellipse.Width = 60;
            ellipse.Height = 60;
            ellipse.Fill = GetPieceBrush(piece.Color);
            ellipse.Stroke = GetPieceStrokeBrush(piece.Color);
            ellipse.StrokeThickness = 2;
            ellipse.HorizontalAlignment = HorizontalAlignment.Center;
            ellipse.VerticalAlignment = VerticalAlignment.Center;

            IconBlock icon = new IconBlock();
            icon.Icon = GetPieceIcon(piece.Type);
            icon.FontSize = 34;
            icon.Width = 44;
            icon.Height = 44;
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.VerticalAlignment = VerticalAlignment.Center;
            icon.TextAlignment = TextAlignment.Center;
            icon.Foreground = piece.Color == PieceColor.White ? Brushes.Black : Brushes.White;

            grid.Children.Add(ellipse);
            grid.Children.Add(icon);

            return grid;
        }

        private IconChar GetPieceIcon(PieceType type)
        {
            switch (type)
            {
                case PieceType.King:
                    return IconChar.ChessKing;

                case PieceType.Queen:
                    return IconChar.ChessQueen;

                case PieceType.Rook:
                    return IconChar.ChessRook;

                case PieceType.Bishop:
                    return IconChar.ChessBishop;

                case PieceType.Knight:
                    return IconChar.ChessKnight;

                case PieceType.Pawn:
                    return IconChar.ChessPawn;

                default:
                    return IconChar.None;
            }
        }

        private string GetPieceLetter(ChessPiece piece)
        {
            switch (piece.Type)
            {
                case PieceType.King:
                    return "K";

                case PieceType.Queen:
                    return "Q";

                case PieceType.Rook:
                    return "R";

                case PieceType.Bishop:
                    return "B";

                case PieceType.Knight:
                    return "N";

                case PieceType.Pawn:
                    return "P";

                default:
                    return string.Empty;
            }
        }

        private Brush GetPieceBrush(PieceColor color)
        {
            string pieceColor = visualCustomization == null ? string.Empty : visualCustomization.PieceColor;

            if (pieceColor == "Синие фигуры")
            {
                return color == PieceColor.White
                    ? new SolidColorBrush(Color.FromRgb(235, 248, 255))
                    : new SolidColorBrush(Color.FromRgb(10, 45, 120));
            }

            if (pieceColor == "Красные фигуры")
            {
                return color == PieceColor.White
                    ? new SolidColorBrush(Color.FromRgb(255, 235, 235))
                    : new SolidColorBrush(Color.FromRgb(140, 20, 20));
            }

            if (pieceColor == "Золотые фигуры")
            {
                return color == PieceColor.White
                    ? new SolidColorBrush(Color.FromRgb(255, 226, 95))
                    : new SolidColorBrush(Color.FromRgb(115, 70, 8));
            }

            if (pieceColor == "Алмазные фигуры")
            {
                return color == PieceColor.White
                    ? new SolidColorBrush(Color.FromRgb(230, 255, 255))
                    : new SolidColorBrush(Color.FromRgb(0, 95, 125));
            }

            return color == PieceColor.White ? Brushes.White : Brushes.Black;
        }

        private Brush GetPieceStrokeBrush(PieceColor color)
        {
            string pieceColor = visualCustomization == null ? string.Empty : visualCustomization.PieceColor;

            if (pieceColor == "Синие фигуры")
            {
                return color == PieceColor.White
                    ? new SolidColorBrush(Color.FromRgb(10, 45, 120))
                    : new SolidColorBrush(Color.FromRgb(210, 235, 255));
            }

            if (pieceColor == "Красные фигуры")
            {
                return color == PieceColor.White
                    ? new SolidColorBrush(Color.FromRgb(140, 20, 20))
                    : new SolidColorBrush(Color.FromRgb(255, 220, 220));
            }

            if (pieceColor == "Золотые фигуры")
            {
                return color == PieceColor.White
                    ? new SolidColorBrush(Color.FromRgb(115, 70, 8))
                    : new SolidColorBrush(Color.FromRgb(255, 226, 95));
            }

            if (pieceColor == "Алмазные фигуры")
            {
                return color == PieceColor.White
                    ? new SolidColorBrush(Color.FromRgb(0, 95, 125))
                    : new SolidColorBrush(Color.FromRgb(210, 250, 255));
            }

            return color == PieceColor.White ? Brushes.Black : Brushes.White;
        }

        private double GetPieceFontSize()
        {
            string shape = visualCustomization == null ? string.Empty : visualCustomization.PieceShape;

            if (shape == "Буквенная форма")
                return 54;

            return 12;
        }
        private Viewbox BuildVectorPieceContent(ChessPiece piece, bool classicStyle)
        {
            Canvas canvas = new Canvas();
            canvas.Width = 100;
            canvas.Height = 100;

            AddPiecePath(canvas, piece, classicStyle, GetPieceBrush(piece.Color), GetPieceStrokeBrush(piece.Color));

            Viewbox viewbox = new Viewbox();
            viewbox.Width = 72;
            viewbox.Height = 72;
            viewbox.Stretch = Stretch.Uniform;
            viewbox.Child = canvas;

            return viewbox;
        }

        private void AddPiecePath(Canvas canvas, ChessPiece piece, bool classicStyle, Brush fill, Brush stroke)
        {
            Path path = new Path();
            path.Data = Geometry.Parse(GetPieceGeometryData(piece.Type, classicStyle));
            path.Fill = fill;
            path.Stroke = stroke;
            path.StrokeThickness = classicStyle ? 2.2 : 3.4;
            path.StrokeLineJoin = PenLineJoin.Round;
            path.StrokeStartLineCap = PenLineCap.Round;
            path.StrokeEndLineCap = PenLineCap.Round;
            path.Stretch = Stretch.Fill;
            path.Width = 100;
            path.Height = 100;

            path.Effect = new System.Windows.Media.Effects.DropShadowEffect
            {
                Color = piece.Color == PieceColor.White ? Colors.Black : Colors.White,
                BlurRadius = 3,
                ShadowDepth = 0,
                Opacity = 0.55
            };

            canvas.Children.Add(path);
        }

        private string GetPieceGeometryData(PieceType type, bool classicStyle)
        {
            if (!classicStyle)
                return GetMinimalPieceGeometryData(type);

            switch (type)
            {
                case PieceType.King:
                    return "M47,5 L53,5 L53,17 L64,17 L64,23 L53,23 L53,34 C65,38 73,49 73,63 C73,72 67,79 59,82 L72,89 L72,95 L28,95 L28,89 L41,82 C33,79 27,72 27,63 C27,49 35,38 47,34 L47,23 L36,23 L36,17 L47,17 Z M50,42 C40,46 34,54 34,63 C34,71 41,77 50,77 C59,77 66,71 66,63 C66,54 60,46 50,42 Z";

                case PieceType.Queen:
                    return "M18,92 L82,92 L78,82 L22,82 Z M26,78 L74,78 L68,36 L56,68 L50,28 L44,68 L32,36 Z M17,25 A8,8 0 1 1 33,25 A8,8 0 1 1 17,25 M42,18 A8,8 0 1 1 58,18 A8,8 0 1 1 42,18 M67,25 A8,8 0 1 1 83,25 A8,8 0 1 1 67,25";

                case PieceType.Rook:
                    return "M25,92 L75,92 L75,82 L25,82 Z M31,78 L69,78 L65,38 L35,38 Z M27,34 L73,34 L73,18 L62,18 L62,28 L54,28 L54,18 L46,18 L46,28 L38,28 L38,18 L27,18 Z";

                case PieceType.Bishop:
                    return "M27,92 L73,92 L73,84 L27,84 Z M34,80 L66,80 C61,67 60,54 68,43 C62,29 52,21 50,10 C48,21 38,29 32,43 C40,54 39,67 34,80 Z M50,30 C55,36 58,43 58,50 C58,61 54,69 50,73 C46,69 42,61 42,50 C42,43 45,36 50,30 Z M59,37 L42,58";

                case PieceType.Knight:
                    return "M24,92 L78,92 L78,82 L33,82 C36,68 43,55 55,45 C49,43 43,38 39,31 C37,39 30,45 22,47 C24,33 33,20 49,14 C57,11 68,15 72,25 C78,40 72,54 63,65 C58,70 55,75 54,82 L78,82 L78,92 Z M45,27 A4,4 0 1 1 53,27 A4,4 0 1 1 45,27";

                case PieceType.Pawn:
                    return "M28,92 L72,92 L72,84 L28,84 Z M34,80 L66,80 L60,66 C70,58 70,43 61,34 C52,25 38,31 35,43 C32,53 37,61 45,66 Z M41,45 A9,9 0 1 1 59,45 A9,9 0 1 1 41,45";

                default:
                    return string.Empty;
            }
        }

        private string GetMinimalPieceGeometryData(PieceType type)
        {
            switch (type)
            {
                case PieceType.King:
                    return "M46,8 L54,8 L54,24 L68,24 L68,32 L54,32 L54,46 L72,72 L72,92 L28,92 L28,72 L46,46 L46,32 L32,32 L32,24 L46,24 Z";

                case PieceType.Queen:
                    return "M18,30 L34,70 L50,22 L66,70 L82,30 L74,92 L26,92 Z";

                case PieceType.Rook:
                    return "M24,18 L76,18 L76,38 L68,38 L68,76 L76,76 L76,92 L24,92 L24,76 L32,76 L32,38 L24,38 Z";

                case PieceType.Bishop:
                    return "M50,10 C68,30 72,52 58,72 L74,92 L26,92 L42,72 C28,52 32,30 50,10 Z M60,34 L38,62";

                case PieceType.Knight:
                    return "M24,92 L78,92 L78,76 L56,76 C58,62 74,54 70,34 C67,18 50,10 34,20 C28,24 23,33 20,44 C30,42 38,36 42,28 C45,38 51,45 60,48 C46,56 36,70 32,92 Z";

                case PieceType.Pawn:
                    return "M30,92 L70,92 L70,78 L58,78 L58,64 C68,58 68,42 58,34 C50,28 38,34 36,46 C34,54 38,61 46,64 L46,78 L30,78 Z";

                default:
                    return string.Empty;
            }
        }

        private void RefreshInfo()
        {
            ModeText.Text = GameOptions.GetModeName(options.Mode) + " | " +
                            GameOptions.GetTimeModeName(options.TimeMode) +
                            (options.Mode == GameMode.HumanVsBot ? " | " + GameOptions.GetBotName(options.BotDifficulty) : string.Empty);

            StatusText.Text = "Ход: " + (game.SideToMove == PieceColor.White ? "белые" : "чёрные") +
                              (game.IsKingInCheck(game.SideToMove) ? " | шах" : string.Empty);

            WhiteTimeText.Text = "Белые: " + FormatTime(whiteTimeSeconds);
            BlackTimeText.Text = "Чёрные: " + FormatTime(blackTimeSeconds);
            MoveCounterText.Text = "Сделано ходов: " + game.MoveCount;
        }

        private string FormatTime(int seconds)
        {
            if (options.TimeMode == GameTimeMode.NoLimit)
                return "без лимита";

            if (seconds < 0)
                seconds = 0;

            TimeSpan span = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}", (int)span.TotalMinutes, span.Seconds);
        }

        private bool CheckGameEnd()
        {
            PieceColor side = game.SideToMove;

            if (game.IsCheckmate(side))
            {
                GameResult result = side == PieceColor.White ? GameResult.BlackWin : GameResult.WhiteWin;
                FinishGame(result, "Мат");
                return true;
            }

            if (game.IsStalemate(side))
            {
                FinishGame(GameResult.Draw, "Пат");
                return true;
            }

            if (game.HalfMoveClock >= 100)
            {
                FinishGame(GameResult.Draw, "Правило 50 ходов");
                return true;
            }

            if (HasOnlyKings())
            {
                FinishGame(GameResult.Draw, "Недостаточно материала для мата");
                return true;
            }

            return false;
        }

        private bool HasOnlyKings()
        {
            int piecesCount = 0;

            for (int r = 0; r < 8; r++)
            {
                for (int c = 0; c < 8; c++)
                {
                    ChessPiece piece = game.Board[r, c];

                    if (piece == null)
                        continue;

                    piecesCount++;

                    if (piece.Type != PieceType.King)
                        return false;
                }
            }

            return piecesCount == 2;
        }

        private void FinishGame(GameResult result, string reason)
        {
            if (gameFinished)
                return;

            gameFinished = true;
            timer.Stop();
            RefreshBoard();
            RefreshInfo();

            int duration = (int)(DateTime.Now - startedAt).TotalSeconds;
            bool savedToDatabase = true;

            try
            {
                GameService.AddFinishedChessGame(options, result, game.MoveCount, duration, reason);
            }
            catch (Exception ex)
            {
                savedToDatabase = false;
                MessageBox.Show("Партия завершена, но результат не удалось записать в базу данных: " + ex.Message);
            }

            if (options.LoadedSaveId.HasValue)
            {
                try
                {
                    SavedGameService.DeleteSave(options.LoadedSaveId.Value);
                    options.LoadedSaveId = null;
                }
                catch
                {
                }
            }

            string message = reason + ". ";
            if (result == GameResult.WhiteWin)
                message += "Победили белые.";
            else if (result == GameResult.BlackWin)
                message += "Победили чёрные.";
            else
                message += "Ничья.";

            StatusText.Text = message;
            ShowGameResultDialog(message, savedToDatabase);
        }

        private void ShowGameResultDialog(string message, bool savedToDatabase)
        {
            Window dialog = new Window();
            dialog.Title = "Партия завершена";
            dialog.Width = 460;
            dialog.Height = 300;
            dialog.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dialog.Owner = this;
            dialog.ResizeMode = ResizeMode.NoResize;
            dialog.Background = new SolidColorBrush(Color.FromRgb(234, 241, 248));

            StackPanel panel = new StackPanel();
            panel.Margin = new Thickness(30);

            IconBlock icon = new IconBlock();
            icon.Icon = IconChar.ChessQueen;
            icon.FontSize = 54;
            icon.Foreground = new SolidColorBrush(Color.FromRgb(36, 59, 85));
            icon.HorizontalAlignment = HorizontalAlignment.Center;
            icon.Margin = new Thickness(0, 0, 0, 10);

            TextBlock title = new TextBlock();
            title.Text = "Партия завершена";
            title.FontSize = 26;
            title.FontWeight = FontWeights.Bold;
            title.Foreground = new SolidColorBrush(Color.FromRgb(36, 59, 85));
            title.HorizontalAlignment = HorizontalAlignment.Center;
            title.Margin = new Thickness(0, 0, 0, 15);

            TextBlock text = new TextBlock();
            text.Text = message + (savedToDatabase
            ? "\nРезультат записан в статистику."
            : "\nРезультат не удалось записать в статистику."); text.FontSize = 17;
            text.TextAlignment = TextAlignment.Center;
            text.TextWrapping = TextWrapping.Wrap;
            text.Margin = new Thickness(0, 0, 0, 25);

            Button ok = new Button();
            ok.Content = "ОК";
            ok.Height = 42;
            ok.Width = 160;
            ok.HorizontalAlignment = HorizontalAlignment.Center;
            ok.Click += (s, e) => dialog.Close();

            panel.Children.Add(icon);
            panel.Children.Add(title);
            panel.Children.Add(text);
            panel.Children.Add(ok);

            dialog.Content = panel;
            dialog.ShowDialog();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (gameFinished)
            {
                MessageBox.Show("Завершённую партию сохранять не нужно.");
                return;
            }

            SavedGameService.SaveGame(options, game, whiteTimeSeconds, blackTimeSeconds);
            MessageBox.Show("Партия сохранена. Её можно продолжить из профиля.");
        }

        private void Resign_Click(object sender, RoutedEventArgs e)
        {
            if (gameFinished)
                return;

            GameResult result = game.SideToMove == PieceColor.White ? GameResult.BlackWin : GameResult.WhiteWin;
            FinishGame(result, "Сдача партии");
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (!gameFinished && game != null)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Партия ещё не завершена. Сохранить её перед выходом?",
                    "Сохранение партии",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (result == MessageBoxResult.Yes)
                    SavedGameService.SaveGame(options, game, whiteTimeSeconds, blackTimeSeconds);
            }

            timer.Stop();
            base.OnClosing(e);
        }

        private void PlayMoveSound()
        {
            if (playerSettings == null || !playerSettings.EffectsEnabled)
                return;

            if (movePlayer == null)
                return;

            try
            {
                movePlayer.Stop();
                movePlayer.Position = TimeSpan.Zero;
                movePlayer.Play();
            }
            catch
            {
            }
        }
        private TextBlock BuildTextPiece(string text, ChessPiece piece, double fontSize, bool withShadow)
        {
            TextBlock textBlock = new TextBlock();
            textBlock.Text = text;
            textBlock.FontSize = fontSize;
            textBlock.FontWeight = FontWeights.Bold;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.Foreground = GetPieceBrush(piece.Color);

            if (withShadow)
            {
                textBlock.Effect = new System.Windows.Media.Effects.DropShadowEffect
                {
                    Color = piece.Color == PieceColor.White ? Colors.Black : Colors.White,
                    BlurRadius = 4,
                    ShadowDepth = 0,
                    Opacity = 0.85
                };
            }

            return textBlock;
        }
    }
    }

