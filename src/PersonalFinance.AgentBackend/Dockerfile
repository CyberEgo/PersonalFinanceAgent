FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["PersonalFinance.AgentBackend/PersonalFinance.AgentBackend.csproj", "PersonalFinance.AgentBackend/"]
COPY ["PersonalFinance.Common/PersonalFinance.Common.csproj", "PersonalFinance.Common/"]
COPY ["PersonalFinance.ServiceDefaults/PersonalFinance.ServiceDefaults.csproj", "PersonalFinance.ServiceDefaults/"]
RUN dotnet restore "PersonalFinance.AgentBackend/PersonalFinance.AgentBackend.csproj"

COPY . .
WORKDIR /src/PersonalFinance.AgentBackend
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PersonalFinance.AgentBackend.dll"]
