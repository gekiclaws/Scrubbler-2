#!/bin/zsh

set -euo pipefail

readonly SCRIPT_DIR="${0:A:h}"
readonly PROJECT_FILE="$SCRIPT_DIR/Scrubbler/Scrubbler.Host/Scrubbler.Host.csproj"

find_dotnet() {
    if command -v dotnet >/dev/null 2>&1; then
        command -v dotnet
        return
    fi

    local candidate
    for candidate in \
        /opt/homebrew/bin/dotnet \
        /usr/local/bin/dotnet \
        /usr/local/share/dotnet/dotnet; do
        if [[ -x "$candidate" ]]; then
            print -r -- "$candidate"
            return
        fi
    done

    return 1
}

if [[ ! -f "$PROJECT_FILE" ]]; then
    print -u2 -- "Scrubbler project not found at:"
    print -u2 -- "  $PROJECT_FILE"
    exit 1
fi

if ! DOTNET="$(find_dotnet)"; then
    print -u2 -- "The .NET 10 SDK is required to run Scrubbler."
    print -u2 -- "Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
fi

if ! "$DOTNET" --list-sdks | grep -Eq '^10\.'; then
    print -u2 -- "The .NET 10 SDK was not found."
    print -u2 -- "Install it from https://dotnet.microsoft.com/download/dotnet/10.0"
    exit 1
fi

cd "$SCRIPT_DIR/Scrubbler"
exec "$DOTNET" run \
    --project "$PROJECT_FILE" \
    --framework net10.0-desktop \
    -- "$@"
