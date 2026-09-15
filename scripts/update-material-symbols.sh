#!/usr/bin/env bash
# Refreshes the Material Symbols Rounded font embedded in Mythetech.Framework.
#
# Google only serves the woff2 build to browser-like clients, so the CSS is
# requested with a desktop Chrome user agent. The font file is downloaded over
# the embedded copy and the version tag recorded in mythetech.css is updated so
# the diff shows which Google Fonts release the package now ships.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CSS_FILE="$REPO_ROOT/Mythetech.Framework/wwwroot/mythetech.css"
FONT_FILE="$REPO_ROOT/Mythetech.Framework/wwwroot/fonts/material-symbols-rounded.woff2"
GOOGLE_CSS_URL="https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded"
USER_AGENT="Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36"

google_css="$(curl --fail --silent --show-error --location --user-agent "$USER_AGENT" "$GOOGLE_CSS_URL")"

font_url="$(printf '%s' "$google_css" | grep -o 'https://fonts.gstatic.com/[^)]*\.woff2' | head -n 1 || true)"
if [[ -z "$font_url" ]]; then
    echo "Could not find a woff2 url in the Google Fonts response" >&2
    exit 1
fi

new_version="$(printf '%s' "$font_url" | grep -o '/v[0-9]*/' | tr -d '/')"
current_version="$(grep -oE 'material-symbols-version: v[0-9]+' "$CSS_FILE" | grep -oE 'v[0-9]+$' || echo "none")"

echo "Current embedded version: $current_version"
echo "Latest Google Fonts version: $new_version"

if [[ "$current_version" == "$new_version" ]]; then
    echo "Already up to date"
    exit 0
fi

curl --fail --silent --show-error --location --user-agent "$USER_AGENT" --output "$FONT_FILE" "$font_url"

sed -i.bak "s|material-symbols-version: v[0-9]*|material-symbols-version: $new_version|" "$CSS_FILE"
rm -f "$CSS_FILE.bak"

echo "Updated $FONT_FILE to $new_version ($(wc -c < "$FONT_FILE" | tr -d ' ') bytes)"
