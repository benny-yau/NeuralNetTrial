using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectFour
{
    public static class TrainingRegimen
    {
        public static Func<Board> Blank = () =>
        {
            return new Board();
        };

        public static Random rand = new Random();
        /// <summary>
        /// Chooses random board position.
        /// </summary>
        public static Func<Board> Random = () =>
        {
            Board board = new Board();
            int checkers = rand.Next(board.Rows * board.Columns+1);
            Checker checker = Checker.Blue;
            int column;
            for (int i = 0; i < checkers && !board.IsGameOver; ++i)
            {
                do
                    column = rand.Next(board.Columns);
                while (board.IsColumnFull(column));
                board.AddChecker(checker, column);
                checker = Board.Toggle(checker);
            }

            return board;
        };
  
    }
}
