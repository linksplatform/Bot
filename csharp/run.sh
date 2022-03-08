# Install dotnet
wget https://packages.microsoft.com/config/ubuntu/21.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

sudo apt-get update; \
  sudo apt-get install -y apt-transport-https && \
  sudo apt-get update && \
  sudo apt-get install -y dotnet-sdk-5.0

# Run bot
# $1 is token, $2 is app name
dotnet run --project ./Platform.Bot/Platform.Bot.csproj $1 $2 $3 db.links HelloWorldSet 60
