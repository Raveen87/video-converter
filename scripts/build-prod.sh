#!/bin/bash
set -e
docker build -t video-converter:latest .
echo "Build complete: video-converter:latest"

