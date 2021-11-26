FROM mcr.microsoft.com/dotnet/core/aspnet:2.2-stretch-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2-stretch AS build
WORKDIR /src
COPY ["KANO.Api.Complaint/KANO.Api.Complaint.csproj", "KANO.Api.Complaint/"]
RUN dotnet restore "KANO.Api.Complaint/KANO.Api.Complaint.csproj"
COPY . .
WORKDIR "/src/KANO.Api.Complaint"
RUN dotnet build "KANO.Api.Complaint.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "KANO.Api.Complaint.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "KANO.Api.Complaint.dll"]