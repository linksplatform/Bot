FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env
WORKDIR /app

COPY . ./
RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
RUN apt update && apt install jq -y
COPY entrypoint.sh .
COPY --from=build-env /app/out .

ENTRYPOINT ["bash", "/app/entrypoint.sh"]