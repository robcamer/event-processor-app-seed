#!/bin/bash

# set -o errexit   # abort on nonzero exitstatus
set -o nounset   # abort on unbound variable
set -o pipefail  # don't hide errors within pipes

# shellcheck disable=SC1091
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"

main() {
  local files=""

  for i in ${1:-}; do
    case $i in
      --files=*)
        delimentated_files="${i#*=}"
        unvalidated_files=$(split_delimenated_string "$delimentated_files" ",")
        files=$(remove_invalid_files "$unvalidated_files")
        shift # past argument=value
        ;;
      -h|--help)
        usage
        ;;
      *)
        ;;
    esac
  done

  print_title "Checking for Secrets"
  secret_output=$(run_detect_secrets "$files")
  print_output "$secret_output" "Secrets detected in repo!"

  if [[ -z $secret_output ]]; then exit 0; else exit 1; fi;
}

usage() {
  cat <<- USAGE
  Usage: $0 [--files] [-h help]

  Options:
    --files:      comma seperated files to run checks for
    -h, --help:   display usage information
	USAGE
  exit 1
}

run_detect_secrets() {
  local -r files="${1}"
  local output=""
  if [[ -z "$files" ]]; then
    output=$(detect-secrets-hook --baseline .secrets.baseline)
  else
    output=$(echo "$files" | xargs detect-secrets-hook --baseline .secrets.baseline)
  fi
  echo "$output"
}

main "${@}"