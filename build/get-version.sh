#!/bin/bash
# Get version using GitVersion with fallback
set +e
version=$(dotnet gitversion /nofetch /showvariable SemVer 2>/dev/null)
if [ $? -ne 0 ] || [ -z "$version" ]; then
    version="0.1.0-dev+$(git rev-parse --short HEAD)"
fi
echo "$version"
