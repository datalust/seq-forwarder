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

    foreach ($file in ls ./src/*/Properties/AssemblyInfo.cs)  
    {     
        (cat $file) | foreach {  
                % {$_ -replace $versionPattern, "$version.0" }             
            } | sc -Encoding "UTF8" $file                                 
    }  
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

function Execute-MSBuild
{
	& msbuild ./seq-forwarder.sln /t:Rebuild /p:Configuration=Release /p:Platform=x64
}

function Execute-Tests
{
	& ./packages/xunit.runner.console.2.1.0/tools/xunit.console.x86.exe ./test/Seq.Forwarder.Tests/bin/Release/Seq.Forwarder.Tests.dll
}

function Publish-Artifacts($version)
{
	mkdir ./artifacts
	mv ./setup/SeqForwarder/bin/Release/SeqForwarder.msi ./artifacts/SeqForwarder-$version-pre.msi
}

Push-Location $PSScriptRoot

$version = @{ $true = $env:APPVEYOR_BUILD_VERSION; $false = "0.0.0" }[$env:APPVEYOR_BUILD_VERSION -ne $NULL];

Clean-Output
Restore-Packages
Update-AssemblyInfo($version)
Update-WixVersion($version)
Execute-MSBuild
Execute-Tests
Publish-Artifacts($version)

Pop-Location
