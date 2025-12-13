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
        TAS_URL="https://github.com/VampireFlower/CelesteTAS/archive/fbfbd6c3026e6fb946f85eb98498e4e632ee399b.zip"
        TAS_PATH="CelesteTAS-fbfbd6c3026e6fb946f85eb98498e4e632ee399b/0 - 100%.tas"
        ;;

    "StrawberryJam2021")
        TAS_URL="https://github.com/VampireFlower/StrawberryJamTAS/archive/d7775dc7f38c4fb06eb63700ec5aec6dfee86031.zip"
        TAS_PATH="StrawberryJamTAS-d7775dc7f38c4fb06eb63700ec5aec6dfee86031/0-SJ All Levels.tas"
        BUNDLE_DOWNLOAD="https://celestemodupdater.0x0a.de/pinned-mods/StrawberryJam2021-Bundle-582b1269.zip"
        ;;

    *)
        echo "Unknown TAS: $1"
        exit 1
esac

cd "`dirname "$0"`"
./1-get-build-url.sh "$2"
./2-1-install.sh "${TAS_URL}" "${BUNDLE_DOWNLOAD}"
./3-run.sh "${TAS_PATH}"
