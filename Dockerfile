FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY prjGoHike/prjGoHike.csproj prjGoHike/
RUN dotnet restore prjGoHike/prjGoHike.csproj

COPY prjGoHike/ prjGoHike/
RUN dotnet publish prjGoHike/prjGoHike.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
USER $APP_UID
ENTRYPOINT ["dotnet", "prjGoHike.dll"]
