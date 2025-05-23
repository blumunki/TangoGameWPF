using System;

namespace TangoGame
{
    public class BorderSymbol
    {
        public string Symbol { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsHorizontal { get; set; }

        public BorderSymbol(string symbol, int row, int col, bool isHorizontal)
        {
            Symbol = symbol; 
            Row = row;
            Col = col;
            IsHorizontal = isHorizontal;
        }

        public override bool Equals(object? obj)
        {
            return obj is BorderSymbol symbol &&
                   Symbol == symbol.Symbol &&
                   Row == symbol.Row &&
                   Col == symbol.Col &&
                   IsHorizontal == symbol.IsHorizontal;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Symbol, Row, Col, IsHorizontal);
        }
    }
}
