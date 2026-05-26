FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Prode.Api.csproj .
RUN dotnet restore Prode.Api.csproj

COPY . .
RUN dotnet publish Prode.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

ENV PORT=8080
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["sh", "-c", "dotnet Prode.Api.dll --urls http://0.0.0.0:${PORT}"]
