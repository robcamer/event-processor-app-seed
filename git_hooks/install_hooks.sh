#!/bin/bash
directory="git_hooks"

# Make the hooks runnable
chmod +x $directory/pre-commit

# Configure git to use the new hook - it will stay with the repository
git config core.hooksPath "./$directory"