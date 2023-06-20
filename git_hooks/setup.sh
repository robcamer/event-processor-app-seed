#!/bin/bash

CURRENT_DIR=$(dirname "${BASH_SOURCE[0]}")

readonly GIT_HOOKS_DIR=".git/hooks"

#configure pre-commit
cat "$CURRENT_DIR"/pre-commit.sh > "$GIT_HOOKS_DIR"/pre-commit
chmod +x "$GIT_HOOKS_DIR"/pre-commit
