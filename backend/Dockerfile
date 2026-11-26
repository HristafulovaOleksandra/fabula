FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Fabula.Api/Fabula.Api.csproj Fabula.Api/
RUN dotnet restore Fabula.Api/Fabula.Api.csproj
COPY Fabula.Api/ Fabula.Api/
RUN dotnet publish Fabula.Api/Fabula.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:5080
EXPOSE 5080
ENTRYPOINT ["dotnet", "Fabula.Api.dll"]
