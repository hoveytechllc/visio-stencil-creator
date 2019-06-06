#!/bin/bash
#
# Script that will build docker image locally
#   and run image to generate Visio Stencil.
#
# Parameters must be in format "--parameter=value"
# i.e.:
#
#       --content-path="./../images"
#       --output-filename=ProjectStencilPack
#

GITHUB_ORG="hoveytechllc"
REPO_NAME="visio-stencil-creator"

while [ $# -gt 0 ]; do
  case "$1" in
    --content-path=*)
      CONTENT_PATH="${1#*=}"
      ;;
    --output-filename=*)
      OUTPUT_FILENAME="${1#*=}"
      ;;
    *)
      printf "***************************\n"
      printf "* Error: Invalid argument.*\n"
      printf "***************************\n"
      exit 1
  esac
  shift
done

if [ -z $OUTPUT_FILENAME ]; then
  OUTPUT_FILENAME="Generated"
fi
if [ -z $CONTENT_PATH ]; then
  CONTENT_PATH=${PWD}
fi

echo "Using content path: '"${CONTENT_PATH}"'"
echo "Using output filename: '"${OUTPUT_FILENAME}"'"

rm -fdr ./${REPO_NAME}

# Clone repository in current path
git clone https://github.com/${GITHUB_ORG}/${REPO_NAME}.git

# build image using Dockerfile from github repository
docker build \
    -t ${REPO_NAME}:latest \
    -f ./${REPO_NAME}/Dockerfile \
    ./${REPO_NAME}

# Run newly created Docker image
docker run \
    -v ${CONTENT_PATH}:/app/content \
    ${REPO_NAME}:latest \
    "**/*.png" \
    "/app/content" \
    "/app/content/${OUTPUT_FILENAME}.vssx"

rm -fdr ./${REPO_NAME}