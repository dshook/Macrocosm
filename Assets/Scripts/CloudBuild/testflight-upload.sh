#!/bin/sh

buildTarget=${1:-dev-ios}
echo "Getting Version Info for ${buildTarget}"
# Get the JSON data of the latest Cloud Build
curl --header "Content-type: application/json" --header "Authorization: Basic 00000000000000000000000000000000" "https://build-api.cloud.unity3d.com/api/v1/orgs/dshook/projects/test/buildtargets/$buildTarget/builds" -o ipaLocation.txt

# Find the index of the last successful build
buildIndex=0
for (( ; ; ))
do
  status=$(jq ".[$buildIndex].buildStatus" ipaLocation.txt)

  if [[ $status == null ]]; then
    echo "No Successful build found"
    exit;
  fi

  if [ "$status" == '"success"' ]; then
    break
  fi
  buildIndex=$((buildIndex + 1))
done

# Retrieve the URL of the IPA file
cat ipaLocation.txt | jq ".[$buildIndex].links.download_primary.href" | sed '1s/^/url = /' > ipaLocationStripped.txt

buildNumber=$(jq ".[$buildIndex].build" ipaLocation.txt)

echo "Downloading IPA build $buildNumber"
# Download the IPA build
curl --config ipaLocationStripped.txt -o build.ipa

echo "Uploading build $buildNumber"
# Upload the build to iTunes Connect
/Applications/Xcode.app/Contents/Developer/usr/bin/altool --upload-app -f build.ipa -u 'email@solariasoftware.com' -p ''
