# Build the source code into an image
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS postats-build

ADD . /code/

WORKDIR /code/
RUN dotnet publish -c Release -f net8.0 -r linux-x64 --no-self-contained

COPY /code/APSIM.POStats.Collector/bin/Release/net8.0/linux-x64/publish/ /code/bin/postats-collector/
COPY /code/APSIM.POStats.Portal/bin/Release/net8.0/linux-x64/publish/ /code/bin/postats-portal/


# Create the POStats-Collector image without all the other project and source code
FROM mcr.microsoft.com/dotnet/runtime:8.0-noble-chiseled AS postats-collector

COPY --from=postats-build /code/bin/postats-collector/ /code/postats-collector/

WORKDIR /code/postats-collector/
ENTRYPOINT ["dotnet", "APSIM.POStats.Collector.dll"]


# Create the POStats-Portal image without all the other project and source code
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS postats-portal
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=postats-build /code/bin/postats-portal/ /code/postats-portal/

WORKDIR /code/postats-portal/
ENTRYPOINT ["dotnet", "/code/postats-portal/APSIM.POStats.Portal.dll"]
