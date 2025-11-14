# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

# Install Chrome, Node.js, and other dependencies in the base image
RUN apt-get update && \
    apt-get install -y wget curl gnupg2 && \
    # Install Chrome
    wget -q -O - https://dl-ssl.google.com/linux/linux_signing_key.pub | apt-key add - && \
    echo "deb [arch=amd64] http://dl.google.com/linux/chrome/deb/ stable main" > /etc/apt/sources.list.d/google-chrome.list && \
    apt-get update && \
    apt-get install -y google-chrome-stable && \
    # Install Node.js
    curl -sL https://deb.nodesource.com/setup_20.x | bash - && \
    apt-get install -y nodejs && \
    # Install Playwright and browsers
    npm install -g playwright @playwright/browser-chromium && \
    npx playwright install chrome --with-deps && \
    # Create directory for Playwright and set permissions
    mkdir -p /home/app/.cache/ms-playwright && \
    chmod -R 777 /home/app/.cache/ms-playwright && \
    # Clean up
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Set environment variables for Playwright
ENV PLAYWRIGHT_BROWSERS_PATH=/home/app/.cache/ms-playwright
ENV PLAYWRIGHT_SKIP_BROWSER_DOWNLOAD=1
ENV PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH=/usr/bin/google-chrome-stable

# Force isolated browser instances
ENV PLAYWRIGHT_FORCE_ISOLATED=1
ENV PLAYWRIGHT_BROWSERS_NO_SANDBOX=1

# Add these environment variables to help with Chrome in Docker
ENV CHROMIUM_FLAGS="--no-sandbox --disable-dev-shm-usage --isolated"

USER $APP_UID
WORKDIR /app
EXPOSE 80
EXPOSE 443

# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Dotnet.GenAI.MyCareerAssistant/Dotnet.GenAI.MyCareerAssistant.csproj", "Dotnet.GenAI.MyCareerAssistant/"]

RUN dotnet restore "./Dotnet.GenAI.MyCareerAssistant/Dotnet.GenAI.MyCareerAssistant.csproj"

COPY . .

WORKDIR "/src/Dotnet.GenAI.MyCareerAssistant"

RUN dotnet build "./Dotnet.GenAI.MyCareerAssistant.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Dotnet.GenAI.MyCareerAssistant.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dotnet.GenAI.MyCareerAssistant.dll"]