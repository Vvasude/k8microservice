#!/usr/bin/env bash
#
# Deletes the k3d cluster (and everything in it, including the Postgres volume).
#
set -euo pipefail
CLUSTER="${CLUSTER:-bankdev}"
k3d cluster delete "${CLUSTER}"
echo "Cluster '${CLUSTER}' deleted."
