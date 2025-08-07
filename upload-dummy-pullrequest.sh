#!/usr/bin/env bash

echo Opening pull request
curl --verbose \
     'http://localhost:8081/api/open?pullRequestNumber=1234&author=author_name&commitid=123456&count=100'
echo

echo Adding data.
curl --verbose \
     --header "Content-Type: application/json" \
     --data-binary "@dummy-pullrequest.json" \
     http://localhost:8081/api/adddata
echo

# echo Closing pull request
# curl --verbose \
#       'http://localhost:8081/api/close?pullRequestNumber=1234'
# echo