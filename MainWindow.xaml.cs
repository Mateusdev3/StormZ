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
using Microsoft.Win32;
using System.Text.RegularExpressions;

namespace StormZ {
    public partial class MainWindow : Window {

        string pastamods = "Mods";
        private DispatcherTimer timer;
        private DispatcherTimer loopTimer;
        public string dayzexe = "DayZ_x64.exe";
        public string dayzprocessname = "DayZ_x64";
        public string NickName = string.Empty;
        public string PcName = string.Empty;
        public string UserName = string.Empty;
        public string Sistema = string.Empty;
        public string IpLocal = string.Empty;
        public string Cpu = string.Empty;
        public string Hd = string.Empty;
        public string Gpu = string.Empty;
        public string GpuSerial = string.Empty;
        public string RamSerial = string.Empty;
        public string BoardSerial = string.Empty;
        public string Horario = string.Empty;
        public string arqn = "nick.txt";
        public string stid = GetSteamId();
        public string versaolauncher = "1.0";
      

        public MainWindow() {

            // Inicializa o componente e as configurações

            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }
       private async void MainWindow_Loaded(object sender, RoutedEventArgs e) {
            await InicializarConfiguracoes();
            if (File.Exists(arqn))
                txtnick.Text = File.ReadAllText(arqn).Trim();
            else
                txtnick.Text = "Sem nick";
            Application.Current.Properties["NickName"] = "Sem nick";

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
           
          
            await PrimaryName();
            _ = SendIds();
            await Task.Delay(1000);
            CheckManutence();
            CloseIfnoConect(dayzprocessname);
            LoopCheck();
            IniciarEnvioAutomatico();
        }

        public class Config {

            // Propriedades para armazenar as configurações remotas

            public string IpServer { get; set; }
            public string PortServer { get; set; }
            public string Urlw { get; set; }
            public string Webl { get; set; }
            public string Webp { get; set; }
            public string Webt { get; set; }
            public string Webba { get; set; }
            public string Webha { get; set; }
            public int Temp { get; set; }
            public string Version { get; set; }
            public string Discord { get; set; }
            public string Manutencao { get; set; }
        }
        public class PlayerInfo {
            public string NICK { get; set; }
            public string HWID { get; set; }
            public string RAMID { get; set; }
            public string COMPUTERID { get; set; }
            public string CPUID { get; set; }
            public string GPUID { get; set; }
            public string BOARDID { get; set; }
        }

        private string IpServer;
        private string PortServer;
        private string Urlw;
        private string Webl;
        private string Webp;
        private string Webt;
        private string Webba;
        private string Webha;
        private int Temp;
        private string Version;
        private string Discord;
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
            Urlw = config.Urlw;
            Webl = config.Webl;
            Webp = config.Webp;
            Webt = config.Webt;
            Webba = config.Webba;
            Webha = config.Webha;
            Temp = config.Temp;
            Version = config.Version;
            Discord = config.Discord;
            Manutencao = config.Manutencao;
            CheckVersion();
        }
        // Método para iniciar o envio automático de informações e tasklist a cada 30 segundos
        private void IniciarEnvioAutomatico() {
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(Temp);
            timer.Tick += async (s, e) =>
            {
                await EnviarScren();
                await EnviarTasklistParaWebhook();  
            };
            timer.Start();
        }
        private void LoopCheck() {
            loopTimer = new DispatcherTimer();
            loopTimer.Interval = TimeSpan.FromSeconds(30);
            loopTimer.Tick += LoopDayz;
            loopTimer.Start();
        }

        private async void LoopDayz(object sender, EventArgs e) {
            CloseIfnoConect(dayzprocessname);
            if (!CheckProcess(dayzprocessname))
            {
              Whitelistremove();
            }
        }
        private void CheckVersion() {
            if (Version != versaolauncher)
            {
                MessageBox.Show($"O launcher foi atualizado, por favor baixe a nova versão:{Version}.", "Atualização", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
            }
        }
        public async void CloseIfnoConect(string process) {
            if (!CheckCoenction())
            {
                MessageBox.Show("Você não está conectado à internet, o launcher será fechado.", "Sem conexão", MessageBoxButton.OK, MessageBoxImage.Error);

                KillProcess(process);
                Application.Current.Shutdown();
            }
        }

        public static bool CheckCoenction() {
            try
            {
                using (var client = new WebClient())
                using (client.OpenRead("http://clients3.google.com/generate_204"))
                {
                    return true; 
                }
            }
            catch
            {
                return false;
            }
        }

        // Evento de clique do botão "Jogar" que inicia o jogo e envia as informações para o webhook
        private async void btnJogar_Click(object sender, RoutedEventArgs e) {
            
            txtnick.Text = File.ReadAllText(arqn).Trim();
            string mods = ObterMods(pastamods);
            EnviarInfosParaWebhook();
            EnviarScren();
            EnviarTasklistParaWebhook();
            Whitelistadd();
            OpenDayz();
            
        }
        private void OpenDayz() {
            if (File.Exists(dayzexe))
            {
                string mods = ObterMods(pastamods);
                string argumentos;
                argumentos = String.Format("-name={0} \"-mod={1}\" -connect={2} -port={3} -noFilePatching", NickName, mods, IpServer, PortServer);
                Process.Start(dayzexe, argumentos);
            }
            else
            {
                MessageBox.Show("O arquivo DayZ_x64.exe não foi encontrado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
        public void CheckManutence() {
            if (Manutencao == "true")
            {
                MessageBox.Show("O servidor está em manutenção, tente novamente mais tarde.", "Manutenção", MessageBoxButton.OK, MessageBoxImage.Information);
                Application.Current.Shutdown();
                
            }
        }
        public async Task SendIds() {
            var player = new PlayerInfo
            {
                NICK = NickName,
                HWID = Hd,
                RAMID = RamSerial,
                COMPUTERID = PcName,
                CPUID = Cpu,
                GPUID = GpuSerial,
                BOARDID = BoardSerial,
            };

            string json = JsonSerializer.Serialize(player);

            using var httpClient = new HttpClient();
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync($"{Urlw}status", content);
            string resposta = await response.Content.ReadAsStringAsync();
            string respostareform = resposta.Replace("\"","");
            if (respostareform == "BANIDO")
            {
                try
                {
                    using (var client = new HttpClient())
                    {
                        using (var form1 = new MultipartFormDataContent())
                        {
                            var payload = new
                            {
                                embeds = new[]
                                {
                                new
                                {
                                    title = "**PLAYER BANIDO TENTANDO LOGAR **",
                                    color = 16711680,
                                    fields = new[]
                                    {
                                        new { name = "\ud83d\udc64 NICK", value = NickName, inline = false },
                                        new { name = "\ud83d\udc64 STEAMID", value = stid, inline = false },
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

                            await client.PostAsync(Webba, form1);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Erro ao enviar dados: {ex.Message}");
                }
                MessageBox.Show("Você foi banido do servidor, entre em contato com o administrador do servidor para mais informações.", "Banido", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                this.Close();
            }
        }

        public async Task PrimaryName() {
            if(NickName == "Sem nick")
            {
                AbrirJanelaNome_Click(null, null);
  
            }
        }

        private async void Whitelistadd() {
            try
            {
                string steamId = GetSteamId();
                string url = Urlw;

                using var client = new HttpClient();
                string resquisicao = $"{url}add/{steamId}";
                HttpResponseMessage response = await client.GetAsync(resquisicao);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();
                string conteudo = await response.Content.ReadAsStringAsync();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao adicionar à whitelist: {ex.Message}");
                
            }
        }

        private async void Whitelistremove() {
            try
            {
                string steamId = GetSteamId();
                string url = Urlw;
                using var client = new HttpClient();
                string resquisicao = $"{url}remove/{steamId}";
                HttpResponseMessage response = await client.GetAsync(resquisicao);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                string conteudo = await response.Content.ReadAsStringAsync();

            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao remover da whitelist: {ex.Message}");
              
            }
        }

        // Método para obter o Steam ID do usuário a partir do registro do Windows

        public static string GetSteamId() {
            try
            {
                string registryPath = @"HKEY_CURRENT_USER\Software\Valve\Steam";
                object steamPathObj = Registry.GetValue(registryPath, "SteamPath", null);

                if (steamPathObj != null)
                {
                    string steamPath = steamPathObj.ToString();
                    string loginUserpath = Path.Combine(steamPath, "config", "loginusers.vdf");

                    if (!File.Exists(loginUserpath))
                        return null;

                    string content = File.ReadAllText(loginUserpath);

                    Regex regex = new Regex("\"(\\d{17})\"\\s*\\{[^}]*\"MostRecent\"\\s*\"1\"", RegexOptions.Singleline);
                    Match match = regex.Match(content);

                    if (match.Success)
                    {
                        return match.Groups[1].Value;
                    }
                    else
                    {
                        Debug.WriteLine("Steam ID não encontrado no arquivo loginusers.vdf.");
                        return null;
                    }
                }
            }
            catch
            {
                Console.WriteLine("Erro ao obter o Steam ID: ");
            }
            return null;

        }
        // Método para enviar as informações do sistema e captura de tela para o webhook do Discord
        private async Task EnviarInfosParaWebhook() {
            try
            {
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
                                        new { name = "\ud83d\udc64 STEAMID", value = stid, inline = false },
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

                        await client.PostAsync(Webl, form1);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao enviar dados: {ex.Message}");
            }
        }

        private async Task EnviarScren() {
            try
            {

                string screenshotPath = CapturarTela();
                using (var client = new HttpClient())
                    if (!string.IsNullOrEmpty(screenshotPath) && File.Exists(screenshotPath))
                    {
                        using (var form2 = new MultipartFormDataContent())
                        {
                            var imageContent = new ByteArrayContent(File.ReadAllBytes(screenshotPath));
                            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                            form2.Add(imageContent, "file", "screenshot.png");

                            form2.Add(new StringContent($"### {NickName}"), "content");

                            await client.PostAsync(Webp, form2);
                        }
                    }
            } 
            catch (Exception ex)
            {
                Debug.WriteLine($"Erro ao enviar screenshot: {ex.Message}");
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

                    await client.PostAsync(Webt, form);
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

        private void Close_Click(object sender, RoutedEventArgs e) {
            Whitelistremove();
            _ = KillProcess(dayzprocessname);
            Application.Current.Shutdown();
        }

        private void Mouse(object sender, MouseButtonEventArgs e) {
            if (e.LeftButton == MouseButtonState.Pressed)
                this.DragMove();
        }

        private void Abrirdc(object sender, RoutedEventArgs e) {
            try
            {
                Process.Start(new ProcessStartInfo(Discord) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o Discord: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Abrirconfig(object sender, RoutedEventArgs e) => Debug.WriteLine("Abrindo config");



        // Evento de clique do botão para abrir a janela de nome
        private void AbrirJanelaNome_Click(object sender, RoutedEventArgs e) {
            Window1 janelaNome = new Window1(txtnick.Text);
            if (janelaNome.ShowDialog() == true)
            {
                File.WriteAllText("nick.txt", janelaNome.NomeDigitado);
                txtnick.Text = janelaNome.NomeDigitado;
                NickName = janelaNome.NomeDigitado;
            }
        }

        // Evento de clique do botão para definir o nick

        private void Nick(object sender, RoutedEventArgs e) {
            Window1 janela = new Window1(txtnick.Text);
            if (janela.ShowDialog() == true)
            {
                txtnick.Text = janela.NomeDigitado;
                NickName = janela.NomeDigitado;
            }
        }

        private bool CheckProcess(string nameprocess) {
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(nameprocess))
            {
                if (!process.HasExited)
                {

                    return true;
                } 
            }
             return false;
        }
        
        public async Task KillProcess(string nameprocess) {
            foreach (var process in System.Diagnostics.Process.GetProcessesByName(nameprocess))
            {
                if (!process.HasExited)
                {
                    process.Kill();
                }
            }

        }

    }
}