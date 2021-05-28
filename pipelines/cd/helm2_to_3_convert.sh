  
#!/usr/bin/env bash

set -o errexit -o pipefail

usage() {
  cat <<USAGE
$@
Usage ${script_name} [-k] [-n tiller_namespace] [-s {configmaps|secrets}]
Batch convert Helm 2 releases to Helm 3. Options:
  -k: Use kubectl to find the existing releases.
  -n: Namespace of Tiller (default "kube-system").
    Example: "-n cluster-tools"
  -s: Storage backend of Tiller. Must be either 'configmaps' or 'secrets' (default "configmaps").
    Example: "-s configmaps", "-s secrets"
  -h: Print help message.
USAGE

}

helm3_cmd="helm3"

helm2_releases="$(helm ls --all --short)"

echo -e "Found the following releases:\n${helm2_releases}\n"

for release in ${helm2_releases}; do
  ${helm3_cmd} 2to3 convert --dry-run "${release}"
    echo "Converting '${release}'."
    ${helm3_cmd} 2to3 convert "${release}"
    echo "Converted '${release}' successfully."
done