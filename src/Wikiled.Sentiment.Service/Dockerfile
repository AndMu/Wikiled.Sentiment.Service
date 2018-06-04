FROM microsoft/dotnet:2.1-aspnetcore-runtime-nanoserver-1709 AS base
WORKDIR /app
EXPOSE 80

FROM microsoft/dotnet:2.1-sdk-nanoserver-1709 AS build
WORKDIR /src
COPY Wikiled.Sentiment.Service/Wikiled.Sentiment.Service.csproj Wikiled.Sentiment.Service/
RUN dotnet restore Wikiled.Sentiment.Service/Wikiled.Sentiment.Service.csproj
COPY . .
WORKDIR /src/Wikiled.Sentiment.Service
RUN dotnet build Wikiled.Sentiment.Service.csproj -c Release -o /app

FROM build AS publish
RUN dotnet publish Wikiled.Sentiment.Service.csproj -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "Wikiled.Sentiment.Service.dll"]
