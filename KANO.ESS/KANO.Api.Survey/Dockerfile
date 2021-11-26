#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["KANO.Api.Survey/KANO.Api.Survey.csproj", "KANO.Api.Survey/"]
RUN dotnet restore "KANO.Api.Survey/KANO.Api.Survey.csproj"
COPY . .
WORKDIR "/src/KANO.Api.Survey"
RUN dotnet build "KANO.Api.Survey.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KANO.Api.Survey.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KANO.Api.Survey.dll"]