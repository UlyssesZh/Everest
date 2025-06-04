#!/bin/bash
# Installs Everest from the branch to test, CelesteTAS and the mod that is going to be TASed.
# Run from within the Docker image, where Celeste is installed at /home/ubuntu/celeste.

set -xeo pipefail

# download Everest
cd /home/ubuntu
curl --fail -Lo everest.zip "${MAIN_BUILD_URL}"
unzip everest.zip
rm -v everest.zip

# copy Everest files to Celeste install
mv -fv main/* celeste/
rm -rfv main

# install Everest in headless mode
cd celeste
chmod -v u+x MiniInstaller-linux
./MiniInstaller-linux headless

# download TAS files
cd ..
curl --fail -Lo t.zip "https://celestemodupdater.0x0a.de/pinned-mods/TAS-Files-${TAS_TO_RUN}.zip"
unzip t.zip
rm -v t.zip

# install CelesteTAS (https://maddie480.ovh/celeste/dl?id=CelesteTAS&mirror=1)
cd celeste/Mods
curl --fail -Lo CelesteTAS.zip "https://celestemodupdater.0x0a.de/pinned-mods/CelesteTAS.zip"

# install the mod that is going to be TASed, downloaded as a bundle zip containing the mod zip
# and all of its dependencies (https://maddie480.ovh/celeste/bundle-download?id=${TAS_TO_RUN})
# for simplicity's sake, Celeste-Bundle.zip exists but is an empty zip
curl --fail -Lo t.zip "https://celestemodupdater.0x0a.de/pinned-mods/${TAS_TO_RUN}-Bundle.zip"
unzip t.zip
rm -v t.zip