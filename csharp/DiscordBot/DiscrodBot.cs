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
            if (!message.Author.IsBot)
            {
                if (message.Content.Contains("https://github.com"))
                {
                    if (message.Channel.Name == "main")
                        await message.Channel.SendMessageAsync("@konard would we accept " + "@" + message.Author.Username + " as our new team member?");
                }
                if (message.Content.Contains("accept"))
                {
                    if (message.Author.Username == "Konstantin Dyachenko" || message.Author.Username == "konard" || message.Author.Username == "FirstAfterGod")
                    {
                        string link = message.Channel.GetMessageAsync(message.Reference.MessageId.Value).Result.Content;
                        LinksStorage.AddLinkToIvite(link);
                        await message.Channel.SendMessageAsync("@" + message.Author.Username + " please accept invitation to our organization either by going to http://github.com/linksplatform or via email that was sent to you from GitHub.");
                        foreach (var a in LinksStorage.GetLinksToInvite())
                        {
                            Console.WriteLine(a);
                        }
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