#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.


FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

RUN apt-get update \
    && apt-get -y install gss-ntlmssp apt-utils libgdiplus libc6-dev locales \
    && locale-gen fr_FR.UTF-8 \
    && echo "LANG=fr_FR.UTF-8" > /etc/default/locale \
    && echo "LC_ALL=fr_FR.UTF-8" >> /etc/default/locale \
    && ln -s /usr/lib/libgdiplus.so /usr/lib/gdiplus.dll

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Sotoped.csproj", "."]
RUN dotnet restore "./Sotoped.csproj"

COPY . .
RUN dotnet build "./Sotoped.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
RUN dotnet publish "./Sotoped.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY Certificate/ /app/Certificate/
COPY License/ /app/License/
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Sotoped.dll"]
