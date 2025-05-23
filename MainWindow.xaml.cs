using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TangoGame
{
    public partial class MainWindow : Window
    {
        private GameLogic _gameLogic;
        private System.Windows.Threading.DispatcherTimer _errorTimer;
        private List<(int row, int col)> _currentErrorCells = new List<(int row, int col)>();
        private bool _isInitializingComboBox = false;
        private Random _randomForHintChoice = new Random(); // For HintButton_Click logic

        public MainWindow()
        {
            InitializeComponent();
            _gameLogic = new GameLogic();

            _errorTimer = new System.Windows.Threading.DispatcherTimer();
            _errorTimer.Interval = TimeSpan.FromSeconds(1); // Set delay to 1 second
            _errorTimer.Tick += ErrorTimer_Tick;
            
            _isInitializingComboBox = true; // Prevent event firing during setup
            DifficultyComboBox.Items.Clear();
            DifficultyComboBox.Items.Add(new ComboBoxItem { Content = "Easy" });
            DifficultyComboBox.Items.Add(new ComboBoxItem { Content = "Medium" });
            DifficultyComboBox.Items.Add(new ComboBoxItem { Content = "Hard" });
            // Set default difficulty in GameLogic and reflect it in ComboBox
            _gameLogic.SetDifficulty("Medium"); 
            DifficultyComboBox.SelectedIndex = 1; // Select "Medium"
            _isInitializingComboBox = false;

            InitializeGameBoardUI();
            StartNewGameUI();
        }

        private void InitializeGameBoardUI()
        {
            GameGrid.Children.Clear(); // Clear existing buttons if any (e.g., from a previous game)
            for (int row = 0; row < GameLogic.GridSize; row++)
            {
                for (int col = 0; col < GameLogic.GridSize; col++)
                {
                    var button = new TangoButton
                    {
                        Content = "",
                        FontSize = 20,
                        Row = row,
                        Col = col,
                        BorderBrush = Brushes.Black,
                        BorderThickness = new Thickness(1),
                        Background = Brushes.White,
                        Foreground = Brushes.Black
                    };
                    button.Click += GridButton_Click;
                    Grid.SetRow(button, row);
                    Grid.SetColumn(button, col);
                    GameGrid.Children.Add(button);
                }
            }
        }

        private void StartNewGameUI()
        {
            _gameLogic.GeneratePuzzle(_gameLogic.GetCurrentDifficulty());
            ClearGameBoardUI(); // Reset all cells to empty and enabled

            var cellHints = _gameLogic.RevealInitialHints(_gameLogic.GetCurrentDifficulty());
            foreach (var hint in cellHints)
            {
                RevealCellUI(hint.row, hint.col, hint.content);
            }

            // GameLogic now manages revealedBorderSymbols internally after RevealInitialSymbolHints
            _gameLogic.RevealInitialSymbolHints(_gameLogic.GetCurrentDifficulty());
            DrawBorderSymbolsUI();
            ValidateBoardUI(); // Validate and highlight any initial errors if necessary (though unlikely for new puzzle)
        }
        
        private void ClearGameBoardUI()
        {
            foreach (TangoButton button in GameGrid.Children.OfType<TangoButton>())
            {
                button.Content = "";
                button.IsEnabled = true;
                button.Background = Brushes.White;
                button.Foreground = Brushes.Black;
                button.BorderBrush = Brushes.Black;
                button.BorderThickness = new Thickness(1);
                if (button.Tag is List<Line> lines)
                {
                    foreach (var line in lines)
                    {
                        GameGrid.Children.Remove(line);
                    }
                    button.Tag = null;
                }
            }
            // Also clear any drawn border symbols (textblocks)
            GameGrid.Children.OfType<TextBlock>().ToList().ForEach(tb => GameGrid.Children.Remove(tb));
        }

        private void GridButton_Click(object sender, RoutedEventArgs e)
        {
            TangoButton clickedButton = (TangoButton)sender;
            int row = clickedButton.Row;
            int col = clickedButton.Col;

            string newContent = _gameLogic.ProcessCellClick(row, col);
            clickedButton.Content = newContent;
            clickedButton.Foreground = newContent switch
            {
                GameLogic.Sun => Brushes.Gold,
                GameLogic.Moon => Brushes.DodgerBlue,
                _ => Brushes.Black
            };

            ValidateBoardUI();
            CheckWinConditionUI();
        }

        private void ValidateBoardUI()
        {
            _currentErrorCells = _gameLogic.ValidateBoard(GetCellContentUI);
            if (!_errorTimer.IsEnabled) // Or handle immediate highlighting
            {
                 _errorTimer.Start();
            }
             // If you want immediate highlighting without timer:
            // if (_currentErrorCells.Any())
            // {
            //     HighlightInvalidMovesUI(_currentErrorCells);
            // }
            // else
            // {
            //     ClearInvalidMoveHighlightsUI();
            // }
        }

        private void ErrorTimer_Tick(object? sender, EventArgs e)
        {
            _errorTimer.Stop();
            if (_currentErrorCells.Any())
            {
                HighlightInvalidMovesUI(_currentErrorCells);
            }
            else
            {
                ClearInvalidMoveHighlightsUI();
            }
            // _currentErrorCells are processed, no need to clear them here as ValidateBoardUI will repopulate.
        }

        private void HintButton_Click(object sender, RoutedEventArgs e)
        {
            // Randomly choose between symbol hint and cell hint
            bool trySymbolHintFirst = _randomForHintChoice.Next(2) == 0;

            bool hintGiven = false;

            if (trySymbolHintFirst)
            {
                hintGiven = TryRevealSymbolHint();
                if (!hintGiven) // If symbol hint failed (e.g., all revealed), try cell hint
                {
                    hintGiven = TryRevealCellHint();
                }
            }
            else // Try cell hint first
            {
                hintGiven = TryRevealCellHint();
                if (!hintGiven) // If cell hint failed, try symbol hint
                {
                    hintGiven = TryRevealSymbolHint();
                }
            }

            if (!hintGiven)
            {
                MessageBox.Show("No more hints available or puzzle complete!");
            }
        }

        private bool TryRevealSymbolHint()
        {
            BorderSymbol? symbolToReveal = _gameLogic.RevealBorderSymbolHint();
            if (symbolToReveal != null)
            {
                DrawBorderSymbolsUI(); // Redraw all revealed symbols, including the new one
                ValidateBoardUI(); // Re-validate after hint
                return true;
            }
            return false;
        }

        private bool TryRevealCellHint()
        {
            var cellHint = _gameLogic.RevealCellHintFromSolution(IsCellEnabledAndEmptyUI);
            if (cellHint.HasValue)
            {
                RevealCellUI(cellHint.Value.row, cellHint.Value.col, cellHint.Value.content);
                ValidateBoardUI(); // Re-validate after hint
                CheckWinConditionUI(); // Check if hint completed puzzle
                return true;
            }
            return false;
        }
        
        private void RevealCellUI(int row, int col, string content)
        {
            TangoButton button = GetButton(row, col);
            if (button != null)
            {
                button.Content = content;
                button.IsEnabled = false; // Mark as not editable
                button.Background = Brushes.LightGray; // Style for revealed hints
                button.Foreground = content switch
                {
                    GameLogic.Sun => Brushes.Gold,
                    GameLogic.Moon => Brushes.DodgerBlue,
                    _ => Brushes.Black // Should not happen for Sun/Moon
                };
            }
        }

        private void DrawBorderSymbolsUI()
        {
            // Clear existing TextBlocks used for border symbols
            GameGrid.Children.OfType<TextBlock>().ToList().ForEach(textBlock => GameGrid.Children.Remove(textBlock));

            var symbolsToDraw = _gameLogic.GetRevealedBorderSymbols();
            if (symbolsToDraw != null)
            {
                foreach (var symbol in symbolsToDraw)
                {
                    var textBlock = new TextBlock
                    {
                        Text = symbol.Symbol,
                        Foreground = Brushes.Red,
                        FontSize = 19,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(0, -4.75, 0, 0) // Adjust margin as needed
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
        
        private string GetCellContentUI(int row, int col)
        {
            TangoButton button = GetButton(row, col);
            return button?.Content?.ToString() ?? "";
        }

        private bool IsCellEnabledAndEmptyUI(int row, int col)
        {
            TangoButton button = GetButton(row, col);
            if (button != null)
            {
                return button.IsEnabled && string.IsNullOrEmpty(button.Content?.ToString());
            }
            return false;
        }

        private TangoButton GetButton(int row, int col)
        {
            // This method can remain largely the same.
            // Ensure it correctly maps row/col to the UI element in GameGrid.
            foreach (UIElement element in GameGrid.Children)
            {
                if (element is TangoButton button && Grid.GetRow(button) == row && Grid.GetColumn(button) == col)
                {
                    return button;
                }
            }
            // Consider throwing an exception or returning null if not found, based on expected usage.
            // For robust error handling, throwing is often better if a button is expected.
            // throw new ArgumentException($"Button not found at row {row}, col {col}");
            return null; // Or handle as appropriate
        }

        private void HighlightInvalidMovesUI(List<(int row, int col)> errorCells)
        {
            ClearInvalidMoveHighlightsUI(); // Clear previous highlights first
            foreach (var (row, col) in errorCells)
            {
                TangoButton button = GetButton(row, col);
                if (button != null)
                {
                    DrawDiagonalLinesUI(button);
                }
            }
        }

        private void DrawDiagonalLinesUI(TangoButton button)
        {
            // This method's core drawing logic remains, ensure it adds lines to GameGrid
            // and associates them with the button (e.g., via Tag) for later removal.
            double startMargin = 10; 
            double endMargin = 10;
            double thickness = 2; 

            Line line1 = new Line
            {
                X1 = startMargin, Y1 = startMargin,
                X2 = button.ActualWidth - endMargin, Y2 = button.ActualHeight - endMargin,
                Stroke = Brushes.Red, StrokeThickness = thickness
            };
            Line line2 = new Line
            {
                X1 = button.ActualWidth - endMargin, Y1 = startMargin,
                X2 = startMargin, Y2 = button.ActualHeight - endMargin,
                Stroke = Brushes.Red, StrokeThickness = thickness
            };

            GameGrid.Children.Add(line1);
            GameGrid.Children.Add(line2);
            button.Tag = new List<Line> { line1, line2 }; // Store for removal

            Grid.SetRow(line1, button.Row); Grid.SetColumn(line1, button.Col);
            Grid.SetRow(line2, button.Row); Grid.SetColumn(line2, button.Col);
            
            button.BorderBrush = Brushes.Red;
            button.BorderThickness = new Thickness(2);
        }

        private void ClearInvalidMoveHighlightsUI()
        {
            List<Line> linesToRemove = new List<Line>();
            foreach (TangoButton button in GameGrid.Children.OfType<TangoButton>())
            {
                if (button.Tag is List<Line> lines)
                {
                    linesToRemove.AddRange(lines);
                    button.Tag = null;
                }
                // Reset to default border style
                button.BorderBrush = Brushes.Black;
                button.BorderThickness = new Thickness(1);
            }
            foreach (var line in linesToRemove)
            {
                GameGrid.Children.Remove(line);
            }
        }

        private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isInitializingComboBox) return;

            string? selectedDifficulty = (DifficultyComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            if (!string.IsNullOrEmpty(selectedDifficulty))
            {
                _gameLogic.SetDifficulty(selectedDifficulty);
                StartNewGameUI(); // This will generate a new puzzle with the new difficulty
            }
        }
        
        private void CheckWinConditionUI()
        {
            bool isWin = _gameLogic.CheckWinCondition(GetCellContentUI);
            if (isWin)
            {
                MessageBox.Show("Congratulations! You've solved the puzzle!");
                LockBoardUI();
            }
        }

        private void LockBoardUI()
        {
            foreach (TangoButton button in GameGrid.Children.OfType<TangoButton>())
            {
                button.IsEnabled = false;
            }
        }

        private void ResetGame_Click(object sender, RoutedEventArgs e)
        {
            // _gameLogic.ResetGameCore(); // GameLogic.GeneratePuzzle already resets the core state.
            // The difficulty is preserved in _gameLogic.
            StartNewGameUI();
        }
    }
}