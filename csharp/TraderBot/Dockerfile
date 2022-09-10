FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env
WORKDIR /app

COPY config.sh ./

RUN apt update && \
    apt install jq -y && \
    bash /app/config.sh > appsettings.json

COPY app ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
COPY --from=build-env /app/appsettings.json .
ENTRYPOINT ["dotnet", "TraderBot.dll"]