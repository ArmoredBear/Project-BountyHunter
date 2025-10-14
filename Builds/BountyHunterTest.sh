#!/bin/sh
echo -ne '\033c\033]0;Project Bounty Hunter\a'
base_path="$(dirname "$(realpath "$0")")"
"$base_path/BountyHunterTest.x86_64" "$@"
