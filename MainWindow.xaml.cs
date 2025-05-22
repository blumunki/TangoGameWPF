using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TangoGame
{
    public class TangoButton : Button
    {
        //public string SpecialSymbol { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
    }
    public class BorderSymbol
    {
        public string Symbol { get; set; }
        public int Row { get; set; }
        public int Col { get; set; }
        public bool IsHorizontal { get; set; }

        public BorderSymbol(string symbol, int row, int col, bool isHorizontal)
        {
            Symbol = symbol; // Initialize Symbol from constructor parameter - FIX for Warning
            Row = row;
            Col = col;
            IsHorizontal = isHorizontal;
        }

        //Override Equals and GetHashCode for List.Contains and other comparisons to work correctly
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

    public partial class MainWindow : Window
    {
        private const int GridSize = 6;
        private const string Sun = "☀️";
        private const string Moon = "🌙";
        private int[,] clickCounts;
        private List<BorderSymbol> borderSymbols = new List<BorderSymbol>();
        private Random random = new Random();
        private string[,] completePuzzle;
        private List<BorderSymbol> allBorderSymbols; // Store all generated border symbols
        private List<BorderSymbol> revealedBorderSymbols; // Track revealed border symbols
        private System.Windows.Threading.DispatcherTimer errorTimer;
        private List<(int row, int col)> _currentErrorCells = new List<(int row, int col)>();
        private string _currentDifficulty; // Default difficulty
        private bool _isInitializingComboBox = false; // Add this flag - initially false

        public MainWindow()
        {
            InitializeComponent();
            errorTimer = new System.Windows.Threading.DispatcherTimer();
            errorTimer.Interval = TimeSpan.FromSeconds(1); // Set delay to 0.5 seconds
            errorTimer.Tick += ErrorTimer_Tick;

            // INITIALIZE NON-NULLABLE FIELDS IN CONSTRUCTOR - FIX FOR WARNINGS:
            clickCounts = new int[GridSize, GridSize]; // Initialize clickCounts
            completePuzzle = new string[GridSize, GridSize]; // Initialize completePuzzle
            allBorderSymbols = new List<BorderSymbol>(); // Initialize allBorderSymbols
            revealedBorderSymbols = new List<BorderSymbol>(); // Initialize revealedBorderSymbols


            InitializeGame();

            if (string.IsNullOrEmpty(_currentDifficulty)) // Check if _currentDifficulty is null or empty (though it shouldn't be)
            {
                _currentDifficulty = "Medium"; // Default to "Medium" if it's unexpectedly null
                System.Diagnostics.Debug.WriteLine("WARNING: _currentDifficulty was unexpectedly null before GeneratePuzzle, defaulting to Medium."); // Debug output in case this happens
            }
            GeneratePuzzle(_currentDifficulty); // Pass _currentDifficulty here

            // COMBOBOX INITIALIZATION MOVED TO CONSTRUCTOR:
            DifficultyComboBox.Items.Clear();
            DifficultyComboBox.Items.Add(new ComboBoxItem { Content = "Easy" });
            DifficultyComboBox.Items.Add(new ComboBoxItem { Content = "Medium" });
            DifficultyComboBox.Items.Add(new ComboBoxItem { Content = "Hard" });
            DifficultyComboBox.SelectedIndex = 1; // Select "Medium" by default
        }

        // Initializes the grid of buttons (editable cells) with persistent borders and a bright white background.
        private void InitializeGame()
        {
            _isInitializingComboBox = true; // Set flag to true at the start of InitializeGame

            _currentDifficulty = "Medium"; // Set default difficulty to Medium

            clickCounts = new int[GridSize, GridSize];
            GameGrid.Children.Clear();

            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize; col++)
                {
                    var button = new TangoButton
                    {
                        Content = "",
                        FontSize = 20,
                        Row = row,
                        Col = col,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Background = Brushes.White,  // User editable cells are bright white
                        Foreground = Brushes.Black
                    };
                    button.Click += GridButton_Click;
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                    GameGrid.Children.Add(button);
                }
            }
            _isInitializingComboBox = false; // Reset flag to false at the end of InitializeGame
        }

        // Handles user clicks. Cycles through empty → Sun → Moon.
        private void GridButton_Click(object sender, RoutedEventArgs e)
        {
            TangoButton clickedButton = (TangoButton)sender;
            int row = clickedButton.Row;
            int col = clickedButton.Col;

            clickCounts[row, col]++;
            int state = clickCounts[row, col] % 3;
            string newContent = state switch
            {
                1 => Sun,
                2 => Moon,
                _ => ""
            };

            clickedButton.Content = newContent;
            clickedButton.Foreground = newContent switch
            {
                Sun => Brushes.Gold,
                Moon => Brushes.DodgerBlue,
                _ => Brushes.Black
            };

            ValidateBoard();
            CheckWinCondition();
        }

        private void ValidateBoard()
        {
            List<(int row, int col)> errorCells = new List<(int row, int col)>();

            // Check for consecutive and balance errors
            for (int i = 0; i < GridSize; i++)
            {
                errorCells.AddRange(CheckLineErrors(i, true));  // Check row
                errorCells.AddRange(CheckLineErrors(i, false)); // Check column
            }

            // Check for special symbol errors
            foreach (var symbol in revealedBorderSymbols) // Only check revealed symbols
            {
                if (!CheckSpecialSymbol(symbol))
                {
                    errorCells.Add((symbol.Row, symbol.Col));
                    errorCells.Add((symbol.Row + (symbol.IsHorizontal ? 0 : 1), symbol.Col + (symbol.IsHorizontal ? 1 : 0)));
                }
            }

            _currentErrorCells = errorCells.Distinct().ToList(); // Store error cells
            if (!errorTimer.IsEnabled)
            {
                errorTimer.Start(); // Start the timer if it's not already running
            }
        }

        private void ErrorTimer_Tick(object? sender, EventArgs e)
        {
            errorTimer.Stop(); // Stop the timer

            if (_currentErrorCells.Any())
            {
                HighlightInvalidMoves(_currentErrorCells); // Call error highlighting with stored cells
            }
            else
            {
                ClearInvalidMoveHighlights(); // Clear highlighting if no errors
            }
            _currentErrorCells.Clear(); // Clear stored error cells for next validation
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (revealedBorderSymbols == null) revealedBorderSymbols = new List<BorderSymbol>();

                Random random = new Random();
                if (random.Next(2) == 0) // 50% chance to reveal symbol hint
                {
                    if (revealedBorderSymbols.Count < borderSymbols.Count)
                    {
                        RevealBorderSymbolHint();
                    }
                    else if (GameGrid.Children.OfType<TangoButton>().Any(b => string.IsNullOrEmpty(b.Content?.ToString()) && b.IsEnabled)) // If no more symbol hints, try cell hint
                    {
                        RevealCellHintFromSolution();
                    }
                    else
                    {
                        MessageBox.Show("No more hints available or puzzle complete!");
                    }
                }
                else // 50% chance to reveal cell hint
                {
                    if (GameGrid.Children.OfType<TangoButton>().Any(b => string.IsNullOrEmpty(b.Content?.ToString()) && b.IsEnabled))
                    {
                        RevealCellHintFromSolution();
                    }
                    else if (revealedBorderSymbols.Count < borderSymbols.Count) // If no more cell hints, try symbol hint
                    {
                        RevealBorderSymbolHint();
                    }
                    else
                    {
                        MessageBox.Show("No more hints available or puzzle complete!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in HintButton_Click: {ex.Message}");
                MessageBox.Show($"Hint Button Error: {ex.Message}", "Error");
            }
        }

        private void RevealBorderSymbolHint()
        {
            try
            {
                // Find an unrevealed border symbol
                BorderSymbol? symbolToReveal = borderSymbols.FirstOrDefault(bs => !revealedBorderSymbols.Contains(bs));
                if (symbolToReveal != null)
                {
                    revealedBorderSymbols.Add(symbolToReveal); // Track revealed symbols
                    DrawBorderSymbols(); // Redraw symbols to show the new one
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RevealBorderSymbolHint: {ex.Message}"); // Log error
                MessageBox.Show($"Reveal Symbol Hint Error: {ex.Message}", "Error");
            }

        }


        private void RevealCellHintFromSolution()
        {
            try
            {
                // Find an empty cell that is part of the solution
                List<(int row, int col)> emptyCells = new List<(int row, int col)>();
                for (int row = 0; row < GridSize; row++)
                {
                    for (int col = 0; col < GridSize; col++)
                    {
                        if (string.IsNullOrEmpty(GetCellContent(row, col)) && GetButton(row, col).IsEnabled) //Find editable empty cells
                        {
                            emptyCells.Add((row, col));
                        }
                    }
                }

                if (emptyCells.Count > 0)
                {
                    Random random = new Random();
                    var cellToReveal = emptyCells[random.Next(emptyCells.Count)];
                    string solutionContent = completePuzzle[cellToReveal.row, cellToReveal.col]; // Get content from the SOLUTION
                    RevealCell(cellToReveal.row, cellToReveal.col, solutionContent); // Reveal VALID content
                }
                else
                {
                    MessageBox.Show("No more hints available or puzzle complete!"); // Or handle no hints left scenario
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RevealCellHintFromSolution: {ex.Message}"); // Log error
                MessageBox.Show($"Reveal Cell Hint Error: {ex.Message}", "Error");
            }
        }

        // Checks for consecutive same icons (horizontally and vertically)
        private (bool isValid, List<(int row, int col)> errorCells) CheckConsecutive(int row, int col, string content)
        {
            List<(int row, int col)> errorCells = new List<(int row, int col)>();

            // Check horizontal
            int leftCount = CountConsecutive(row, col, 0, -1, content);
            int rightCount = CountConsecutive(row, col, 0, 1, content);
            if (leftCount + rightCount + 1 > 3) // Include the current cell
            {
                for (int i = Math.Max(0, col - leftCount); i <= Math.Min(GridSize - 1, col + rightCount); i++)
                {
                    errorCells.Add((row, i));
                }
            }

            // Check vertical
            int upCount = CountConsecutive(row, col, -1, 0, content);
            int downCount = CountConsecutive(row, col, 1, 0, content);
            if (upCount + downCount + 1 > 3) // Include the current cell
            {
                for (int i = Math.Max(0, row - upCount); i <= Math.Min(GridSize - 1, row + downCount); i++)
                {
                    errorCells.Add((i, col));
                }
            }

            return (errorCells.Count == 0, errorCells);
        }

        // Counts consecutive cells in one direction.
        private int CountConsecutive(int row, int col, int dRow, int dCol, string content)
        {
            int count = 0;
            row += dRow;
            col += dCol;
            while (row >= 0 && row < GridSize && col >= 0 && col < GridSize)
            {
                if (GetCellContent(row, col) == content)
                    count++;
                else
                    break;
                row += dRow;
                col += dCol;
            }
            return count;
        }

        // Checks for valid special symbol placement (equals and times signs) between adjacent cells.
        private bool CheckSpecialSymbols(int row, int col)
        {
            bool horizontalValid = CheckHorizontalSymbols(row, col);
            bool verticalValid = CheckVerticalSymbols(row, col);
            return horizontalValid && verticalValid;
        }

        private bool CheckSpecialSymbol(BorderSymbol symbol)
        {
            string left = GetCellContent(symbol.Row, symbol.Col);
            string right = GetCellContent(symbol.Row + (symbol.IsHorizontal ? 0 : 1), symbol.Col + (symbol.IsHorizontal ? 1 : 0));

            if (left == "" || right == "") return true;

            return symbol.Symbol == "=" ? left == right : left != right;
        }

        private bool CheckHorizontalSymbols(int row, int col)
        {
            if (col > 0)
            {
                var leftSymbol = GetBorderSymbol(row, col - 1, isHorizontal: true);
                if (leftSymbol != null && !ValidateSymbol(
                    GetCellContent(row, col - 1),
                    GetCellContent(row, col),
                    leftSymbol.Symbol))
                {
                    return false;
                }
            }
            if (col < GridSize - 1)
            {
                var rightSymbol = GetBorderSymbol(row, col, isHorizontal: true);
                if (rightSymbol != null && !ValidateSymbol(
                    GetCellContent(row, col),
                    GetCellContent(row, col + 1),
                    rightSymbol.Symbol))
                {
                    return false;
                }
            }
            return true;
        }

        private bool CheckVerticalSymbols(int row, int col)
        {
            if (row > 0)
            {
                var topSymbol = GetBorderSymbol(row - 1, col, isHorizontal: false);
                if (topSymbol != null && !ValidateSymbol(
                    GetCellContent(row - 1, col),
                    GetCellContent(row, col),
                    topSymbol.Symbol))
                {
                    return false;
                }
            }
            if (row < GridSize - 1)
            {
                var bottomSymbol = GetBorderSymbol(row, col, isHorizontal: false);
                if (bottomSymbol != null && !ValidateSymbol(
                    GetCellContent(row, col),
                    GetCellContent(row + 1, col),
                    bottomSymbol.Symbol))
                {
                    return false;
                }
            }
            return true;
        }

        private List<(int row, int col)> CheckLineErrors(int index, bool isRow)
        {
            List<(int row, int col)> errors = new List<(int row, int col)>();
            int sunCount = 0, moonCount = 0;
            string prev = "", prevPrev = "";

            for (int i = 0; i < GridSize; i++)
            {
                string current = isRow ? GetCellContent(index, i) : GetCellContent(i, index);
                if (current == Sun) sunCount++;
                if (current == Moon) moonCount++;

                if (current != "" && current == prev && current == prevPrev)
                {
                    errors.Add(isRow ? (index, i) : (i, index));
                    errors.Add(isRow ? (index, i - 1) : (i - 1, index));
                    errors.Add(isRow ? (index, i - 2) : (i - 2, index));
                }

                prevPrev = prev;
                prev = current;
            }

            if (sunCount > GridSize / 2 || moonCount > GridSize / 2)
            {
                for (int i = 0; i < GridSize; i++)
                {
                    errors.Add(isRow ? (index, i) : (i, index));
                }
            }

            return errors;
        }

        private bool ValidateSymbol(string left, string right, string symbol)
        {
            // If either cell is empty, allow it.
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right))
                return true;
            return symbol switch
            {
                "=" => left == right,
                "x" => left != right,
                _ => true
            };
        }

        private BorderSymbol? GetBorderSymbol(int row, int col, bool isHorizontal)
        {
            if (revealedBorderSymbols == null) revealedBorderSymbols = new List<BorderSymbol>(); // Ensure initialized

            return revealedBorderSymbols.FirstOrDefault(bs => // Search in revealedBorderSymbols
                bs.Row == row &&
                bs.Col == col &&
                bs.IsHorizontal == isHorizontal);
        }

        // Draws the hint symbols (equals or times) on the grid.
        private void DrawBorderSymbols()
        {
            GameGrid.Children.OfType<TextBlock>().ToList().ForEach(textBlock => GameGrid.Children.Remove(textBlock)); // Clear existing TextBlocks

            if (revealedBorderSymbols != null) // Check if initialized and not null
            {
                foreach (var symbol in revealedBorderSymbols) // Draw only revealed symbols
                {
                    var textBlock = new TextBlock
                    {
                        Text = symbol.Symbol,
                        Foreground = Brushes.Red,
                        FontSize = 19,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, -4.75, 0, 0)
                    };

                    Grid.SetRow(textBlock, symbol.Row);
                    Grid.SetColumn(textBlock, symbol.Col);
                    if (symbol.IsHorizontal)
                        Grid.SetColumnSpan(textBlock, 2);
                    else
                        Grid.SetRowSpan(textBlock, 2);

                    GameGrid.Children.Add(textBlock);
                }
            }
        }


        // Generates the puzzle by placing hint icons and SOME hint symbols.
        private void GeneratePuzzle(string difficulty)
        {
            //Console.WriteLine("GeneratePuzzle() called with difficulty: " + difficulty); // Debugging

            ResetGame(); // Clear the board and state

            completePuzzle = new string[GridSize, GridSize]; // Initialize completePuzzle

            if (!GenerateValidPuzzle(completePuzzle, 0, 0)) // Generate a valid solution
            {
                MessageBox.Show("Failed to generate a valid puzzle!"); // Handle failure case
                return; // Exit if puzzle generation fails
            }

            GenerateHintSymbols(completePuzzle); // Generate ALL border symbols based on the SOLUTION
            revealedBorderSymbols = new List<BorderSymbol>(); // Initialize revealedBorderSymbols

            RevealInitialHints(difficulty); // Pass difficulty to RevealInitialHints - IMPORTANT
            RevealInitialSymbolHints(difficulty); // Reveal symbol hints based on difficulty - NEW


            // Random random = new Random();
            // List<(int row, int col)> hintCellPositions = new List<(int row, int col)>();

            // // Reveal some cells as initial hints - let's reveal a proportion, e.g., 10-20% of cells
            // int numberOfHints = (int)(GridSize * GridSize * random.NextDouble() * 0.1) + (GridSize * GridSize / 10); // 10% to 20% hints
            // for (int i = 0; i < numberOfHints; i++)
            // {
            //     int row, col;
            //     do
            //     {
            //         row = random.Next(GridSize);
            //         col = random.Next(GridSize);
            //     } while (hintCellPositions.Contains((row, col))); // Ensure no duplicate hint cells

            //     hintCellPositions.Add((row, col));
            //     RevealCell(row, col, completePuzzle[row, col]); // Reveal from the SOLUTION
            // }

            // // Reveal some border symbols as initial hints - reveal a proportion, e.g. 30-40% of symbols
            // int numberOfSymbolHints = (int)(borderSymbols.Count * random.NextDouble() * 0.2) + (borderSymbols.Count / 5); // 20% to 30% symbol hints
            // List<int> symbolIndices = Enumerable.Range(0, borderSymbols.Count).OrderBy(_ => random.Next()).ToList(); // Randomize symbol indices
            // for (int i = 0; i < numberOfSymbolHints; i++)
            // {
            //     revealedBorderSymbols.Add(borderSymbols[symbolIndices[i]]); // Add randomly selected symbols to revealed list
            // }


            DrawBorderSymbols(); // Draw the initial revealed hint symbols
        }

        private void RevealInitialHints(string difficulty) // Add difficulty parameter
        {
            ClearInitialHints(); // Clear any existing hints

            int hintsToReveal = 0;

            switch (difficulty)
            {
                case "Easy":
                    hintsToReveal = 12; // Example: Reveal 12 hints for Easy
                    break;
                case "Medium":
                    hintsToReveal = 10; // Example: Reveal 10 hints for Medium
                    break;
                case "Hard":
                    hintsToReveal = 5;  // Example: Reveal 5 hints for Hard
                    break;
                default: // Default to Medium if difficulty is not recognized
                    hintsToReveal = 10;
                    break;
            }

            Random rng = new Random();
            List<int> cellIndices = Enumerable.Range(0, 36).ToList(); // 6x6 grid = 36 cells

            for (int i = 0; i < hintsToReveal; i++)
            {
                if (!cellIndices.Any()) break; // Prevent error if no cells left

                int indexIndex = rng.Next(cellIndices.Count);
                int cellIndex = cellIndices[indexIndex];
                cellIndices.RemoveAt(indexIndex); // Avoid duplicate hints

                int row = cellIndex / 6; // Calculate row and column from index
                int col = cellIndex % 6;

                TangoButton button = GetButton(row, col);
                if (button != null && string.IsNullOrEmpty(button.Content?.ToString())) // Check if button is valid and empty
                {
                    RevealCell(row, col, completePuzzle[row, col]); // Call RevealCell to set locked style and background - CORRECTED
                    // button.Foreground = Brushes.Gray; // Make hints gray to distinguish them
                    // button.IsHint = true; // You might want to add an IsHint property to TangoButton if you need to track hints specifically
                }
            }
        }

        private void RevealInitialSymbolHints(string difficulty)
        {
            int symbolHintsToReveal = 0;

            switch (difficulty)
            {
                case "Easy":
                    symbolHintsToReveal = 7; // Example: Reveal 7 symbol hints for Easy
                    break;
                case "Medium":
                    symbolHintsToReveal = 4; // Example: Reveal 4 symbol hints for Medium
                    break;
                case "Hard":
                    symbolHintsToReveal = 2;  // Example: Reveal 2 symbol hints for Hard
                    break;
                default: // Default to Medium if difficulty is not recognized
                    symbolHintsToReveal = 4;
                    break;
            }

            Random random = new Random();
            List<int> symbolIndices = Enumerable.Range(0, borderSymbols.Count).OrderBy(_ => random.Next()).ToList(); // Randomize symbol indices

            revealedBorderSymbols = new List<BorderSymbol>(); // Clear any previously revealed symbols - IMPORTANT

            for (int i = 0; i < symbolHintsToReveal; i++)
            {
                if (i < borderSymbols.Count) // Make sure we don't go out of bounds if there are fewer symbols than hints to reveal
                {
                    revealedBorderSymbols.Add(borderSymbols[symbolIndices[i]]); // Add randomly selected symbols to revealed list
                }
            }
        }

        private void ClearInitialHints()
        {
            foreach (var child in GameGrid.Children.OfType<TangoButton>())
            {
                if (child.Foreground == Brushes.Gray) // Identify buttons that are hints by checking Foreground
                {
                    child.Content = ""; // Clear the content (icon)
                    child.Foreground = Brushes.Black; // Reset foreground to default black
                    // If you had an IsHint property, you would also reset it here: child.IsHint = false;
                }
            }
        }

        private bool GenerateValidPuzzle(string[,] puzzle, int row, int col)
        {
            if (col == GridSize)
            {
                row++;
                col = 0;
            }
            if (row == GridSize) return true;

            string[] symbols = { Sun, Moon };
            foreach (var symbol in symbols)
            {
                if (IsValidPlacement(puzzle, row, col, symbol))
                {
                    puzzle[row, col] = symbol;
                    if (GenerateValidPuzzle(puzzle, row, col + 1))
                        return true;
                }
            }
            puzzle[row, col] = "";
            return false;
        }

        private bool IsValidPlacement(string[,] puzzle, int row, int col, string symbol)
        {
            // Check row and column balance
            int rowCount = 0, colCount = 0;
            for (int i = 0; i < GridSize; i++)
            {
                if (puzzle[row, i] == symbol) rowCount++;
                if (puzzle[i, col] == symbol) colCount++;
            }
            if (rowCount >= GridSize / 2 || colCount >= GridSize / 2) return false;

            // Check for three consecutive
            if (col >= 2 && puzzle[row, col - 1] == symbol && puzzle[row, col - 2] == symbol) return false;
            if (row >= 2 && puzzle[row - 1, col] == symbol && puzzle[row - 2, col] == symbol) return false;

            return true;
        }


        private void GenerateHintSymbols(string[,] puzzle)
        {
            borderSymbols.Clear();
            for (int row = 0; row < GridSize; row++)
            {
                for (int col = 0; col < GridSize - 1; col++)
                {
                    string symbol = puzzle[row, col] == puzzle[row, col + 1] ? "=" : "x";
                    borderSymbols.Add(new BorderSymbol(symbol, row, col, true));
                    // borderSymbols.Add(new BorderSymbol
                    // {
                    //     Symbol = symbol,
                    //     Row = row,
                    //     Col = col,
                    //     IsHorizontal = true
                    // });
                }
            }
            for (int col = 0; col < GridSize; col++)
            {
                for (int row = 0; row < GridSize - 1; row++)
                {
                    string symbol = puzzle[row, col] == puzzle[row + 1, col] ? "=" : "x";
                    borderSymbols.Add(new BorderSymbol(symbol, row, col, false));
                    // borderSymbols.Add(new BorderSymbol
                    // {
                    //     Symbol = symbol,
                    //     Row = row,
                    //     Col = col,
                    //     IsHorizontal = false
                    // });
                }
            }
            allBorderSymbols = new List<BorderSymbol>(borderSymbols); // Store all generated symbols
        }

        private void RevealCell(int row, int col, string content)
        {
            TangoButton button = GetButton(row, col);
            button.Content = content;
            button.IsEnabled = false;
            button.Background = Brushes.LightGray; // Use light gray for hint icons
            button.Foreground = content == Sun ? Brushes.Gold : Brushes.DodgerBlue;
        }


        // Resets the game grid and state.
        private void ResetGame()
        {
            GameGrid.Children.Clear();
            InitializeGame();
            borderSymbols.Clear();
            revealedBorderSymbols?.Clear(); // Clear revealed symbols on reset
            clickCounts = new int[GridSize, GridSize];
            completePuzzle = new string[GridSize, GridSize]; // Assign a new empty 2D array - FIX for Warning CS8625
            allBorderSymbols?.Clear(); // Clear all border symbols
        }

        // Simple retrieval of the text content of a cell.
        private string GetCellContent(int row, int col)
        {
            TangoButton button = GetButton(row, col);
            return button?.Content?.ToString() ?? "";
        }

        // Retrieves the button at the specified grid location.
        private TangoButton GetButton(int row, int col)
        {
            try
            {
                return (TangoButton)GameGrid.Children
                    .Cast<UIElement>()
                    .First(e => Grid.GetRow(e) == row && Grid.GetColumn(e) == col);
            }
            catch (InvalidOperationException)
            {
                throw new ArgumentException($"No button found at position ({row}, {col})");
            }
        }

        // Highlights the provided cells by setting a red border.
        private void HighlightInvalidMoves(List<(int row, int col)> errorCells)
        {
            ClearInvalidMoveHighlights();

            foreach (var (row, col) in errorCells)
            {
                var button = GetButton(row, col);
                DrawDiagonalLines(button); // Call new method to draw lines
            }
        }

        private void DrawDiagonalLines(TangoButton button)
        {
            double startMargin = 10; // Increased margin from corners for shorter lines
            double endMargin = 10;
            double thickness = 2; // Thinner lines

            Line line1 = new Line
            {
                X1 = startMargin,
                Y1 = startMargin,
                X2 = button.ActualWidth - endMargin,
                Y2 = button.ActualHeight - endMargin,
                Stroke = Brushes.Red,
                StrokeThickness = thickness // Use thinner thickness
            };

            Line line2 = new Line
            {
                X1 = button.ActualWidth - endMargin,
                Y1 = startMargin,
                X2 = startMargin,
                Y2 = button.ActualHeight - endMargin,
                Stroke = Brushes.Red,
                StrokeThickness = thickness // Use thinner thickness
            };

            // Lines need to be added to the GameGrid, not directly to the button, to overlay correctly
            GameGrid.Children.Add(line1);
            GameGrid.Children.Add(line2);

            // Store lines in button's tag for easy removal later
            button.Tag = new List<Line> { line1, line2 };

            Grid.SetRow(line1, button.Row);
            Grid.SetColumn(line1, button.Col);
            Grid.SetRow(line2, button.Row);
            Grid.SetColumn(line2, button.Col);

            button.BorderBrush = Brushes.Red; // Re-add red border highlighting
            button.BorderThickness = new Thickness(2); // Thicker red border for errors
        }


        // Clears error highlighting, restoring the persistent black borders.
        private void ClearInvalidMoveHighlights()
        {
            List<Line> linesToRemove = new List<Line>(); // Create a list to store lines to remove

            foreach (var child in GameGrid.Children.OfType<TangoButton>())
            {
                if (child.Tag is List<Line> lines)
                {
                    linesToRemove.AddRange(lines); // Add lines to the removal list
                    child.Tag = null; // Clear the tag
                }
                child.BorderBrush = Brushes.Black; // Restore black border
                child.BorderThickness = new Thickness(1);
            }

            foreach (var line in linesToRemove)
            {
                GameGrid.Children.Remove(line); // Remove lines AFTER iterating through TangoButtons
            }
        }

        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingComboBox) // Check if we are initializing - if so, do nothing
            {
                return; // Exit event handler if initializing
            }

            string? selectedDifficulty = (DifficultyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (!string.IsNullOrEmpty(selectedDifficulty))
            {
                SetDifficulty(selectedDifficulty);

                // UPDATE COMBOBOX DISPLAY HERE:
                foreach (ComboBoxItem item in DifficultyComboBox.Items) // Iterate through ComboBox items
                {
                    if (item.Content.ToString() == selectedDifficulty) // Find the matching item
                    {
                        DifficultyComboBox.SelectedItem = item; // Set it as the SelectedItem - THIS IS THE FIX
                        break; // Exit loop once found
                    }
                }

                StartNewGame(); // Restart the game with the new difficulty
            }
        }

        private void StartNewGame()
        {
            //Console.WriteLine("StartNewGame() called - Difficulty: " + _currentDifficulty); // Debugging with difficulty

            GeneratePuzzle(_currentDifficulty); // Call GeneratePuzzle and pass the current difficulty
        }

        private void SetDifficulty(string difficulty)
        {
            _currentDifficulty = difficulty;
            // You can add more logic here later to adjust game parameters based on difficulty
           //Console.WriteLine($"Difficulty set to: {_currentDifficulty}"); // For debugging
        }

        // Checks whether the puzzle is complete and valid.
        // If so, locks the board and notifies the user.
        private void CheckWinCondition()
        {
            bool allFilled = GameGrid.Children.OfType<TangoButton>().All(b => !string.IsNullOrEmpty(b.Content?.ToString()));
            bool allValid = Enumerable.Range(0, GridSize).All(i =>
                CheckBalance(i, 0) && CheckBalance(0, i) &&
                Enumerable.Range(0, GridSize).All(j => CheckSpecialSymbols(i, j)));

            if (allFilled && allValid)
            {
                MessageBox.Show("Congratulations! You've solved the puzzle!");
                LockBoard();
            }
        }

        // Locks the board so that no further changes are allowed.
        private void LockBoard()
        {
            foreach (var button in GameGrid.Children.OfType<TangoButton>())
            {
                button.IsEnabled = false;
            }
        }

        // Checks balance in a row or column (ensuring no more than half of the cells are Suns or Moons).
        private bool CheckBalance(int row, int col)
        {
            bool CheckLine(IEnumerable<string> line) =>
                line.Count(c => c == Sun) <= GridSize / 2 &&
                line.Count(c => c == Moon) <= GridSize / 2;

            return CheckLine(Enumerable.Range(0, GridSize).Select(i => GetCellContent(row, i))) &&
                   CheckLine(Enumerable.Range(0, GridSize).Select(i => GetCellContent(i, col)));
        }

        // Reset button click handler.
        private void ResetGame_Click(object sender, RoutedEventArgs e)
        {
            StartNewGame(); // Restart the game with the current difficulty - calls StartNewGame now
        }
    }
}