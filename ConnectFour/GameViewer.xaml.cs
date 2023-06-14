using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Threading;
using System.Threading.Tasks;
using GlassExtension;
using System.ComponentModel;

using NeuralNet;
using ConnectFour.Properties;


namespace ConnectFour
{
    public enum GameMode { HumanVHuman, HumanVComputer, ComputerVComputer }

    /// <summary>
    /// Interaction logic for GameViewer.xaml
    /// </summary>
    public partial class GameViewer : Window
    {
        public GameMode Mode;
        public Board CurrentBoard;
        public Simulator Simulator;
        public Bot Bot;

        public Log Log = new Log();
        public Menu Menu = new Menu();
        public GameViewer()
        {
            InitializeComponent();
            Restart(GameMode.HumanVComputer, true);
            Simulator = new Simulator(this);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Glass.ExtendGlassFrame(this);
        }

        public void Restart(GameMode mode, bool startup = false)
        {
            gridBoard.Children.RemoveRange(7, gridBoard.Children.Count - 7); // Don't remove the 7 borders.
            Mode = mode;
            Checker = Checker.Blue;
            CurrentBoard = new Board();
            if (!startup)
                GetValidNetwork(); // Assert valid current network be means of a messagebox.
        }

        public Network GetValidNetwork()
        {
            if (Mode == GameMode.HumanVComputer || Mode == GameMode.ComputerVComputer)
                if (Settings.Default.CurrentNetwork == null)
                {
                    MessageBox.Show("Current network path is null or invalid.\r\nLoad a new network in settings.");
                    Menu.UpdateNetworkPathLabel();
                    return null;
                }
            return Settings.Default.CurrentNetwork;
        }

        private void HumanVHuman_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Restart(GameMode.HumanVHuman);
        }

        private void HumanVComputer_MouseUp(object sender, MouseButtonEventArgs e)
        {

            Restart(GameMode.HumanVComputer);
        }

        private void ComputerVComputer_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Restart(GameMode.ComputerVComputer);
            if (Settings.Default.CurrentNetwork != null)
                Simulator.Play(TrainingRegimen.Blank(), Settings.Default.CurrentNetwork);
        }

        private Checker Checker = Checker.Blue;
        private void Border_MouseUp(object sender, MouseEventArgs e)
        {
            if (CurrentBoard.IsGameOver)
                return;

            Network network = GetValidNetwork();
            if (network != null)
                Bot = new NeuralNetBot(Checker.Green, network, 0);
            else if (Mode != GameMode.HumanVHuman)
                return;


            int column = (int)(sender as Border).GetValue(Grid.ColumnProperty);
            if (IsColumnFull(column))
                return;

            if (Mode == GameMode.HumanVComputer)
            {
                double score;
                int column2;
                CurrentBoard.AddChecker(Checker, column);
                if (CurrentBoard.IsGameOver)
                {
                    AddChecker(Checker, column, updateBoard: false);
                    GameOverAnimation();
                    return;
                }
                Bot.SelectMove(CurrentBoard, out column2, out score);
                CurrentBoard.AddChecker(Board.Toggle(Checker), column2);
                BatchAddCheckers(Checker, new List<int> { column, column2 }, updateBoard: false);
            }
            else
            {
                AddChecker(Checker, column);
                Checker = Board.Toggle(Checker);
            }

            if (CurrentBoard.IsGameOver)
                GameOverAnimation();
        }

        public void GameOverAnimation()
        {
            if (!CurrentBoard.IsGameOver)
                return;

            Storyboard story = new Storyboard();
            for (int i = 0; i < CurrentBoard.Rows; ++i)
                for (int j = 0; j < CurrentBoard.Columns; ++j)
                {
                    if (!CurrentBoard.WinningSequence.Any(t => t.Item1 == i && t.Item2 == j))
                    {
                        Image image = gridBoard.Children.OfType<Image>().Where(e => (int)e.GetValue(Grid.RowProperty) == i && (int)e.GetValue(Grid.ColumnProperty) == j).FirstOrDefault();
                        if (image == null)
                            continue;
                        story.Children.Add(Fade(image, 1, Settings.Default.FadeTo, 0, Settings.Default.FadeSpeed));
                    }
                }
            story.Begin();
        }


        DoubleAnimation Fade(Image image, double from, double to, int beginTime, int duration)
        {
            DoubleAnimation fade = new DoubleAnimation(from, to, TimeSpan.FromMilliseconds(duration));
            fade.BeginTime = TimeSpan.FromMilliseconds(beginTime);
            Storyboard.SetTarget(fade, image);
            Storyboard.SetTargetProperty(fade, new PropertyPath(Image.OpacityProperty));
            return fade;
        }


        public void AddChecker(Checker checker, int column, bool isGameOver = false, bool updateBoard = true)
        {
            BatchAddCheckers(checker, new List<int> { column }, updateBoard: updateBoard, delay: false, completedBoard: null);
        }

        public void BatchAddCheckers(Checker start, List<int> columnHistory, bool updateBoard = true, bool delay = true, Board completedBoard = null)
        {
            Storyboard story = new Storyboard();
            Checker checker = start;
            int i = 0;
            foreach (int column in columnHistory)
            {
                Image image = new Image();
                image.Stretch = Stretch.Uniform;
                image.Source = (checker == Checker.Blue ? new BitmapImage(new Uri("/Icons/orbz_water.ico", UriKind.Relative)) : new BitmapImage(new Uri("/Icons/orbz_spirit.ico", UriKind.Relative)));
                image.SetValue(Grid.ColumnProperty, column);
                int? minRow = gridBoard.Children.OfType<Image>().Where(e => (int)e.GetValue(Grid.ColumnProperty) == column).Select(e => (int?)e.GetValue(Grid.RowProperty)).Min();
                if (minRow.HasValue && minRow == 0)
                    throw new Exception("Cannot add checker to full column");
                int row = (int)(minRow.HasValue ? minRow - 1 : 5);
                image.SetValue(Grid.ZIndexProperty, -1);
                image.SetValue(Grid.RowProperty, row);
                image.Opacity = 0;
                image.Height = gridBoard.RowDefinitions[0].ActualHeight;
                gridBoard.Children.Add(image);
                if (updateBoard)
                    CurrentBoard.AddChecker(checker, column);

                ThicknessAnimation animation = new ThicknessAnimation(new Thickness(0, -gridBoard.ActualHeight * 2 * Settings.Default.DropHeightRatio, 0, 0), new Thickness(0, 0, 0, 0), TimeSpan.FromMilliseconds(Settings.Default.DropSpeed));
                animation.EasingFunction = new BounceEase() { Bounces = 3, Bounciness = 5, EasingMode = System.Windows.Media.Animation.EasingMode.EaseOut };
                animation.BeginTime = TimeSpan.FromMilliseconds(i * Settings.Default.MoveDelay);
                Storyboard.SetTarget(animation, image);
                Storyboard.SetTargetProperty(animation, new PropertyPath(Image.MarginProperty));
                story.Children.Add(animation);

                DoubleAnimation fade = (completedBoard != null && !completedBoard.WinningSequence.Any(t => t.Item1 == row && t.Item2 == column) ? Fade(image, 1, Settings.Default.FadeTo, i * Settings.Default.MoveDelay, Settings.Default.FadeSpeed) : Fade(image, 0, 1, i * Settings.Default.MoveDelay, 0));
                story.Children.Add(fade);
                story.Completed += new EventHandler(story_Completed);

                checker = Board.Toggle(checker);
                ++i;
            }
            story.Begin();
        }

        public bool IsColumnFull(int column)
        {
            int? minRow = gridBoard.Children.OfType<Image>().Where(e => (int)e.GetValue(Grid.ColumnProperty) == column).Select(e => (int?)e.GetValue(Grid.RowProperty)).Min();
            return (minRow.HasValue && minRow == 0);
        }

        void story_Completed(object sender, EventArgs e)
        {
            Storyboard story = (Storyboard)(sender as ClockGroup).Timeline;
            foreach (var child in story.Children)
            {
                Image image = (Image)Storyboard.GetTarget(child);
                image.ClearValue(Image.HeightProperty); // Keeps images resizable if window is resized.
            }
        }

        private void ToggleLog_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Log.IsVisible)
                Log.Hide();
            else
                Log.Show();
        }

        private void ToggleSettings_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (Menu.IsVisible)
                Menu.Hide();
            else
                Menu.Show();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            Settings.Default.Save();
            Log.ForceClose();
            Menu.ForceClose();
        }
    }
}
