FROM mcr.microsoft.com/dotnet/core/sdk:3.0 AS builder
ARG BUILD_VERSION  
WORKDIR /source

COPY . .
RUN dotnet publish /p:Version=$BUILD_VERSION --output /app/ --configuration Release

# Stage 2
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0
WORKDIR /app
COPY --from=builder /app .
ENV ASPNETCORE_ENVIRONMENT Production

#EXPOSE 5000 5083

ENTRYPOINT ["dotnet", "Wikiled.Sentiment.Service.dll"]

