using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace ConnectFour
{
    public enum Checker { Empty, Blue, Green }
    public enum GameResult { Draw, Win, Loss }

    /// <summary>
    /// Represents the current state of the game of ConnectFour
    /// </summary>
	public class Board
	{
        public List<int> MoveHistory = new List<int>();
        public int Rows { get { return Cells.GetLength(0); } }
        public int Columns { get { return Cells.GetLength(1); } }
        public Checker[,] Cells;
        private List<Tuple<int,int>> _lastSequence = new List<Tuple<int,int>>();
        public List<Tuple<int, int>> WinningSequence { get { return IsGameOver && _lastSequence.Count >= 4 ? _lastSequence : new List<Tuple<int, int>>(); } } 

        public Board(int rows=6, int columns=7)
        {
            Cells = new Checker[rows,columns];
            for (int i = 0; i < rows; ++i)
                for (int j = 0; j < columns; ++j)
                    Cells[i, j] = Checker.Empty;
        }

        public Checker this[int row, int col]
        {
            get { return Cells[row, col]; }
            set { Cells[row, col] = value; }
        }

		// column should be an integer in [0-6]
        public void AddChecker(Checker checker, int column)
		{
			if (IsColumnFull(column))
			{
                throw new Exception("Column already filled!");
			}
			else
			{
				for (int row = Rows-1; row >= 0; --row)
				{
					if (Cells[row,column] == Checker.Empty)
					{
						Cells[row, column] = checker;
                        MoveHistory.Add(column);
						return;
					}
				}
			}
		}

		// column should be an integer in [0-6]
		public Checker RemoveChecker(int column)
		{
			for (int row = 0; row < Rows; row++)
			{
				Checker checker = Cells[row, column];
				if (checker != Checker.Empty)
				{
                    Cells[row, column] = Checker.Empty;
                    int lastindex = MoveHistory.FindLastIndex(e => e == column);
                    if (lastindex != -1)
                    MoveHistory.RemoveAt(lastindex);
                    
					return checker;
				}
			}
			return 0;
		}


		// column should be an integer in [0-6]
		public bool IsColumnFull(int column)
		{
            return Cells[0, column] != Checker.Empty;
		}

		/// <summary>
        /// Returns the number of matching cells
		/// </summary>
		/// <param name="y">Column to start checking in</param>
		/// <param name="x">Row to start checking in</param>
		/// <param name="dy">Look in the vertical direction</param>
		/// <param name="dx">Look in the horizontal direction</param>
		/// <returns></returns>
		int CheckSequence(int y, int x, int dy, int dx)
		{
			Checker checker = Cells[y,x];
            if (checker == Checker.Empty)
				return 0;

            _lastSequence.Clear();
            _lastSequence.Add(Tuple.Create(y, x));
			for (int num = 1;; num++)
			{
				y += dy;
				x += dx;
				if (y < 0 || y >= Rows)
					return num;
				if (x < 0 || x >= Columns)
					return num;
				if (Cells[y,x] != checker)
					return num;
                _lastSequence.Add(Tuple.Create(y, x));
			}
		}

        /// <summary>
        /// Checks if there is a winner.
        /// </summary>
        public bool TryGetWinner(out Checker winner)
        {
            winner = Checker.Empty;
            for (int y = 0; y < Rows; y++)
            {
                for (int x = 0; x < Columns; x++)
                {
                    // check for Four-in-a-row in
                    // various directions

                    if (CheckSequence(y, x, -1, 1) >= 4)
                        winner = Cells[y, x];
                    else if (CheckSequence(y, x, 0, 1) >= 4)
                        winner = Cells[y, x];
                    else if (CheckSequence(y, x, 1, 1) >= 4)
                        winner = Cells[y, x];
                    else if (CheckSequence(y, x, 1, 0) >= 4)
                        winner = Cells[y, x];
                    if (winner != Checker.Empty)
                        return true;
                }
            }
            // if reached here, then no winner
            return false;

        }

		public bool IsGameOver
		{
            get
            {
                Checker winner;
                if (TryGetWinner(out winner))
                    return true;

                for (int x = 0; x < Columns; x++)
                {
                    if (!IsColumnFull(x))
                        return false;
                }
                // if reached here, then all columns are full
                return true;
            }
		}

        public Checker NextPlayer
        {
            get
            {
                int numBlue = 0, numGreen = 0;
                for (int i = 0; i < Rows; ++i)
                    for (int j = 0; j < Columns; ++j)
                        if (Cells[i, j] == Checker.Blue)
                            ++numBlue;
                        else if (Cells[i, j] == Checker.Green)
                            ++numGreen;
                return (numBlue > numGreen ? Checker.Green : Checker.Blue);
            }
        }

        /// <summary>
        /// Return the highest checker in the given column.
        /// </summary>
        public Checker Peek(int column)
        {
            for (int i = 0; i < Rows; ++i)
            {
                if (Cells[i, column] != Checker.Empty)
                    return Cells[i, column];
            }
            return Checker.Empty;
        }

        public static Checker Toggle(Checker c)
        {
            return c == Checker.Green ? Checker.Blue : Checker.Green;
        }

		public bool isValidCoords(int row, int column)
		{
			return row >= 0 && column >= 0 &&
				row < Rows && column < Columns;
		}

		public string ToStringNormalized(Checker myColor)
		{
			string str = string.Empty;
			for (int row = 0; row < Rows; row++)
			{
				if (row > 0)
					str += "\r\n";
				for (int col = 0; col < Columns; col++)
				{
					str += Cells[row,col] == myColor ? "x" :
						Cells[row,col] == Checker.Empty ? "-" :
						"o";
				}
			}
			return str;
		}

        public List<int> LineOfX(Checker myColor, bool potential = false)
        {
            int[] counts = new int[8];
            int[,] lengths = new int[3,3];
            int[] sumLengths = new int[4];

            for (int i=0; i<Rows; ++i)
                for (int j = 0; j < Columns; ++j)
                    if (potential && Cells[i, j] == Checker.Empty && (i == Rows-1 || Cells[i+1,j] != Checker.Empty) 
                    || !potential && Cells[i, j] == myColor)
                    {
                        if (potential) Cells[i, j] = myColor;

                        for (int di = -1; di < 2; ++di)
                            for (int dj = -1; dj < 2; ++dj)
                            {
                                if (di == 0 && dj == 0)
                                    continue;
                                lengths[di+1,dj+1] = CheckSequence(i, j, di, dj); 
                            }
                        sumLengths[0] = lengths[0, 0] + lengths[2, 2] - 1; // diag1
                        sumLengths[1] = lengths[0, 2] + lengths[2, 0] - 1; // diag2
                        sumLengths[2] = lengths[0, 1] + lengths[2, 1] - 1; // vertical
                        sumLengths[3] = lengths[1, 0] + lengths[1, 2] - 1; // horizontal
                        for (int k = 0; k < 4; ++k)
                        {
                            ++counts[sumLengths[k]]; 
                        }
                        if (potential) Cells[i, j] = Checker.Empty;
                    }
            if (!potential)
            {
                for (int i = 1; i < counts.Length; ++i)
                    counts[i] /= i; // Do this because every line of x is counted x times
            }
            return new List<int>{ counts[2], counts[3], counts[4] + counts[5] + counts[6] + counts[7]};
        }

        public List<int> NumbersInColumns(Checker myColor)
        {
            List<int> cols = Enumerable.Repeat(0, Columns).ToList();
            for (int i = 0; i < Rows; ++i)
                for (int j = 0; j < Columns; ++j)
                    if (Cells[i, j] == myColor)
                        ++cols[j];
            return cols;
        }

        public List<int> NumbersInRows(Checker myColor)
        {
            List<int> rows = Enumerable.Repeat(0, Rows).ToList();
            for (int i = 0; i < Rows; ++i)
                for (int j = 0; j < Columns; ++j)
                    if (Cells[i, j] == myColor)
                        ++rows[i];
            return rows;
        }

        public int NumberOnBoard(Checker myColor)
        {
            int num = 0;
            for (int i = 0; i < Rows; ++i)
                for (int j = 0; j < Columns; ++j)
                    if (Cells[i, j] == myColor)
                        ++num;
            return num;
        }

        public override string ToString()
        {
            string str = string.Empty;
            for (int i = 0; i < Rows; ++i)
            {
                for (int j = 0; j < Columns; ++j)
                    str += Cells[i, j] == Checker.Blue ? "x" : Cells[i, j] == Checker.Green ? "o" : " ";
                str += "\r\n";
            }
            return str;
        }

	}
}
