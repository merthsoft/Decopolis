FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /source
COPY . .
RUN dotnet publish -o /app -c Release \
    -p:PublishTrimmed=true -p:TrimMode=link \
    -p:DebugType=embedded \
    -p:PublishSingleFile=true -r linux-x64 Merthsoft.Decopolis.csproj

FROM mcr.microsoft.com/dotnet/runtime-deps:8.0
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["/app/Merthsoft.Decopolis"]
