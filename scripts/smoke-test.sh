#!/bin/sh
set -eu

base_url=${1:-http://localhost:5045}
base_url=${base_url%/}

check_page() {
    html=$(curl -fsS --max-time 5 "$base_url$1")
    if ! printf '%s\n' "$html" | grep -Fq "$2"; then
        printf 'Expected %s on %s\n' "$2" "$1" >&2
        exit 1
    fi
}

check_page / 'Menu · Barista'
check_page '/Order?Input.Item=Espresso&Input.Size=Small' 'id="receipt-title"'

redirect=$(curl -fsS --max-time 5 -o /dev/null -w '%{redirect_url}' "$base_url/Admin")
case "$redirect" in
    "$base_url"/Staff*) ;;
    *)
        printf 'Admin did not redirect a signed-out visitor to staff sign-in.\n' >&2
        exit 1
        ;;
esac

printf 'Barista smoke test passed.\n'
