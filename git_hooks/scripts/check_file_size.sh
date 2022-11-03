#!/bin/bash
#
# An example hook script to verify what is about to be committed.
# Called by "git commit" with no arguments. The hook should
# exit with non-zero status after issuing an appropriate message if
# it wants to stop the commit.
#

# set -o errexit   # abort on nonzero exitstatus
set -o nounset   # abort on unbound variable
set -o pipefail  # don't hide errors within pipes

# shellcheck disable=SC1091
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"

declare FILE_SIZE_LIMIT_KB=51200
declare CURRENT_DIR
declare COUNTER=0
CURRENT_DIR="$(pwd)"
readonly FILE_SIZE_LIMIT_KB CURRENT_DIR

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

  print_title "Checking File Sizes"
	if [[ -z $files ]]; then
    print_output "skipped"
    exit 0
  fi
  error_output=$(check_file_sizes "$files")
  print_output "$error_output" "$COUNTER files are larger than permitted, please fix them before commit"

  if [[ -z "$error_output" ]]; then exit 0; else exit 1; fi;
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

check_file_sizes() {
	for file in $1 ; do
		file_path=$CURRENT_DIR/$file
		file_size=$(stat -c%s "$file_path")
		file_size_kb=$((file_size / 1024))
		if [[ "$file_size_kb" -ge "$FILE_SIZE_LIMIT_KB" ]]; then
			printf "%s has size %'d KB, over commit limit, %'d KB.\n" "$file" "$file_size_kb" "$FILE_SIZE_LIMIT_KB"
			COUNTER=$((COUNTER+1))
		fi
	done
}

main "${@}"