# This dockerfile is used to build a docker image for the POStats website.
FROM mcr.microsoft.com/dotnet/sdk:6.0 as build

# Clone and build the website.
#RUN git clone --depth 1 https://github.com/APSIMInitiative/APSIM.PerformanceTests /APSIM.POStats

ADD . /APSIM.POStats

RUN cd /APSIM.POStats && dotnet publish -c Release -f net6.0 -r linux-x64 --no-self-contained

# Actual container is based on dotnet/aspnet, and doesn't include build tools.
FROM mcr.microsoft.com/dotnet/aspnet:6.0
RUN mkdir -p /opt/APSIM.POStats
COPY --from=build /APSIM.POStats/APSIM.POStats.Portal/bin/Release/net6.0/linux-x64/publish/ /opt/APSIM.POStats/
ENV ASPNETCORE_ENVIRONMENT=Production
SHELL ["/bin/bash"]
WORKDIR "/opt/APSIM.POStats"
ENTRYPOINT ["dotnet", "/opt/APSIM.POStats/APSIM.POStats.Portal.dll"]