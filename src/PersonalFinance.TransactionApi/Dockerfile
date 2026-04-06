FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["PersonalFinance.TransactionApi/PersonalFinance.TransactionApi.csproj", "PersonalFinance.TransactionApi/"]
COPY ["PersonalFinance.Common/PersonalFinance.Common.csproj", "PersonalFinance.Common/"]
COPY ["PersonalFinance.ServiceDefaults/PersonalFinance.ServiceDefaults.csproj", "PersonalFinance.ServiceDefaults/"]
RUN dotnet restore "PersonalFinance.TransactionApi/PersonalFinance.TransactionApi.csproj"

COPY . .
WORKDIR /src/PersonalFinance.TransactionApi
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PersonalFinance.TransactionApi.dll"]
