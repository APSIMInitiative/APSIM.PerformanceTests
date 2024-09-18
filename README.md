# Performance Stats

## To build, run and test docker image from a local repo
Comment git clone line in dockerfile and uncomment the 'ADD' line. Then build and run docker image:
    docker build -t apsiminitiative/postats .
    docker compose up -d


To test docker image from a shell script:
    bash upload-dummy-pullrequest.sh
