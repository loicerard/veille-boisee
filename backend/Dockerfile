FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/VeilleBoisee.Api/VeilleBoisee.Api.csproj",                       "src/VeilleBoisee.Api/"]
COPY ["src/VeilleBoisee.Application/VeilleBoisee.Application.csproj",       "src/VeilleBoisee.Application/"]
COPY ["src/VeilleBoisee.Domain/VeilleBoisee.Domain.csproj",                 "src/VeilleBoisee.Domain/"]
COPY ["src/VeilleBoisee.Infrastructure/VeilleBoisee.Infrastructure.csproj", "src/VeilleBoisee.Infrastructure/"]
RUN dotnet restore "src/VeilleBoisee.Api/VeilleBoisee.Api.csproj"
COPY . .
WORKDIR "/src/src/VeilleBoisee.Api"
RUN dotnet publish "VeilleBoisee.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "VeilleBoisee.Api.dll"]
