#!/bin/bash

function initialise {
  if ! pgrep -x "dockerd" > /dev/null;then
    sudo snap install docker
    sleep 10
  fi
  sudo docker pull digitalag/workflo:latest
  sudo docker pull apsiminitiative/apsimng
}

function run_00001 {
  echo ------------------------------ >> metadata.txt
  echo Date/time: `date +"%Y-%m-%d %T"` >> metadata.txt
  sudo --preserve-env docker run --rm -v $PWD:/wd -w=/wd -e AZURE_ACCOUNT_URL -e AZURE_ACCOUNT_NAME -e AZURE_PRIMARY_ACCESS_KEY -e AZURE_STORAGE_ACCOUNT_NAME -e AZURE_KEY1 -e CLIMATE_API_KEY -e AZURE_STORAGE_CONNECTION_STRING -e AZURE_STORAGE_CONTAINER -e INPUT_FILES digitalag/workflo:latest "Azure.CopyFilesFromStorage($AZURE_STORAGE_CONTAINER, $INPUT_FILES)"
  sudo --preserve-env docker run --rm -v $PWD:/wd -w=/wd -e AZURE_ACCOUNT_URL -e AZURE_ACCOUNT_NAME -e AZURE_PRIMARY_ACCESS_KEY -e AZURE_STORAGE_ACCOUNT_NAME -e AZURE_KEY1 -e POSTATS_UPLOAD_URL -e APSIM_NO_DOCKER -e AZURE_STORAGE_CONNECTION_STRING -e AZURE_STORAGE_CONTAINER -e Path -e DockerImage -e INPUT_FILES "apsiminitiative/apsimplusr:pr-$PR_NUMBER" "$Path" --verbose
}

function run_00001_finally {
  # Function always called, regardless of any errors.
  sudo --preserve-env docker run --rm -v $PWD:/wd -w=/wd -e AZURE_ACCOUNT_URL -e AZURE_ACCOUNT_NAME -e AZURE_PRIMARY_ACCESS_KEY -e AZURE_STORAGE_ACCOUNT_NAME -e AZURE_KEY1 -e CLIMATE_API_KEY -e AZURE_STORAGE_CONNECTION_STRING -e AZURE_STORAGE_CONTAINER -e INPUT_FILES digitalag/workflo:latest "Checksum.CreateHashes(hashes.txt)"
  sudo --preserve-env docker run --rm -v $PWD:/wd -w=/wd -e AZURE_ACCOUNT_URL -e AZURE_ACCOUNT_NAME -e AZURE_PRIMARY_ACCESS_KEY -e AZURE_STORAGE_ACCOUNT_NAME -e AZURE_KEY1 -e CLIMATE_API_KEY -e AZURE_STORAGE_CONNECTION_STRING -e AZURE_STORAGE_CONTAINER -e INPUT_FILES -e OUTPUT_FILES digitalag/workflo:latest "Azure.CopyFilesToStorage($AZURE_STORAGE_CONTAINER, $OUTPUT_FILES, true)"
}



# ==============================================================
# Entry point for script.
start_time=$(date +%s.%N)
(
  echo ------------------------------------------------------------ &>> local.stdout.txt
  echo Running $1 &>> local.stdout.txt
  set -e;
  $1 &>> local.stdout.txt
);
exit_code=$?
if [[ $(type -t $1_finally) == function ]]; then
  $1_finally &>> local.stdout.txt
fi
end_time=$(date +%s.%N)
elapsed=$(echo "$end_time - $start_time" | bc -l)
echo "Elapsed time: $elapsed seconds" &>> local.stdout.txt
if [ $exit_code -gt 0 ]; then
  exit $exit_code
fi