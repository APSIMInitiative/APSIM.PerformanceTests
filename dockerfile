# This dockerfile is used to build a local docker image for the POStats website.
# When running on the server, use deploy script
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Uncomment next line (and comment previous line) to use a local repo rather than clone remote.
ADD . /APSIM.POStats

# Build POStats
RUN cd /APSIM.POStats && dotnet publish -c Release -f net8.0 -r linux-x64 --no-self-contained

# Actual container is based on dotnet/aspnet, and doesn't include build tools.
FROM mcr.microsoft.com/dotnet/aspnet:8.0
RUN mkdir -p /opt/APSIM.POStats
COPY --from=build /APSIM.POStats/APSIM.POStats.Portal/bin/Release/net8.0/linux-x64/publish/ /opt/APSIM.POStats/
ENV ASPNETCORE_ENVIRONMENT=Production
SHELL ["/bin/bash"]
WORKDIR "/opt/APSIM.POStats"
ENTRYPOINT ["dotnet", "/opt/APSIM.POStats/APSIM.POStats.Portal.dll"]