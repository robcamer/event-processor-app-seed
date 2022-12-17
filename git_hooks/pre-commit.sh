#!/bin/bash

changed_file_count=$(git diff --cached --name-only -- 'src/**/*.cs' 'src/**/*.csproj' | wc -l)

if [ "$changed_file_count" -gt 0 ]; then
	# Format code
	printf "\n\n**Checking code formatted properly!**\n"
	dotnet format --verify-no-changes || { echo "Run 'dotnet format' to fix formatting errors!"; exit 1; }

	# Check Build
	printf "\n\n**Building project!**\n"
	dotnet build --no-restore || exit 1

	# Run Test
	printf "\n\n**Running unit test!**\n"
	dotnet test --no-build || exit 1
fi;