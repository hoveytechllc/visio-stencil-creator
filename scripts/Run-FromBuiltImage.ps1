param (
    [string] $outputFilename, 
    [string] $contentPath
)

SET GIT_REDIRECT_STDERR="2>&1"

if (!$outputFilename){
    $outputFilename = "Generated"
}
if (!$contentPath){
    $contentPath = $PWD
}

$githubOrganization="hoveytechllc"
$repositoryName="visio-stencil-creator"

Get-ChildItem -Path .\${repositoryName} -Recurse | Remove-Item -Force -Recurse

# Clone repository in current path
git clone -q https://github.com/${githubOrganization}/${repositoryName}.git

# build image using Dockerfile from github repository
docker build `
    -t ${repositoryName}:latest `
    -f ./${repositoryName}/Dockerfile `
    ./${repositoryName}

# Run newly created Docker image
docker run `
    -v ${contentPath}:/app/content `
    ${repositoryName}:latest `
    "*.png" `
    "/app/content" `
    "/app/content/${outputFilename}.vssx"

Get-ChildItem -Path .\${repositoryName} -Recurse | Remove-Item -Force -Recurse
Remove-Item -Force -Recurse .\${repositoryName}