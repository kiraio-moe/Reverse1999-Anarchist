#!/bin/bash

# Define the target operating systems and architectures
VERSION="1.0.0"
TARGET_OS_ARCHITECTURES=("win-x64" "win-arm64" "linux-x64" "linux-arm64" "osx-x64" "osx-arm64")
TARGET_FRAMEWORKS=("net6.0" "net7.0" "net8.0")

echo "Building Reverse: 1999 - Anarchist..."
# Build the project for each target OS architecture
for os_arch in "${TARGET_OS_ARCHITECTURES[@]}"
do
    for framework in "${TARGET_FRAMEWORKS[@]}"
    do
        echo "Building for $os_arch architecture..."
        dotnet publish -p:PublishSingleFile=true -c Release -f "$framework" -r "$os_arch" --no-self-contained

        # Check if build was successful
        if [ $? -eq 0 ]; then
            echo "Build for $os_arch completed successfully."
        else
            echo "Build for $os_arch failed."
            exit 1  # Exit the script with an error code
        fi
    done
done

echo "Build process completed for all target OS architectures."
echo "Creating ZIP archive for every architecture..."
for framework in "${TARGET_FRAMEWORKS[@]}"
do
    for os_arch in "${TARGET_OS_ARCHITECTURES[@]}"
    do
        PUBLISH_PATH="bin/Release/${framework}/${os_arch}/publish"
        ZIP_OUTPUT="Reverse1999-Anarchist-v${VERSION}-${framework}-${os_arch}.zip"

        # Make a zip file for every architecture to be distributed
        cd "${PUBLISH_PATH}"
        zip -r "../../../${ZIP_OUTPUT}" * # /bin/Releases directory
        cd "../../../../../" # build.sh directory
    done
done
