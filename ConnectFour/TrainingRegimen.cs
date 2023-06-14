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
            return new ConnectFourBoard();
        };
    }
}
