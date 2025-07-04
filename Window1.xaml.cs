using System.Windows;
using System.Windows.Input;

namespace StormZ {
    public partial class Window1 : Window {
        public string NomeDigitado { get; private set; }

        public Window1() {
            InitializeComponent();

        }

        private void Confirmar(object sender, RoutedEventArgs e) {
            NomeDigitado = txtNome.Text;
            this.DialogResult = true;
        }
        private void Mouse(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void CloseWindow(object sender, RoutedEventArgs e) =>
            this.Close();
        
    }
}
