#!/bin/zsh

set -euo pipefail

readonly SCRIPT_DIR="${0:A:h}"
readonly PROJECT_FILE="$SCRIPT_DIR/Scrubbler/Scrubbler.Host/Scrubbler.Host.csproj"
readonly DEBUG_PLUGINS_DIR="$SCRIPT_DIR/Scrubbler/DebugPlugins"

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

find_repo_dir() {
    local repo_name="$1"
    local candidate

    for candidate in \
        "$SCRIPT_DIR/$repo_name" \
        "${SCRIPT_DIR:h}/$repo_name"; do
        if [[ -d "$candidate/.git" ]]; then
            print -r -- "$candidate"
            return
        fi
    done

    return 1
}

build_local_plugin() {
    local project_file="$1"
    local plugin_base_project="$2"
    local project_name="${project_file:t:r}"
    local output_dir="${project_file:h}/bin/Release/net10.0"
    local destination="$DEBUG_PLUGINS_DIR/$project_name"

    print -r -- "Building local plugin: $project_name"
    "$DOTNET" build "$project_file" \
        --configuration Release \
        --nologo \
        -p:LocalPluginBaseProject="$plugin_base_project"

    mkdir -p "$destination"
    find "$output_dir" -maxdepth 1 -type f \
        \( -name '*.dll' \
        -o -name '*.pdb' \
        -o -name '*.xml' \
        -o -name '*.env' \
        -o -name '*.png' \
        -o -name '*.deps.json' \
        -o -name '*.runtimeconfig.json' \) \
        -exec cp -f -- {} "$destination/" \;
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

launch_profile="Scrubbler.Host (Desktop)"
local_plugin_base_argument=()

if PLUGIN_BASE_REPO="$(find_repo_dir "Scrubbler.PluginBase")" \
    && MANUAL_PLUGIN_REPO="$(find_repo_dir "Scrubbler.Plugin.Scrobblers.ManualScrobbler")" \
    && FILE_PARSE_PLUGIN_REPO="$(find_repo_dir "Scrubbler.Plugin.Scrobblers.FileParseScrobbler")"; then
    readonly PLUGIN_BASE_PROJECT="$PLUGIN_BASE_REPO/Scrubbler.PluginBase/Scrubbler.PluginBase.csproj"
    readonly MANUAL_PLUGIN_PROJECT="$MANUAL_PLUGIN_REPO/Scrubbler.Plugin.Scrobblers.ManualScrobbler/Scrubbler.Plugin.Scrobblers.ManualScrobbler.csproj"
    readonly FILE_PARSE_PLUGIN_PROJECT="$FILE_PARSE_PLUGIN_REPO/Scrubbler.Plugin.Scrobblers.FileParseScrobbler/Scrubbler.Plugin.Scrobblers.FileParseScrobbler.csproj"

    build_local_plugin "$MANUAL_PLUGIN_PROJECT" "$PLUGIN_BASE_PROJECT"
    build_local_plugin "$FILE_PARSE_PLUGIN_PROJECT" "$PLUGIN_BASE_PROJECT"

    launch_profile="Local Plugins"
    local_plugin_base_argument=(-p:LocalPluginBaseProject="$PLUGIN_BASE_PROJECT")
else
    print -r -- "Local plugin repositories not found; launching installed plugins."
fi

cd "$SCRIPT_DIR/Scrubbler"
exec "$DOTNET" run \
    --project "$PROJECT_FILE" \
    --framework net10.0-desktop \
    --launch-profile "$launch_profile" \
    "${local_plugin_base_argument[@]}" \
    -- "$@"
