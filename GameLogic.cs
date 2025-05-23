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
        }

        public void InitializeGameCore() // Can be called by ResetGameCore or if a lighter reset is needed
        {
            clickCounts = new int[GridSize, GridSize];
            completePuzzle = new string[GridSize, GridSize]; 
            borderSymbols.Clear();
            revealedBorderSymbols.Clear();
        }

        public void ResetGameCore() // Used before generating a new puzzle
        {
            InitializeGameCore(); // Calls the main initialization logic
        }

        public void GeneratePuzzle(string difficulty)
        {
            _currentDifficulty = difficulty;
            ResetGameCore(); 

            if (!GenerateValidPuzzleRecursive(completePuzzle, 0, 0))
            {
                Console.WriteLine("Failed to generate a valid puzzle!"); // Non-UI error reporting
                return; 
            }
            GenerateHintSymbolsFromSolution(completePuzzle); 
        }

        private bool GenerateValidPuzzleRecursive(string[,] currentPuzzle, int row, int col)
        {
            if (row == GridSize) 
            {
                for(int i = 0; i < GridSize; i++) { // Final balance check for the generated solution
                    if (!IsLineBalancedForSolution(currentPuzzle, i, true) || !IsLineBalancedForSolution(currentPuzzle, i, false)) {
                        return false; 
                    }
                }
                return true; 
            }

            int nextRow = col == GridSize - 1 ? row + 1 : row;
            int nextCol = col == GridSize - 1 ? 0 : col + 1;

            string[] symbolsToTry = { Sun, Moon };
            symbolsToTry = symbolsToTry.OrderBy(x => random.Next()).ToArray(); // Shuffle for variety

            foreach (var symbol in symbolsToTry)
            {
                currentPuzzle[row, col] = symbol; 
                if (IsValidPlacementForGeneration(currentPuzzle, row, col)) 
                {
                    if (GenerateValidPuzzleRecursive(currentPuzzle, nextRow, nextCol))
                        return true; 
                }
            }
            currentPuzzle[row, col] = ""; 
            return false; 
        }
        
        // Stricter balance for a complete solution
        private bool IsLineBalancedForSolution(string[,] puzzle, int index, bool isRow)
        {
            int sunCount = 0;
            int moonCount = 0;
            for (int i = 0; i < GridSize; i++)
            {
                string cell = isRow ? puzzle[index, i] : puzzle[i, index];
                if (cell == Sun) sunCount++;
                else if (cell == Moon) moonCount++;
            }
            return sunCount == GridSize / 2 && moonCount == GridSize / 2;
        }

        // Validation during puzzle generation (checks current symbol at r,c)
        private bool IsValidPlacementForGeneration(string[,] puzzle, int r, int c)
        {
            string symbol = puzzle[r,c];
            if (string.IsNullOrEmpty(symbol)) return true; // Empty is always valid during this phase if allowed by recursion

            // Check row balance up to current column c
            int rowSymbolCount = 0;
            for (int i = 0; i <= c; i++) if (puzzle[r, i] == symbol) rowSymbolCount++;
            if (rowSymbolCount > GridSize / 2) return false;

            // Check col balance up to current row r
            int colSymbolCount = 0;
            for (int i = 0; i <= r; i++) if (puzzle[i, c] == symbol) colSymbolCount++;
            if (colSymbolCount > GridSize / 2) return false;
            
            // Check three consecutive in row (checking leftwards from current position)
            if (c >= 2 && puzzle[r,c-1] == symbol && puzzle[r,c-2] == symbol) return false;
            
            // Check three consecutive in col (checking upwards from current position)
            if (r >= 2 && puzzle[r-1,c] == symbol && puzzle[r-2,c] == symbol) return false;

            return true;
        }
        
        // Public version for external validation if needed (more comprehensive)
        public bool IsValidPlacement(string[,] puzzle, int r, int c, string symbol)
        {
            string originalSymbol = puzzle[r,c];
            puzzle[r,c] = symbol; // Temporarily place

            int rowSun = 0, rowMoon = 0, colSun = 0, colMoon = 0;
            for(int i=0; i<GridSize; ++i) {
                if(puzzle[r,i] == Sun) rowSun++; else if(puzzle[r,i] == Moon) rowMoon++;
                if(puzzle[i,c] == Sun) colSun++; else if(puzzle[i,c] == Moon) colMoon++;
            }
            if(rowSun > GridSize/2 || rowMoon > GridSize/2 || colSun > GridSize/2 || colMoon > GridSize/2) {
                puzzle[r,c] = originalSymbol; return false;
            }

            for(int i=0; i <= GridSize-3; ++i) { // Check 3-in-a-row/col
                if((!string.IsNullOrEmpty(puzzle[r,i]) && puzzle[r,i] == puzzle[r,i+1] && puzzle[r,i+1] == puzzle[r,i+2]) ||
                   (!string.IsNullOrEmpty(puzzle[i,c]) && puzzle[i,c] == puzzle[i+1,c] && puzzle[i+1,c] == puzzle[i+2,c])) {
                    puzzle[r,c] = originalSymbol; return false;
                }
            }
            puzzle[r,c] = originalSymbol; // Revert
            return true;
        }

        public void GenerateHintSymbolsFromSolution(string[,] solutionPuzzle)
        {
            borderSymbols.Clear(); 
            for (int row = 0; row < GridSize; row++) {
                for (int col = 0; col < GridSize - 1; col++) {
                    if (!string.IsNullOrEmpty(solutionPuzzle[row,col]) && !string.IsNullOrEmpty(solutionPuzzle[row,col+1])) {
                        borderSymbols.Add(new BorderSymbol(solutionPuzzle[row, col] == solutionPuzzle[row, col + 1] ? "=" : "x", row, col, true));
                    }
                }
            }
            for (int col = 0; col < GridSize; col++) {
                for (int row = 0; row < GridSize - 1; row++) {
                     if (!string.IsNullOrEmpty(solutionPuzzle[row,col]) && !string.IsNullOrEmpty(solutionPuzzle[row+1,col])) {
                        borderSymbols.Add(new BorderSymbol(solutionPuzzle[row, col] == solutionPuzzle[row + 1, col] ? "=" : "x", row, col, false));
                    }
                }
            }
        }

        public List<(int row, int col, string content)> RevealInitialHints(string difficulty)
        {
            var hints = new List<(int row, int col, string content)>();
            int hintsToReveal = difficulty switch { "Easy" => 12, "Medium" => 10, "Hard" => 5, _ => 10 };
            if (completePuzzle[0,0] == null) GeneratePuzzle(difficulty);

            List<int> cellIndices = Enumerable.Range(0, GridSize * GridSize).OrderBy(x => random.Next()).ToList();
            for(int i=0; i < hintsToReveal && i < cellIndices.Count; ++i) {
                int r = cellIndices[i] / GridSize;
                int c = cellIndices[i] % GridSize;
                if (!string.IsNullOrEmpty(completePuzzle[r,c])) hints.Add((r, c, completePuzzle[r, c]));
                else hintsToReveal++; // Try to get one more if we picked an empty solution cell (rare for this puzzle type)
            }
            return hints;
        }

        public List<BorderSymbol> RevealInitialSymbolHints(string difficulty)
        {
            int symbolHintsToReveal = difficulty switch { "Easy" => 7, "Medium" => 4, "Hard" => 2, _ => 4 };
            revealedBorderSymbols.Clear();
            if (borderSymbols.Any()) {
                revealedBorderSymbols.AddRange(borderSymbols.OrderBy(_ => random.Next()).Take(symbolHintsToReveal));
            }
            return new List<BorderSymbol>(revealedBorderSymbols); 
        }

        public string ProcessCellClick(int row, int col)
        {
            clickCounts[row, col]++;
            return (clickCounts[row, col] % 3) switch { 1 => Sun, 2 => Moon, _ => "" };
        }

        public List<(int row, int col)> ValidateBoard(Func<int, int, string> getCellContentAt)
        {
            var errorCells = new List<(int row, int col)>();
            for (int i = 0; i < GridSize; i++) {
                errorCells.AddRange(CheckLineErrors(i, true, getCellContentAt));
                errorCells.AddRange(CheckLineErrors(i, false, getCellContentAt));
            }
            foreach (var symbol in revealedBorderSymbols) {
                if (!CheckSpecialSymbol(symbol, getCellContentAt)) {
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
            for (int i = 0; i < GridSize; i++) {
                string current = isRow ? getCellContentAt(index, i) : getCellContentAt(i, index);
                if (current == Sun) sunCount++; else if (current == Moon) moonCount++;
            }
            if (sunCount > GridSize / 2 || moonCount > GridSize / 2) {
                for (int i = 0; i < GridSize; i++) errors.Add(isRow ? (index, i) : (i, index));
            }
            for (int i = 0; i <= GridSize - 3; i++) {
                string s1 = isRow ? getCellContentAt(index, i) : getCellContentAt(i, index);
                string s2 = isRow ? getCellContentAt(index, i + 1) : getCellContentAt(i + 1, index);
                string s3 = isRow ? getCellContentAt(index, i + 2) : getCellContentAt(i + 2, index);
                if (!string.IsNullOrEmpty(s1) && s1 == s2 && s2 == s3) {
                    errors.AddRange(isRow ? new[]{(index,i),(index,i+1),(index,i+2)} : new[]{(i,index),(i+1,index),(i+2,index)});
                }
            }
            return errors;
        }

        public bool CheckSpecialSymbol(BorderSymbol borderSym, Func<int, int, string> getCellContentAt)
        {
            string cell1 = getCellContentAt(borderSym.Row, borderSym.Col);
            string cell2 = getCellContentAt(borderSym.Row + (borderSym.IsHorizontal ? 0 : 1), borderSym.Col + (borderSym.IsHorizontal ? 1 : 0));
            if (string.IsNullOrEmpty(cell1) || string.IsNullOrEmpty(cell2)) return true;
            return borderSym.Symbol == "=" ? cell1 == cell2 : cell1 != cell2;
        }

        public BorderSymbol? RevealBorderSymbolHint()
        {
            var symbolToReveal = borderSymbols.FirstOrDefault(bs => !revealedBorderSymbols.Contains(bs));
            if (symbolToReveal != null) revealedBorderSymbols.Add(symbolToReveal);
            return symbolToReveal;
        }

        public (int row, int col, string content)? RevealCellHintFromSolution(Func<int,int,bool> isCellEnabledAndEmpty)
        {
            var emptyCells = new List<(int r, int c)>();
            for (int r = 0; r < GridSize; r++) for (int c = 0; c < GridSize; c++) if (isCellEnabledAndEmpty(r,c)) emptyCells.Add((r,c));
            if (!emptyCells.Any()) return null;
            var (row, col) = emptyCells[random.Next(emptyCells.Count)];
            if (completePuzzle[0,0] == null) GeneratePuzzle(_currentDifficulty);
            return (row, col, completePuzzle[row, col]);
        }

        public bool CheckWinCondition(Func<int, int, string> getCellContentAt)
        {
            for (int r = 0; r < GridSize; r++) for (int c = 0; c < GridSize; c++) if (string.IsNullOrEmpty(getCellContentAt(r, c))) return false;
            if (ValidateBoard(getCellContentAt).Any()) return false;
            // Check all implicit border symbols for win condition
            for (int r = 0; r < GridSize; r++) for (int c = 0; c < GridSize - 1; c++) { // Horizontal
                string s1 = getCellContentAt(r,c); string s2 = getCellContentAt(r,c+1);
                if(!string.IsNullOrEmpty(s1) && !string.IsNullOrEmpty(s2)) {
                    if (!CheckSpecialSymbol(new BorderSymbol(s1 == s2 ? "=" : "x", r,c,true), getCellContentAt)) return false;
                }
            }
            for (int c = 0; c < GridSize; c++) for (int r = 0; r < GridSize - 1; r++) { // Vertical
                 string s1 = getCellContentAt(r,c); string s2 = getCellContentAt(r+1,c);
                 if(!string.IsNullOrEmpty(s1) && !string.IsNullOrEmpty(s2)) {
                    if (!CheckSpecialSymbol(new BorderSymbol(s1 == s2 ? "=" : "x", r,c,false), getCellContentAt)) return false;
                }
            }
            return true; 
        }

        public List<BorderSymbol> GetRevealedBorderSymbols() => new List<BorderSymbol>(revealedBorderSymbols);
        public string GetCompletePuzzleCell(int r, int c) {
            if (completePuzzle[0,0] == null) GeneratePuzzle(_currentDifficulty);
            return (r>=0 && r<GridSize && c>=0 && c<GridSize) ? completePuzzle[r,c] ?? "" : "";
        }
        public void SetDifficulty(string difficulty) => _currentDifficulty = difficulty;
        public string GetCurrentDifficulty() => _currentDifficulty;
        public List<BorderSymbol> GetAllBorderSymbols() { 
             if (completePuzzle[0,0] == null) GeneratePuzzle(_currentDifficulty);
             if (!borderSymbols.Any() && completePuzzle[0,0] != null) GenerateHintSymbolsFromSolution(completePuzzle);
            return new List<BorderSymbol>(borderSymbols);
        }
    }
}
