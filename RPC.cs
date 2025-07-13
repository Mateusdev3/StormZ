using System;
using DiscordRPC;
using Button = DiscordRPC.Button;

namespace StormZ {
    public class RPC {
        public static DiscordRpcClient client;
        private static RichPresence presence;
        public static Timestamps rpctimestamp { get; set; }

        public static void InitializeRPC(string nk, string ur, string map) {
            client = new DiscordRpcClient("1393824357884625047");
            client.Initialize();

            rpctimestamp = new Timestamps { Start = DateTime.UtcNow };

            var buttons = new Button[]
               {
                    new Button { Label = "💬 Discord", Url = ur },
                    new Button { Label = "🌐 Site", Url = "https://exemplo.com" }
               };


            presence = new RichPresence
            {
                State = $"Nick: {nk}",
                Details = $"Mapa: {map}",
                Timestamps = rpctimestamp,
                Buttons = buttons,
                Assets = new Assets
                {
                    LargeImageKey = "logo1",
                    LargeImageText = "StormZ"
                }
            };

            if (client.IsInitialized)
                client.SetPresence(presence);
        }

        public static void SetState(string state, bool watching = false) {
            if (watching)
                state = "Venha para o StormZ " + state;

            if (presence != null)
            {
                presence.State = state;
                if (client != null && client.IsInitialized)
                    client.SetPresence(presence);
            }
        }

        public static void ShutdownRPC() {
            if (client != null && client.IsInitialized)
            {
                client.ClearPresence();
                client.Dispose();
            }
        }
    }
}
