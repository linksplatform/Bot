using Discord;
using Discord.WebSocket;
using System;
using System.Threading.Tasks;

namespace LinksPlatformDiscordBot
{
    public class BotStartup
    {
        private Storage.Local.FileStorage LinksStorage { get; set; }

        private string Token { get; set; }

        private DiscordSocketClient Client { get; set; }

        public BotStartup(string token, Storage.Local.FileStorage storage)
        {
            LinksStorage = storage;
            Client = new DiscordSocketClient();
            Token = token;
        }
        public async Task StartupAsync()
        {
            Client.Log += Log;
            Client.MessageReceived += MessageReceived;
            await Client.LoginAsync(TokenType.Bot, Token);
            await Client.StartAsync();

            await Task.Delay(-1);
        }
        private async Task MessageReceived(SocketMessage message)
        {
            if (message.Content.Contains("https://github.com"))
            {
                if (message.Channel.Name == "main")
                    await message.Channel.SendMessageAsync("@konard look at this dude");
            }
            if (message.Content.Contains("accept"))
            {
                if (message.Author.Username == "FirstAfterGod")
                {
                    await message.Channel.SendMessageAsync("accepted.");
                    string link = message.Channel.GetMessageAsync(message.Reference.MessageId.Value).Result.Content;
                    LinksStorage.AddLinkToIvite(link);
                    foreach (var a in LinksStorage.GetLinksToInvite())
                    {
                        Console.WriteLine(a);
                    }
                }
            }
        }
        public void CreateInvite(string link)
        {

        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}