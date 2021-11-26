#Depending on the operating system of the host machines(s) that will build or run the containers, the image specified in the FROM statement may need to be changed.
#For more information, please see https://aka.ms/containercompat

FROM mcr.microsoft.com/dotnet/core/aspnet:2.2 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:2.2 AS build
WORKDIR /src
COPY ["KANO.Api.MobilePortal/KANO.Api.MobilePortal.csproj", "KANO.Api.MobilePortal/"]
COPY ["KANO.Core/KANO.Core.csproj", "KANO.Core/"]
RUN dotnet restore "KANO.Api.MobilePortal/KANO.Api.MobilePortal.csproj"
COPY . .
WORKDIR "/src/KANO.Api.MobilePortal"
RUN dotnet build "KANO.Api.MobilePortal.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "KANO.Api.MobilePortal.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "KANO.Api.MobilePortal.dll"]