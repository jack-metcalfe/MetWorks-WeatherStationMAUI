#!/usr/bin/env bash
# Usage: add-references.sh "path1;path2" "ProjectFile.csproj"

refs="$1"
projfile="$2"

IFS=';' read -ra arr <<< "$refs"
for r in "${arr[@]}"; do
  trimmed="$(echo "$r" | xargs)"
  if [ -n "$trimmed" ]; then
    echo "Adding reference to $trimmed"
    dotnet add "$projfile" reference "$trimmed"
  fi
done
