FROM microsoft/dotnet:2.1-sdk AS builder
WORKDIR /source

COPY *.csproj .
RUN dotnet restore

# copies the rest of your code
COPY . .
RUN dotnet publish /p:Version=$Version --output /app/ --configuration Release

# Stage 2
FROM microsoft/dotnet:2.1-aspnetcore-runtime
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "Wikiled.Sentiment.Service.dll"]

