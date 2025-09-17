# This script builds and pushes the Docker images to Docker Hub

echo "Building and pushing Docker images to Docker Hub..."
echo "Make sure you are logged in to Docker Hub using 'docker login' before running this script."
echo "Also ensure that you have permission to push to the 'apsiminitiative' repository."
echo "You can login as apsimbot if you have the credentials."
echo "You can run './build.sh' to build the images locally before pushing."
echo ""

# build the image first
./build.sh

simple-sha=$(git rev-parse --short HEAD)

# push the images to Docker Hub
docker push apsiminitiative/postats2-portal:latest
docker push apsiminitiative/postats2-portal:$simple-sha
docker push apsiminitiative/postats2-collector:latest
docker push apsiminitiative/postats2-collector:$simple-sha

echo "Done."