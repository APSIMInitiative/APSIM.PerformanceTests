#!/usr/bin/env bash

#Run this script to build the docker images localled. Not used by github actions.

docker build --target postats-build -t postats-build .
docker build --target postats-collector -t apsiminitiative/postats-collector .
docker build --target postats-portal -t apsiminitiative/postats-portal .

docker rmi postats-build