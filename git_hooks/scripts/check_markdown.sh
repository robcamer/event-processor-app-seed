#!/bin/bash

# set -o errexit   # abort on nonzero exitstatus
set -o nounset   # abort on unbound variable
set -o pipefail  # don't hide errors within pipes

# shellcheck disable=SC1091
source "$(dirname "${BASH_SOURCE[0]}")/helpers.sh"

main() {
  local files=""
  local delimentated_files=""
  local unvalidated_files=""
	local markdown_files=""
	local error_output=""

  for i in ${1:-}; do
    case $i in
      --files=*)
        delimentated_files="${i#*=}"
        unvalidated_files=$(split_delimenated_string "$delimentated_files" ",")
        files=$(remove_invalid_files "$unvalidated_files")
				markdown_files=$(get_markdown_files "$files")
        shift # past argument=value
        ;;
      -h|--help)
        usage
        ;;
      *)
        ;;
    esac
  done

	print_title "Checking Markdown"
  if [[ -z $markdown_files && -n $files ]]; then
    print_output "skipped"
    exit 0
  fi
	run_markdown_lint "$markdown_files"
	error_output=$(get_output)
	print_output "$error_output" "Lint errors found in markdown!"

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

get_markdown_files () {
  local -r files="${1}" # local readonly variable for holding all files
  local markdown_files  # local variable for holding markdown files
  markdown_files=$(echo "$files" | grep -e '\.md$') # filter out non-markdown files
  echo "$markdown_files" # return all markdown files
}

run_markdown_lint () {
  local -r markdown_files="${1}" # local readonly variable holding files passed
  local -r markdown_regex='**/*.md' # local readonly regex pattern for matching markdown files.

  if [[ -z $markdown_files ]];
  then markdownlint "$markdown_regex" -c .markdownlint.json -o linterror
  else echo "$markdown_files" | xargs markdownlint -c .markdownlint.json -o linterror
  fi
}

get_output() {
	local output
	output=$(<linterror)
	rm -f linterror
	echo "$output"
}

main "${@}"