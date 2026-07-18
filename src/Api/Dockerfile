FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore

RUN dotnet build -c Release
RUN dotnet publish BlazorClient -c Release -o /app/blazor
RUN dotnet publish Api/Api.csproj -c Release -o /app/api

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/api ./

RUN mkdir -p wwwroot
COPY --from=build /app/blazor/wwwroot ./wwwroot

EXPOSE 80
ENTRYPOINT ["dotnet", "Api.dll"]