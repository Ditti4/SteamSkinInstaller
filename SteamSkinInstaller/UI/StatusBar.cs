using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;

namespace SteamSkinInstaller.UI {
    internal class StatusBar {
        private readonly TextBlock _statusText;
        private readonly Rectangle _statusProgress;
        private readonly DockPanel _root;

        public string StatusText {
            get { return _statusText.Text; }
            set { _statusText.Text = value; }
        }

        public int StatusProgress {
            get { return (int)(_statusProgress.Width / 2.5); }
            set { _statusProgress.Width = (int)(value * 2.5); }
        }

        public bool Visible {
            get { return _root.Visibility == Visibility.Visible; }
            set { _root.Visibility = value ? Visibility.Visible : Visibility.Hidden; }
        }

        public bool ProgressVisible {
            get { return _statusProgress.Visibility == Visibility.Visible; }
            set { _statusProgress.Visibility = value ? Visibility.Visible : Visibility.Hidden; }
        }

        public StatusBar() {
            BrushConverter brushConverter = new BrushConverter();
            Thickness progressThickness = new Thickness(25, 15, 25, 15);
            Grid progressGrid = new Grid();

            _root = new DockPanel {
                Width = 300,
                Height = 100,
                VerticalAlignment = VerticalAlignment.Bottom,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20),
                Background = (SolidColorBrush) brushConverter.ConvertFrom("#212121"),
                Visibility = Visibility.Hidden,
                Effect = new DropShadowEffect {
                    BlurRadius = 20,
                    ShadowDepth = 1
                }
            };

            _statusText = new TextBlock {
                TextWrapping = TextWrapping.Wrap,
                Foreground = (SolidColorBrush) brushConverter.ConvertFrom("#FFFFFF"),
                Margin = new Thickness(20, 15, 20, 15)
            };

            Rectangle progressBackground = new Rectangle {
                Height = 5,
                Width = 250,
                Fill = (SolidColorBrush) brushConverter.ConvertFrom("#FFFFFF"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = progressThickness
            };

            _statusProgress = new Rectangle {
                Height = 5,
                Width = 0,
                Fill = (SolidColorBrush) brushConverter.ConvertFrom("#FB8C00"),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = progressThickness
            };

            DockPanel.SetDock(_statusText, Dock.Top);
            DockPanel.SetDock(progressGrid, Dock.Bottom);

            progressGrid.Children.Add(progressBackground);
            progressGrid.Children.Add(_statusProgress);

            _root.Children.Add(_statusText);
            _root.Children.Add(progressGrid);
        }

        public DockPanel ToDockPanel() {
            return _root;
        }

        public static implicit operator DockPanel(StatusBar statusBar) {
            return statusBar.ToDockPanel();
        }
    }
}
