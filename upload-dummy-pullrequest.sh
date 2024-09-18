#!/usr/bin/env bash

echo Opening pull request
curl --verbose \
     'http://localhost:8001/api/open?pullRequestNumber=1234&author=author_name'
echo

echo Adding data.
curl --verbose \
     --header "Content-Type: application/json" \
     --data-binary "@dummy-pullrequest.json" \
     http://localhost:8001/api/adddata
echo

echo Closing pull request
curl --verbose \
      'http://localhost:8001/api/close?pullRequestNumber=1234'
echo