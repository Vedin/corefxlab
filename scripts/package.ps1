﻿Param(
    [string]$Configuration="Debug",
    [string]$ApiKey,
    [string]$BuildVersion=[System.DateTime]::Now.ToString('eyyMMdd-1')
)

$repoRoot = "$PSScriptRoot\.."
$dotnetExePath="$repoRoot\dotnet\dotnet.exe"
$nugetPath = "$repoRoot\nuget\nuget.exe"
$packagesPath = "$repoRoot\packages"

Function Ensure-Nuget-Exists {
    if (!(Test-Path "$nugetPath")) {
        if (!(Test-Path "$repoRoot\nuget")) {
            New-Item -ItemType directory -Path "$repoRoot\nuget"
        }
        Write-Host "nuget.exe not found. Downloading to $nugetPath"
        Invoke-WebRequest "https://nuget.org/nuget.exe" -OutFile $nugetPath
    }
}

Write-Host "** Building all NuGet packages. **"
foreach ($file in [System.IO.Directory]::EnumerateFiles("$repoRoot\src", "System*.csproj", "AllDirectories")) {
    Write-Host "Creating NuGet package for $file..."
    Invoke-Expression "$dotnetExePath pack $file -c $Configuration -o $packagesPath --include-symbols --version-suffix $BuildVersion"

    if (!$?) {
        Write-Error "Failed to create NuGet package for project $file"
    }
}

Ensure-Nuget-Exists
Write-Host "** Creating BrotliNative NuGet packages. **"
$brotliExternalFile = "$repoRoot\external\BrotliNative.nuspec"
Invoke-Expression "$nugetPath pack $brotliExternalFile -Version 0.0.1 -o $packagesPath"

if (!$?) {
    Write-Error "Failed to create NuGet package for project $brotliExternalFile"
}

if ($ApiKey)
{
    foreach ($file in [System.IO.Directory]::EnumerateFiles("$packagesPath", "*.nupkg")) {
        try {
            Write-Host "Pushing package $file to MyGet..."
            if($file.EndsWith("symbols.nupkg")) {
                $arguments = "push $file $apiKey -Source https://dotnet.myget.org/F/dotnet-corefxlab/symbols/api/v2/package"
            }
            else { 
                $arguments = "push $file $apiKey -Source https://dotnet.myget.org/F/dotnet-corefxlab/api/v2/package"
            }
            Start-Process -FilePath $nugetPath -ArgumentList $arguments -Wait -PassThru
            Write-Host "done"
        } catch [System.Exception] {
            Write-Host "Failed to push nuget package $file with error $_.Exception.Message"
        }
    }
}
