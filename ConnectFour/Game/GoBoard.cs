using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ConnectFour
{
    public class GoBoard : Go.Board, ConnectFour.Board
    {
        public int Rows { get { return this.SizeY; } }
        public int Columns { get { return this.SizeX; } }

        public Tuple<int, int> Move { get; set; }

        public GoBoard(int rows = 19, int columns = 19) : base(rows, columns)
        {
        }

        public GoBoard(Go.Board fromBoard) : base(fromBoard.SizeY, fromBoard.SizeX)
        {
            content = new Go.Content[fromBoard.SizeY, fromBoard.SizeX];
            for (int i = 0; i < SizeX; i++)
            {
                for (int j = 0; j < SizeY; j++)
                {
                    content[i, j] = fromBoard[i, j];
                }
            }
            this.GameInfo = fromBoard.GameInfo;
        }

        public Checker[,] Cells { get; set; }

        public Board MakeMoveOnNewBoard(Checker c, int column, int? r = null)
        {
            Go.Point p = new Go.Point(column, (int)r);
            GoBoard b = new GoBoard(this);
            if (b.InternalMakeMove(p, (Go.Content)c) == Go.MakeMoveResult.Legal)
            {
                Go.Point movePt = ((Go.Board)b).Move.Value;
                b.Move = Tuple.Create(movePt.x, movePt.y);
                return b;
            }
            return null;
        }

        public void AddChecker(Checker c, int column, int? r = null)
        {
            Go.Point p = new Go.Point(column, (int)r);
            Go.Board b = (Go.Board)this;
            b.InternalMakeMove(p, (Go.Content)c);

            ConnectFour.Board board = (ConnectFour.Board)this;
            Go.Point move = b.Move.Value;
            board.Move = Tuple.Create(move.x, move.y);
        }

        public Board MakeRandomMove(Checker c)
        {
            //to implement
            return this;
        }

        public bool TryGetWinner(out Checker winner)
        {
            winner = Checker.Empty;
            if (Move == null) return false;
            Go.Content c = this[Move.Item1, Move.Item2];
            Go.SurviveOrKill surviveOrKill = (Go.GameHelper.GetContentForSurviveOrKill(GameInfo, Go.SurviveOrKill.Survive) == c) ? Go.SurviveOrKill.Survive : Go.SurviveOrKill.Kill;

            Go.ConfirmAliveResult confirmAlive = Go.LifeCheck.CheckIfDeadOrAlive(surviveOrKill, this);

            if (confirmAlive != Go.ConfirmAliveResult.Unknown)
            {
                Boolean win = Go.GameHelper.WinOrLose(surviveOrKill, confirmAlive, GameInfo);
                winner = (Checker)c;
                if (!win) Toggle(winner);
                return true;
            }
            return false;
        }

        public bool IsGameOver
        {
            get
            {
                Checker winner;
                if (TryGetWinner(out winner))
                    return true;
                return false;
            }
        }

        public Checker NextPlayer
        {
            get
            {
                return (Checker)Go.GameHelper.GetContentForNextMove(this);
            }
        }

        public IEnumerable<Board> GetPossibleMoves(Checker c)
        {
            Boolean isSurvive = Go.GameHelper.GetContentForSurviveOrKill(this.GameInfo, Go.SurviveOrKill.Survive) == (Go.Content)c;
            List<Go.Point> moves = (isSurvive) ? this.GameInfo.movablePoints : this.GameInfo.killMovablePoints;
            foreach (Go.Point p in moves)
            {
                if (this[p] != Go.Content.Empty) continue;
                Board b = this.MakeMoveOnNewBoard(c, p.x, p.y);
                if (b == null) continue;
                yield return b;
            }
        }

        public IEnumerable<Checker> CellsInSingleArray()
        {
            for (int y = 8; y <= 18; y++)
            {
                for (int x = 0; x <= 12; x++)
                {
                     yield return (Checker)this[x,y];
                }
            }
        }

        public Checker Toggle(Checker c)
        {
            return c == Checker.White ? Checker.Black : Checker.White;
        }
    }
}
