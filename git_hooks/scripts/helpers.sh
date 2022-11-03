#!/bin/bash
#
# File with helper functions

#######################################
# Prints title.
# Arguments:
#  Title to print.
# Outputs:
#   Title with dot padding.
#######################################
print_title() {
  local -r PADDING="............................................................"
  printf "%s%s" "$1" "${PADDING:${#1}}"
}

print_output() {
  local -r results=$1
  local -r message=${2-:}
  if [[ -z  "$results" ]]; then
    printf "\e[32;1m%s\e[0m\n" "success";
  else
    if [[ "skipped" == "$results" ]]; then
      printf "\e[33;1m%s\e[0m\n" "skipped";
    else
      printf "\e[31;1m%s\e[0m\n\e[31;1m%s\e[0m\n%s\n\n" "failure" "$message" "$results";
    fi;
  fi
}

split_delimenated_string() {
  local -r string="${1}"
  local -r delimeter="${2}"

  echo "$string" | tr "$delimeter" "\n"
}

remove_invalid_files() {
  local -r files="${1}"

  echo "$files" |
  while IFS= read -r filename; do
    if [[ -f "$filename" ]]; then
      echo "$filename"
    fi
  done
}