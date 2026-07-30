#!/bin/zsh

set -euo pipefail

readonly SCRIPT_DIR="${0:A:h}"
readonly SOLUTION_FILE="$SCRIPT_DIR/Scrubbler/Scrubbler.CrossPlatform.slnf"
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

if [[ ! -f "$SOLUTION_FILE" || ! -f "$PROJECT_FILE" ]]; then
    print -u2 -- "Scrubbler source tree is incomplete at:"
    print -u2 -- "  $SCRIPT_DIR/Scrubbler"
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

print -r -- "Building Scrubbler and bundled plugins..."
"$DOTNET" build "$SOLUTION_FILE" \
    --configuration Debug \
    --nologo

cd "$SCRIPT_DIR/Scrubbler"
exec "$DOTNET" run \
    --project "$PROJECT_FILE" \
    --framework net10.0-desktop \
    --configuration Debug \
    --no-build \
    --launch-profile "Local Plugins" \
    -- "$@"
