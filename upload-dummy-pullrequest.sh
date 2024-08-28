#!/usr/bin/env bash

status=$(curl -i -w POST  http://localhost:8001/api/uploadpodata \
              -H "Content-Type: application/json" \
              --data-binary "@dummy-pullrequest.json")
echo $status