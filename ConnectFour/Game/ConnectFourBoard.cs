using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectFour
{
    public class ConnectFourBoard : Board
    {
        public Checker[,] Cells { get; set; }
        public int Rows { get { return Cells.GetLength(0); } }
        public int Columns { get { return Cells.GetLength(1); } }

        public static Random rand = new Random();

        public Tuple<int, int> Move { get; set; }

        private List<Tuple<int, int>> _lastSequence = new List<Tuple<int, int>>();
        public List<Tuple<int, int>> WinningSequence { get { return IsGameOver && _lastSequence.Count >= 4 ? _lastSequence : new List<Tuple<int, int>>(); } }

        public ConnectFourBoard(int rows = 6, int columns = 7)
        {
            Cells = new Checker[rows, columns];
        }

        public ConnectFourBoard(ConnectFourBoard fromBoard)
        {
            Cells = new Checker[fromBoard.Rows, fromBoard.Columns];
            Array.Copy(fromBoard.Cells, Cells, Cells.Length);
        }


        public Checker this[int row, int col]
        {
            get { return Cells[row, col]; }
            set { Cells[row, col] = value; }
        }        

        public void AddChecker(Checker checker, int column, int? r = null)
        {
            if (IsColumnFull(column))
            {
                throw new Exception("Column already filled!");
            }
            else
            {
                for (int row = Rows - 1; row >= 0; --row)
                {
                    if (Cells[row, column] == Checker.Empty)
                    {
                        Cells[row, column] = checker;
                        Move = Tuple.Create(column, row);
                        return;
                    }
                }
            }
        }

        public Board MakeMoveOnNewBoard(Checker c, int column, int? r = null)
        {
            if (IsColumnFull(column)) return null;
            Board board = new ConnectFourBoard(this);
            board.AddChecker(c, column);
            return board;
        }

        public Board MakeRandomMove(Checker c)
        {
            int[] cols = Enumerable.Range(0, Columns).Where(x => !IsColumnFull(x)).ToArray();
            int column = cols[rand.Next(cols.Count())];
            Board board = new ConnectFourBoard(this);
            board.AddChecker(c, column);
            return board;
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

        public bool IsColumnFull(int column)
        {
            return Cells[0, column] != Checker.Empty;
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

        public Checker NextPlayer
        {
            get
            {
                int numBlack = 0, numWhite = 0;
                for (int i = 0; i < Rows; ++i)
                    for (int j = 0; j < Columns; ++j)
                        if (Cells[i, j] == Checker.Black)
                            ++numBlack;
                        else if (Cells[i, j] == Checker.White)
                            ++numWhite;
                return (numBlack > numWhite ? Checker.White : Checker.Black);
            }
        }

        public Checker Toggle(Checker c)
        {
            return c == Checker.White ? Checker.Black : Checker.White;
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
            Checker checker = Cells[y, x];
            if (checker == Checker.Empty)
                return 0;

            _lastSequence.Clear();
            _lastSequence.Add(Tuple.Create(y, x));
            for (int num = 1; ; num++)
            {
                y += dy;
                x += dx;
                if (y < 0 || y >= Rows)
                    return num;
                if (x < 0 || x >= Columns)
                    return num;
                if (Cells[y, x] != checker)
                    return num;
                _lastSequence.Add(Tuple.Create(y, x));
            }
        }

        public List<int> LineOfX(Checker myColor, bool potential = false)
        {
            int[] counts = new int[8];
            int[,] lengths = new int[3, 3];
            int[] sumLengths = new int[4];

            for (int i = 0; i < Rows; ++i)
                for (int j = 0; j < Columns; ++j)
                    if (potential && Cells[i, j] == Checker.Empty && (i == Rows - 1 || Cells[i + 1, j] != Checker.Empty)
                    || !potential && Cells[i, j] == myColor)
                    {
                        if (potential) Cells[i, j] = myColor;

                        for (int di = -1; di < 2; ++di)
                            for (int dj = -1; dj < 2; ++dj)
                            {
                                if (di == 0 && dj == 0)
                                    continue;
                                lengths[di + 1, dj + 1] = CheckSequence(i, j, di, dj);
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
            return new List<int> { counts[2], counts[3], counts[4] + counts[5] + counts[6] + counts[7] };
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

        public IEnumerable<Checker> CellsInSingleArray()
        {
            return this.Cells.Cast<Checker>();
        }

        public IEnumerable<Board> GetPossibleMoves(Checker c)
        {
            for (int x = 0; x < this.Columns; x++)
            {
                Board b = this.MakeMoveOnNewBoard(c, x);
                if (b == null) continue;
                yield return b;
            }
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
                    str += Cells[row, col] == myColor ? "x" :
                        Cells[row, col] == Checker.Empty ? "-" :
                        "o";
                }
            }
            return str;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('\n');
            for (int i = 0; i < Rows; i++)
            {
                sb.Append("[ ");
                for (int j = 0; j < Columns; j++)
                {
                    String v = Cells[i, j] == Checker.Black ? "x" : Cells[i, j] == Checker.White ? "o" : " ";
                    sb.Append(v.ToString());
                    if (j < Columns - 1)
                        sb.Append(" | ");
                }
                sb.Append(" ]");
                sb.Append('\n');
            }
            return sb.ToString();
        }

    }
}
