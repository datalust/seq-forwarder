function Clean-Output
{
	if(Test-Path ./artifacts) { rm ./artifacts -Force -Recurse }
}

function Restore-Packages
{
	& nuget restore
}

function Update-AssemblyInfo($version)
{  
    $versionPattern = "[0-9]+(\.([0-9]+|\*)){3}"

    (cat ./src/Seq.Forwarder.Administration/Properties/AssemblyInfo.cs) | foreach {  
            % {$_ -replace $versionPattern, "$version.0" }             
        } | sc -Encoding "UTF8" $file                                 
}

function Update-WixVersion($version)
{
    $defPattern = "define Version = ""0\.0\.0"""
	$def = "define Version = ""$version"""
    $product = ".\setup\SeqForwarder\Product.wxs"

    (cat $product) | foreach {  
            % {$_ -replace $defPattern, $def }    
        } | sc -Encoding "UTF8" $product
}

function Execute-MSBuild($version, $suffix)
{
	Write-Output "Building $version (suffix=$suffix)"

	if ($suffix) {
		& msbuild ./seq-forwarder.sln /t:Rebuild /p:Configuration=Release /p:Platform=x64 /p:VersionPrefix=$version /p:VersionSuffix=$suffix
	} else {
		& msbuild ./seq-forwarder.sln /t:Rebuild /p:Configuration=Release /p:Platform=x64 /p:VersionPrefix=$version
	}
	if($LASTEXITCODE -ne 0) { exit 1 }
}

function Execute-Tests
{
    pushd ./test/Seq.Forwarder.Tests

    & dotnet test -c Release
    if($LASTEXITCODE -ne 0) { exit 3 }

    popd
}

function Publish-Artifacts($version, $suffix)
{
	$dashsuffix = "";
	if ($suffix) {
		$dashsuffix = "-$suffix";
	}
	mkdir ./artifacts
	mv ./setup/SeqForwarder/bin/Release/SeqForwarder.msi ./artifacts/SeqForwarder-$version$dashsuffix.msi
	if($LASTEXITCODE -ne 0) { exit 1 }
}

Push-Location $PSScriptRoot

$version = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "99.99.99" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];
$branch = @{ $true = $env:APPVEYOR_REPO_BRANCH; $false = $(git symbolic-ref --short -q HEAD) }[$env:APPVEYOR_REPO_BRANCH -ne $NULL];
$revision = @{ $true = "{0:00000}" -f [convert]::ToInt32("0" + $env:APPVEYOR_BUILD_NUMBER, 10); $false = "local" }[$env:APPVEYOR_BUILD_NUMBER -ne $NULL];
$suffix = @{ $true = ""; $false = "$($branch.Substring(0, [math]::Min(10,$branch.Length)))-$revision"}[$branch -eq "master" -and $revision -ne "local"]

Clean-Output
Restore-Packages
Update-WixVersion $version
Update-AssemblyInfo $version
Execute-MSBuild $version $suffix
Execute-Tests
Publish-Artifacts $version $suffix

Pop-Location
