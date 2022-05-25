# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src


COPY *.csproj .
RUN dotnet restore 

COPY . .
RUN dotnet publish -c Release -o /publish


# Serve Stage
FROM mcr.microsoft.com/dotnet/sdk:6.0
WORKDIR /app
COPY --from=build /publish .

ENV PORT "$PORT"
ENV ASPNETCORE_HTTP_PORT=https://+:$PORT
ENV ASPNETCORE_URLS=https://+:$PORT
ENV ASPNETCORE_Kestrel__Certificates__Default__Password=loneworkingregister
ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/https/LoneWorkingBackend.pfx
EXPOSE $PORT

COPY ["LoneWorkingBackend.pfx", "/https/LoneWorkingBackend.pfx"]
CMD ASPNETCORE_URLS=http://*:$PORT dotnet LoneWorkingBackend.dll