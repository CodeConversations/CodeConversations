FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS="http://*:80;https://*:443"

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-bionic AS build
# Copy package sources
COPY ./NuGet.config /root/nuget.config
COPY ./NuGet.config /nuget.config
WORKDIR /src
COPY ["CodeConversations.csproj", "./"]
RUN dotnet restore "./CodeConversations.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "CodeConversations.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CodeConversations.csproj" -c Release -o /app/publish

FROM base AS final
RUN dotnet tool install -g --add-source "https://dotnet.myget.org/F/dotnet-try/api/v3/index.json" Microsoft.dotnet-interactive --version 1.0.122501
ENV PATH="$PATH:/root/.dotnet/tools"
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CodeConversations.dll"]
