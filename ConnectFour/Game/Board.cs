using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectFour
{
    public enum GameType { ConnectFour, Go }
    public enum Checker { Empty, Black, White, Unknown }
    public enum GameResult { Draw, Win, Loss }

    /// <summary>
    /// Represents the current state of the game of ConnectFour
    /// </summary>
	public interface Board
    {
        Checker[,] Cells { get; set; }

        void AddChecker(Checker checker, int column, int? row = null);
        Board MakeMoveOnNewBoard(Checker c, int column, int? row = null);
        Board MakeRandomMove(Checker c);

        Tuple<int, int> Move { get; set; }

        bool TryGetWinner(out Checker winner);
        bool IsGameOver { get; }
        Checker NextPlayer { get; }

        Checker Toggle(Checker c);
        IEnumerable<Checker> CellsInSingleArray();
        IEnumerable<Board> GetPossibleMoves(Checker c);

        int Rows { get; }
        int Columns { get; }
    }
}
