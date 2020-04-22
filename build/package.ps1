# Script static variables
$buildDir = $env:APPVEYOR_BUILD_FOLDER # e.g. C:\projects\rn-common\
$buildNumber = $env:APPVEYOR_BUILD_VERSION # e.g. 1.0.17

$solution = $buildDir + "\FederationGateway.sln"

$projectDir = $buildDir + "\FederationGateway.Core";
$projectFile = $projectDir + "\FederationGateway.Core.csproj";
$testDir = $buildDir + "\FederationGateway.Tests";
$nugetFile = $projectDir + "\FederationGateway.Core." + $buildNumber + ".nupkg";

$ouputDir = $buildDir + "\output"

# Display .Net Core version
Write-Host "Checking .NET Core version" -ForegroundColor Green
& dotnet --version

# Restore the main project
Write-Host "Restoring project" -ForegroundColor Green
& dotnet restore $solution --verbosity m

# Publish the project
#Write-Host "Publishing project" -ForegroundColor Green
#& dotnet publish $solution

# Discover and run tests
Write-Host "Running tests" -ForegroundColor Green
cd $testDir
$testOutput = & dotnet test | Out-String
Write-Host $testOutput

# Ensure that the tests passed
if ($testOutput.Contains("Test Run Successful.") -eq $False) {
  Write-Host "Build failed!";
  Exit;
}

# Generate a NuGet package for publishing
Write-Host "Generating NuGet Package" -ForegroundColor Green
cd $buildDir
& dotnet pack -c Release /p:PackageVersion=$buildNumber -o $ouputDir

# Save generated artifacts
Write-Host "Saving Artifacts" -ForegroundColor Green
$artifacts = Get-ChildItem -Path $ouputDir
foreach($artifact in $artifacts) {
    Write-Host "Pushing $ouputDir\$artifact";
    Push-AppveyorArtifact "$ouputDir\$artifact"
}

# Publish package to NuGet
$artifacts = Get-ChildItem -Path $ouputDir
foreach($artifact in $artifacts) {
    Write-Host "Publishing NuGet package" -ForegroundColor Green
	& nuget push $ouputDir\$artifact" -ApiKey $env:NUGET_API_KEY -Source https://www.nuget.org/api/v2/package
}

# Done
Write-Host "Done!" -ForegroundColor Green