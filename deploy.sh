#!/usr/bin/env bash

# stop existing containers.
docker compose down

# bring up docker stack
docker compose up -d
