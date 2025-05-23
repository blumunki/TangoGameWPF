using System;
using System.Linq;
using System.Collections.Generic;

namespace TangoGame
{
    public class GameLogic
    {
        public const int GridSize = 6;
        public const string Sun = "☀️";
        public const string Moon = "🌙";

        private int[,] clickCounts;
        private List<BorderSymbol> borderSymbols; // Holds all potential border symbols based on the solution
        private Random random;
        private string[,] completePuzzle; // The solved state of the puzzle
        private List<BorderSymbol> revealedBorderSymbols; // Symbols currently visible to the player
        private string _currentDifficulty;

        public GameLogic()
        {
            clickCounts = new int[GridSize, GridSize];
            completePuzzle = new string[GridSize, GridSize];
            revealedBorderSymbols = new List<BorderSymbol>();
            borderSymbols = new List<BorderSymbol>();
            random = new Random();
            _currentDifficulty = "Medium"; // Default difficulty
            // InitializeGameCore(); // Not strictly needed if constructor does full init.
        }

        // Not strictly necessary if constructor handles all init, but kept for structure if needed later.
        public void InitializeGameCore()
        {
            clickCounts = new int[GridSize, GridSize];
            completePuzzle = new string[GridSize, GridSize]; 
            borderSymbols.Clear();
            revealedBorderSymbols.Clear();
            // _currentDifficulty remains as set by constructor or SetDifficulty
        }

        public void GeneratePuzzle(string difficulty)
        {
            _currentDifficulty = difficulty;
            ResetGameCore(); // Resets counts, puzzle, symbols lists

            // completePuzzle is already new'd in ResetGameCore or constructor
            if (!GenerateValidPuzzleRecursive(completePuzzle, 0, 0))
            {
                Console.WriteLine("Failed to generate a valid puzzle!");
                // In a real application, might throw an exception or have a fallback.
                return; 
            }
            GenerateHintSymbolsFromSolution(completePuzzle); 
            // revealedBorderSymbols is already cleared in ResetGameCore
        }

        private bool GenerateValidPuzzleRecursive(string[,] currentPuzzle, int row, int col)
        {
            if (row == GridSize) // Base case: puzzle board is filled
            {
                // Final validation that the entire board is balanced.
                // IsValidPlacement checks local constraints; this ensures global balance.
                for(int i = 0; i < GridSize; i++) {
                    if (!IsLineBalanced(currentPuzzle, i, true) || !IsLineBalanced(currentPuzzle, i, false)) {
                        return false; // Not a valid solution if any line is unbalanced
                    }
                }
                return true; // Successfully generated and validated full board
            }

            int nextRow = col == GridSize - 1 ? row + 1 : row;
            int nextCol = col == GridSize - 1 ? 0 : col + 1;

            string[] symbolsToTry = { Sun, Moon }; // Only try to fill with Sun or Moon
            
            // Shuffle symbols for variety, though with two it's minor.
            // symbolsToTry = symbolsToTry.OrderBy(x => random.Next()).ToArray();

            foreach (var symbol in symbolsToTry)
            {
                currentPuzzle[row, col] = symbol; // Tentatively place symbol
                if (IsValidPlacement(currentPuzzle, row, col, symbol)) // Check if this placement is valid
                {
                    if (GenerateValidPuzzleRecursive(currentPuzzle, nextRow, nextCol))
                        return true; // If recursion finds a solution, propagate true up
                }
            }
            currentPuzzle[row, col] = ""; // Backtrack: no valid symbol found for this cell, reset it
            return false; // No solution found from this path
        }
        
        public bool IsLineBalanced(string[,] puzzle, int index, bool isRow)
        {
            int sunCount = 0;
            int moonCount = 0;
            for (int i = 0; i < GridSize; i++)
            {
                string cell = isRow ? puzzle[index, i] : puzzle[i, index];
                if (cell == Sun) sunCount++;
                else if (cell == Moon) moonCount++;
            }
            return sunCount == GridSize / 2 && moonCount == GridSize / 2; // Exact balance for a full solution
        }

        public bool IsValidPlacement(string[,] puzzle, int r, int c, string symbol)
        {
            // Check counts with the current symbol placed at puzzle[r,c]
            // Row balance
            int rowSymbolCount = 0;
            for (int i = 0; i < GridSize; i++) if (puzzle[r, i] == symbol) rowSymbolCount++;
            if (rowSymbolCount > GridSize / 2) return false;

            // Column balance
            int colSymbolCount = 0;
            for (int i = 0; i < GridSize; i++) if (puzzle[i, c] == symbol) colSymbolCount++;
            if (colSymbolCount > GridSize / 2) return false;

            // Check for three consecutive identical symbols in row
            if (c >= 2 && puzzle[r, c - 1] == symbol && puzzle[r, c - 2] == symbol) return false; // XXS
            if (c >= 1 && c < GridSize - 1 && puzzle[r, c - 1] == symbol && puzzle[r, c + 1] == symbol) return false; // XSX (not possible if only filling left-to-right, but good for general validator)
            if (c <= GridSize - 3 && puzzle[r, c + 1] == symbol && puzzle[r, c + 2] == symbol) return false; // SXX (not possible if only filling left-to-right)


            // Check for three consecutive identical symbols in col
            if (r >= 2 && puzzle[r - 1, c] == symbol && puzzle[r - 2, c] == symbol) return false; // XXS (col)
            if (r >= 1 && r < GridSize - 1 && puzzle[r - 1, c] == symbol && puzzle[r + 1, c] == symbol) return false; // XSX (col)
            if (r <= GridSize - 3 && puzzle[r + 1, c] == symbol && puzzle[r + 2, c] == symbol) return false; // SXX (col)

            return true;
        }
        
        public void GenerateHintSymbolsFromSolution(string[,] solutionPuzzle)
        {
            borderSymbols.Clear(); 
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize - 1; col++) // Horizontal symbols
                {
                    if (!string.IsNullOrEmpty(solutionPuzzle[row,col]) && !string.IsNullOrEmpty(solutionPuzzle[row,col+1]))
                    {
                        string symbol = solutionPuzzle[row, col] == solutionPuzzle[row, col + 1] ? "=" : "x";
                        borderSymbols.Add(new BorderSymbol(symbol, row, col, true));
                    }
                }
            }
            for (int col = 0; col < GridSize; col++) 
            {
                for (int row = 0; row < GridSize - 1; row++) // Vertical symbols
                {
                     if (!string.IsNullOrEmpty(solutionPuzzle[row,col]) && !string.IsNullOrEmpty(solutionPuzzle[row+1,col]))
                    {
                        string symbol = solutionPuzzle[row, col] == solutionPuzzle[row + 1, col] ? "=" : "x";
                        borderSymbols.Add(new BorderSymbol(symbol, row, col, false));
                    }
                }
            }
        }

        public List<(int row, int col, string content)> RevealInitialHints(string difficulty)
        {
            var hints = new List<(int row, int col, string content)>();
            int hintsToReveal = difficulty switch {
                "Easy" => 12, "Medium" => 10, "Hard" => 5, _ => 10
            };

            if (completePuzzle[0,0] == null) GeneratePuzzle(difficulty); // Ensure puzzle is generated

            List<int> cellIndices = Enumerable.Range(0, GridSize * GridSize).OrderBy(x => random.Next()).ToList();
            int revealedCount = 0;
            foreach(int cellIndex in cellIndices)
            {
                if (revealedCount >= hintsToReveal) break;
                int r = cellIndex / GridSize;
                int c = cellIndex % GridSize;
                if (!string.IsNullOrEmpty(completePuzzle[r,c]))
                {
                    hints.Add((r, c, completePuzzle[r, c]));
                    revealedCount++;
                }
            }
            return hints;
        }

        public List<BorderSymbol> RevealInitialSymbolHints(string difficulty)
        {
            int symbolHintsToReveal = difficulty switch {
                "Easy" => 7, "Medium" => 4, "Hard" => 2, _ => 4
            };
            
            revealedBorderSymbols.Clear();
            if (borderSymbols.Any()) 
            {
                List<BorderSymbol> shuffledAllSymbols = borderSymbols.OrderBy(_ => random.Next()).ToList();
                for (int i = 0; i < symbolHintsToReveal && i < shuffledAllSymbols.Count; i++)
                {
                    revealedBorderSymbols.Add(shuffledAllSymbols[i]);
                }
            }
            return new List<BorderSymbol>(revealedBorderSymbols); 
        }

        public string ProcessCellClick(int row, int col)
        {
            clickCounts[row, col]++;
            return (clickCounts[row, col] % 3) switch {
                1 => Sun, 2 => Moon, _ => ""
            };
        }

        public List<(int row, int col)> ValidateBoard(Func<int, int, string> getCellContentAt)
        {
            var errorCells = new List<(int row, int col)>();
            for (int i = 0; i < GridSize; i++)
            {
                errorCells.AddRange(CheckLineErrors(i, true, getCellContentAt));
                errorCells.AddRange(CheckLineErrors(i, false, getCellContentAt));
            }
            foreach (var symbol in revealedBorderSymbols) 
            {
                if (!CheckSpecialSymbol(symbol, getCellContentAt))
                {
                    errorCells.Add((symbol.Row, symbol.Col));
                    errorCells.Add((symbol.Row + (symbol.IsHorizontal ? 0 : 1), symbol.Col + (symbol.IsHorizontal ? 1 : 0)));
                }
            }
            return errorCells.Distinct().ToList();
        }

        public List<(int row, int col)> CheckLineErrors(int index, bool isRow, Func<int, int, string> getCellContentAt)
        {
            var errors = new List<(int row, int col)>();
            int sunCount = 0, moonCount = 0;
            for (int i = 0; i < GridSize; i++) { // Count symbols for balance check
                string current = isRow ? getCellContentAt(index, i) : getCellContentAt(i, index);
                if (current == Sun) sunCount++; else if (current == Moon) moonCount++;
            }
            if (sunCount > GridSize / 2 || moonCount > GridSize / 2) { // Balance error
                for (int i = 0; i < GridSize; i++) errors.Add(isRow ? (index, i) : (i, index));
            }
            for (int i = 0; i <= GridSize - 3; i++) { // Check for three consecutive
                string s1 = isRow ? getCellContentAt(index, i) : getCellContentAt(i, index);
                string s2 = isRow ? getCellContentAt(index, i + 1) : getCellContentAt(i + 1, index);
                string s3 = isRow ? getCellContentAt(index, i + 2) : getCellContentAt(i + 2, index);
                if (!string.IsNullOrEmpty(s1) && s1 == s2 && s2 == s3) {
                    errors.Add(isRow ? (index, i) : (i, index));
                    errors.Add(isRow ? (index, i + 1) : (i + 1, index));
                    errors.Add(isRow ? (index, i + 2) : (i + 2, index));
                }
            }
            return errors;
        }

        public bool CheckSpecialSymbol(BorderSymbol borderSym, Func<int, int, string> getCellContentAt)
        {
            string cell1 = getCellContentAt(borderSym.Row, borderSym.Col);
            string cell2 = getCellContentAt(borderSym.Row + (borderSym.IsHorizontal ? 0 : 1), borderSym.Col + (borderSym.IsHorizontal ? 1 : 0));
            if (string.IsNullOrEmpty(cell1) || string.IsNullOrEmpty(cell2)) return true; // No error if adjacent cells not filled
            return borderSym.Symbol == "=" ? cell1 == cell2 : cell1 != cell2;
        }

        public BorderSymbol? RevealBorderSymbolHint()
        {
            BorderSymbol? symbolToReveal = borderSymbols.FirstOrDefault(bs => !revealedBorderSymbols.Contains(bs));
            if (symbolToReveal != null) revealedBorderSymbols.Add(symbolToReveal);
            return symbolToReveal;
        }

        public (int row, int col, string content)? RevealCellHintFromSolution(Func<int,int,bool> isCellEnabledAndEmpty)
        {
            var emptyCells = new List<(int r, int c)>();
            for (int r = 0; r < GridSize; r++) for (int c = 0; c < GridSize; c++) if (isCellEnabledAndEmpty(r,c)) emptyCells.Add((r,c));
            
            if (!emptyCells.Any()) return null;
            var (row, col) = emptyCells[random.Next(emptyCells.Count)];
            if (completePuzzle[0,0] == null) GeneratePuzzle(_currentDifficulty); // Ensure puzzle exists
            return (row, col, completePuzzle[row, col]);
        }

        public void ResetGameCore()
        {
            clickCounts = new int[GridSize, GridSize];
            completePuzzle = new string[GridSize, GridSize]; 
            borderSymbols.Clear();
            revealedBorderSymbols.Clear();
        }

        public bool CheckWinCondition(Func<int, int, string> getCellContentAt)
        {
            for (int r = 0; r < GridSize; r++) for (int c = 0; c < GridSize; c++) if (string.IsNullOrEmpty(getCellContentAt(r, c))) return false; // All cells must be filled
            if (ValidateBoard(getCellContentAt).Any()) return false; // All rules must be valid (no errors)

            // Ensure all implicit border symbols are satisfied (ValidateBoard only checks revealed ones)
            for (int r = 0; r < GridSize; r++) { // Horizontal
                for (int c = 0; c < GridSize - 1; c++) {
                    string s1 = getCellContentAt(r,c); string s2 = getCellContentAt(r,c+1);
                    if(!string.IsNullOrEmpty(s1) && !string.IsNullOrEmpty(s2)) {
                        if ((s1 == s2 && !CheckSpecialSymbol(new BorderSymbol("=",r,c,true), getCellContentAt)) ||
                            (s1 != s2 && !CheckSpecialSymbol(new BorderSymbol("x",r,c,true), getCellContentAt))) return false;
                    }
                }
            }
            for (int c = 0; c < GridSize; c++) { // Vertical
                for (int r = 0; r < GridSize - 1; r++) {
                     string s1 = getCellContentAt(r,c); string s2 = getCellContentAt(r+1,c);
                     if(!string.IsNullOrEmpty(s1) && !string.IsNullOrEmpty(s2)) {
                        if ((s1 == s2 && !CheckSpecialSymbol(new BorderSymbol("=",r,c,false), getCellContentAt)) ||
                            (s1 != s2 && !CheckSpecialSymbol(new BorderSymbol("x",r,c,false), getCellContentAt))) return false;
                    }
                }
            }
            return true; 
        }

        public bool IsCellEmpty(int row, int col, Func<int, int, string> getCellContentAt) => string.IsNullOrEmpty(getCellContentAt(row, col));
        public List<BorderSymbol> GetRevealedBorderSymbols() => new List<BorderSymbol>(revealedBorderSymbols);
        public string GetCompletePuzzleCell(int r, int c) {
            if (completePuzzle[0,0] == null) GeneratePuzzle(_currentDifficulty); // Ensure puzzle exists
            return (r>=0 && r<GridSize && c>=0 && c<GridSize) ? completePuzzle[r,c] ?? "" : "";
        }
        public void SetDifficulty(string difficulty) => _currentDifficulty = difficulty;
        public string GetCurrentDifficulty() => _currentDifficulty;
        public List<BorderSymbol> GetAllBorderSymbols() { // All symbols for the current solution
             if (completePuzzle[0,0] == null) GeneratePuzzle(_currentDifficulty);
             if (!borderSymbols.Any()) GenerateHintSymbolsFromSolution(completePuzzle); // Ensure generated
            return new List<BorderSymbol>(borderSymbols);
        }
    }
}
