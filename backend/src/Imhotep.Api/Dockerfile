FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Directory.Build.props ./
COPY src/Imhotep.Domain/Imhotep.Domain.csproj src/Imhotep.Domain/
COPY src/Imhotep.Application/Imhotep.Application.csproj src/Imhotep.Application/
COPY src/Imhotep.Infrastructure/Imhotep.Infrastructure.csproj src/Imhotep.Infrastructure/
COPY src/Imhotep.Api/Imhotep.Api.csproj src/Imhotep.Api/
RUN dotnet restore src/Imhotep.Api/Imhotep.Api.csproj
COPY src/ src/
RUN dotnet publish src/Imhotep.Api/Imhotep.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Imhotep.Api.dll"]
