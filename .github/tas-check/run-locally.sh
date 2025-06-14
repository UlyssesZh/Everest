#!/bin/bash
# Builds and runs the TAS locally, calling the same scripts as the pipeline in order to test them.
# Requires Docker and jq.

if [ "$1" == "" ] || [ "$2" == "" ]; then
    echo "Usage: ./run-locally.sh [Celeste|StrawberryJam2021] [commit SHA]"
    exit 1
fi

set -xeo pipefail

case "$1" in
    "Celeste")
        TAS_URL="https://github.com/VampireFlower/CelesteTAS/archive/60b1680e61e43ec4681d7c9053d249491e0fe905.zip"
        TAS_PATH="CelesteTAS-60b1680e61e43ec4681d7c9053d249491e0fe905/0 - 100%.tas"
        ;;

    "StrawberryJam2021")
        TAS_URL="https://github.com/VampireFlower/StrawberryJamTAS/archive/fc7397c26f4d15468d4a8a3e58e7cc3d62d21223.zip"
        TAS_PATH="StrawberryJamTAS-fc7397c26f4d15468d4a8a3e58e7cc3d62d21223/0-SJ All Levels.tas"
        ;;

    *)
        echo "Unknown TAS: $1"
        exit 1
esac

cd "`dirname "$0"`"
./1-get-build-url.sh "$2"
./2-1-install.sh "$1" "${TAS_URL}"
./3-run.sh "${TAS_PATH}"
