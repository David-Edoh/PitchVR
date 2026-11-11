#!/bin/sh
set -eu
root_dir=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
sdk=/Applications/Unity/Hub/Editor/6000.6.6f1/PlaybackEngines/AndroidPlayer/SDK
jdk=/Applications/Unity/Hub/Editor/6000.6.6f1/PlaybackEngines/AndroidPlayer/OpenJDK/bin
build_dir=$(mktemp -d)
trap 'rm -rf "$build_dir"' EXIT
mkdir -p "$build_dir/classes" "$build_dir/aar"
"$jdk/javac" -source 8 -target 8 -classpath "$sdk/platforms/android-34/android.jar" \
  -d "$build_dir/classes" "$root_dir/PdfBridge.java"
"$jdk/jar" cf "$build_dir/aar/classes.jar" -C "$build_dir/classes" .
cp "$root_dir/AndroidManifest.xml" "$build_dir/aar/AndroidManifest.xml"
(cd "$build_dir/aar" && "$jdk/jar" cf "$root_dir/../../Assets/Plugins/Android/PitchPdf.aar" .)
