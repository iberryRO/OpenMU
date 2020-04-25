FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build

# Requirements for the typescript compiler
RUN apt-get update
RUN apt-get install npm -y
RUN npm i -g typescript

COPY src/ ./app/src

WORKDIR app

RUN dotnet build src/Startup/MUnique.OpenMU.Startup.csproj -c Release -p:ci=true

RUN dotnet publish src/AdminPanel/MUnique.OpenMU.AdminPanel.csproj -o out -c Release --no-build
RUN dotnet publish src/Startup/MUnique.OpenMU.Startup.csproj -o out -c Release --no-build

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1 AS runtime

EXPOSE 1234
EXPOSE 55901
EXPOSE 55902
EXPOSE 55903
EXPOSE 44405
EXPOSE 55980

WORKDIR /app
COPY --from=build /app/out ./
ENTRYPOINT ["dotnet", "MUnique.OpenMU.Startup.dll", "-autostart"]
