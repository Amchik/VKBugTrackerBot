# ====================
#         BUILD SECTOR
# ====================

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env

WORKDIR /app

COPY VKBugTrackerBot/VKBugTrackerBot.csproj ./
RUN dotnet restore

COPY VKBugTrackerBot/. ./
RUN dotnet build --configuration Release --output out/ --no-dependencies

# ====================
#           RUN SECTOR
# ====================
FROM mcr.microsoft.com/dotnet/core/runtime:3.1

WORKDIR /app

COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "VKBugTrackerBot.dll"]