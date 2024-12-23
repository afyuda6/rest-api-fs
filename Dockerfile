FROM mcr.microsoft.com/dotnet/sdk:9.0

RUN apt-get update && \
    DEBIAN_FRONTEND=noninteractive apt-get install -y \
    libsqlite3-dev \
    sqlite3 \
    curl && \
    apt-get clean

WORKDIR /rest-api-fs

COPY . /rest-api-fs

RUN dotnet restore
RUN dotnet publish -c Release -r linux-x64 --self-contained -o /rest-api-fs/out

EXPOSE 8080

ENTRYPOINT ["dotnet", "/rest-api-fs/out/rest-api-fs.dll"]