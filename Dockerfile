# ====================
#         BUILD SECTOR
# ====================

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env

WORKDIR /app

COPY VKBugTrackerBot/. ./
RUN dotnet publish --configuration Release --output out/

# ====================
#           RUN SECTOR
# ====================
FROM mcr.microsoft.com/dotnet/core/runtime:3.1

WORKDIR /app

COPY --from=build-env /app/out .

ENTRYPOINT ["dotnet", "VKBugTrackerBot.dll"]