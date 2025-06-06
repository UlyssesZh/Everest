#!/bin/bash
# Installs Everest from the branch to test, CelesteTAS and the mod that is going to be TASed.
# Parameters: ID of the mod to be TASed, URL of the TAS files

set -xeo pipefail

docker build \
	--build-arg "MAIN_BUILD_URL=`cat /tmp/everest-pr-tas-check/download-link.txt`" \
	--build-arg "TAS_FILES_URL=$2" \
	--build-arg "TAS_TO_RUN=$1" \
	-t celeste .
