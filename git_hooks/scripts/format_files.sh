#!/bin/bash

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
        shift
        ;;
      -h|--help)
        usage
        ;;
      *)
        ;;
    esac
  done

  echo "Auto formatting: $files"

  run_pre_commit_hooks

  formatted_files=$(git diff --cached --name-only)
  formatted_files=$(echo "$formatted_files" | paste -s -d "\n" -)
  git_add_formatted_files "$formatted_files"

  git config core.hooksPath "./git_hooks"
  exit 0
}

git_add_formatted_files() {
  local -r files="${1}"
  local output=""

  for file in $files; do
	echo "Running git add for file: $file"
	output="${output} $(git add "$file")"
  done

  echo "$output"
}

run_pre_commit_hooks() {
  pip install pre-commit
  pre-commit install >/dev/null
  pre-commit run
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

main "$@"
