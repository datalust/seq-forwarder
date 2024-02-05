$ErrorActionPreference = 'Stop'

$framework = 'net8.0'

function Clean-Output
{
    Write-Output "Cleaning output"
	if(Test-Path ./artifacts) { rm ./artifacts -Force -Recurse }
}

function Restore-Packages
{
    Write-Output "Restoring packages"
	& dotnet restore
}

function Execute-Tests
{
    Write-Output "Testing native platform version"
    & dotnet test ./test/Seq.Forwarder.Tests/Seq.Forwarder.Tests.csproj -c Release /p:Configuration=Release /p:Platform=x64 /p:VersionPrefix=$version
    if($LASTEXITCODE -ne 0) { throw "failed" }
}

function Create-ArtifactDir
{
    Write-Output "Creating artifacts directory"
	mkdir ./artifacts
}

function Publish-Archives($version)
{
	$rids = @("linux-x64", "osx-x64", "linux-arm64", "osx-arm64", "win-x64")
	foreach ($rid in $rids) {
        Write-Output "Publishing archive for $rid"
		
		& dotnet publish src/Seq.Forwarder/Seq.Forwarder.csproj -c Release -f $framework -r $rid /p:VersionPrefix=$version /p:SeqForwarderRid=$rid
        if($LASTEXITCODE -ne 0) { throw "failed" }

		# Make sure the archive contains a reasonable root filename
		mv ./src/Seq.Forwarder/bin/Release/$framework/$rid/publish/ ./src/Seq.Forwarder/bin/Release/$framework/$rid/seqfwd-$version-$rid/

		if ($rid.StartsWith("win-")) {
			& ./build/7-zip/7za.exe a -tzip ./artifacts/seqfwd-$version-$rid.zip ./src/Seq.Forwarder/bin/Release/$framework/$rid/seqfwd-$version-$rid/
            if($LASTEXITCODE -ne 0) { throw "failed" }
		} else {
			& ./build/7-zip/7za.exe a -ttar seqfwd-$version-$rid.tar ./src/Seq.Forwarder/bin/Release/$framework/$rid/seqfwd-$version-$rid/
            if($LASTEXITCODE -ne 0) { throw "failed" }

			# Back to the original directory name
			mv ./src/Seq.Forwarder/bin/Release/$framework/$rid/seqfwd-$version-$rid/ ./src/Seq.Forwarder/bin/Release/$framework/$rid/publish/
			
			& ./build/7-zip/7za.exe a -tgzip ./artifacts/seqfwd-$version-$rid.tar.gz seqfwd-$version-$rid.tar
            if($LASTEXITCODE -ne 0) { throw "failed" }

			rm seqfwd-$version-$rid.tar
		}
	}
}

Push-Location $PSScriptRoot

$version = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "99.99.99" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];
Write-Output "Building version $version"

Clean-Output
Create-ArtifactDir
Restore-Packages
Publish-Archives($version)
Execute-Tests

Pop-Location

Write-Output "Done."
