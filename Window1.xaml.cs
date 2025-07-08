using System.Windows;
using System.Windows.Input;
using Application = System.Windows.Application;

namespace StormZ {
    public partial class Window1 : Window {
        public string NomeDigitado { get; private set; }
        public string nickrecebido;

        public Window1(string Nickname) {
            InitializeComponent();
            nickrecebido = Nickname;


        }

        private void Confirmar(object sender, RoutedEventArgs e) {
            NomeDigitado = txtNome.Text;
            int length = NomeDigitado.Trim().Length;
            if (length > 3)
            {
                this.DialogResult = true;
            }
            else
            {
                System.Windows.MessageBox.Show("Por favor, digite um nick válido.", "Nick inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
        private void Mouse(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void CloseWindow(object sender, RoutedEventArgs e) {
           
            if (nickrecebido == "Sem nick")
            {
                System.Windows.MessageBox.Show("Por favor, digite um nick válido.", "Nick inválido", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                this.Close();
            }

            }
        }
}
