
# ---- Stage 1: build ----
    FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
    WORKDIR /src
    
    # Restore first, using only the csproj, so this layer caches independently of code changes
    COPY AuthService.csproj .
    RUN dotnet restore
    
    # Now bring in the rest of the source and publish
    COPY . .
    RUN dotnet publish -c Release -o /app/publish
    
    # ---- Stage 2: runtime ----
    FROM mcr.microsoft.com/dotnet/aspnet:10.0
    WORKDIR /app
    COPY --from=build /app/publish .
    EXPOSE 8080
    ENTRYPOINT ["dotnet", "AuthService.dll"]