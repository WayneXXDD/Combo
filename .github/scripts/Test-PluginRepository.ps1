$ErrorActionPreference = 'Stop'
$testRoot = Join-Path $PSScriptRoot 'test-work'
New-Item -ItemType Directory -Path $testRoot -Force | Out-Null
$env:DOTNET_CLI_HOME = Join-Path $testRoot 'dotnet-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$fixtureCombo = Join-Path $testRoot 'combo'
$fixtureOracle = Join-Path $testRoot 'oracle'
$comboStage = Join-Path $testRoot 'combo-stage'
$oracleRelease = Join-Path $fixtureOracle 'Oracle/bin/Release'
$assertions = 0
function Assert([bool]$Condition, [string]$Name) {
    if (-not $Condition) { throw "FAILED: $Name" }
    $script:assertions++
}
function FixtureAssembly([string]$Name, [string]$Version, [string]$Destination) {
    $project = Join-Path $testRoot $Name
    New-Item -ItemType Directory -Path $project,$Destination -Force | Out-Null
    $projectXml = '<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net10.0</TargetFramework><AssemblyName>' + $Name + '</AssemblyName><Version>' + $Version + '</Version></PropertyGroup></Project>'
    Set-Content -LiteralPath (Join-Path $project "$Name.csproj") -Value $projectXml
    Set-Content -LiteralPath (Join-Path $project 'Fixture.cs') -Value 'public sealed class Fixture { }'
    & dotnet build (Join-Path $project "$Name.csproj") -c Release -o $Destination --nologo -v quiet -p:NuGetAudit=false
    if ($LASTEXITCODE -ne 0) { throw 'Fixture assembly build failed.' }
}
FixtureAssembly 'XIVComboExpandedest' '1.5.0.96' $comboStage
FixtureAssembly 'Oracle' '1.2.3.0' $oracleRelease
FixtureAssembly 'MirageUI' '1.0.0.0' $oracleRelease
New-Item -ItemType Directory -Path (Join-Path $comboStage 'images'),(Join-Path $comboStage 'licenses'),(Join-Path $oracleRelease 'Data/I18n'),(Join-Path $fixtureOracle 'MirageUI'),$fixtureCombo -Force | Out-Null
Set-Content -LiteralPath (Join-Path $comboStage 'XIVComboExpandedest.json') -Value '{"InternalName":"XIVComboExpandedest","AssemblyVersion":"1.5.0.96","Name":"Combo","DalamudApiLevel":15,"Author":"original authors"}'
Set-Content -LiteralPath (Join-Path $oracleRelease 'Oracle.json') -Value '{"InternalName":"Oracle","AssemblyVersion":"0.9.0.0","Name":"Oracle","DalamudApiLevel":"15","Author":"mirage"}'
Set-Content -LiteralPath (Join-Path $comboStage 'licenses/WrathCombo-BSD-3-Clause.txt') -Value 'Fixture BSD notice'
Set-Content -LiteralPath (Join-Path $comboStage 'images/icon.png') -Value 'Fixture image'
Set-Content -LiteralPath (Join-Path $oracleRelease 'Data/plugin-icon.png') -Value 'Fixture image'
Set-Content -LiteralPath (Join-Path $oracleRelease 'Data/I18n/en.json') -Value '{}'
Set-Content -LiteralPath (Join-Path $fixtureCombo 'LICENSE') -Value 'Fixture GPL license'
Set-Content -LiteralPath (Join-Path $fixtureOracle 'LICENSE') -Value 'Fixture AGPL license'
Set-Content -LiteralPath (Join-Path $fixtureOracle 'MirageUI/README.md') -Value 'License: AGPL-3.0-or-later'
$fixtureZipDir = Join-Path $fixtureCombo 'XIVComboExpanded/bin/Release/XIVComboExpandedest'
New-Item -ItemType Directory -Path $fixtureZipDir -Force | Out-Null
Compress-Archive -Path (Join-Path $comboStage '*') -DestinationPath (Join-Path $fixtureZipDir 'latest.zip') -Force
$feedOutput = Join-Path $testRoot 'feed'
& (Join-Path $PSScriptRoot 'New-PluginRepository.ps1') -ComboDirectory $fixtureCombo -OracleDirectory $fixtureOracle -OutputDirectory $feedOutput
$feedText = Get-Content -LiteralPath (Join-Path $feedOutput 'build-manifest.json') -Raw
Assert ($feedText.TrimStart().StartsWith('[')) 'Feed is a JSON array'
$feed = @($feedText | ConvertFrom-Json)
Assert ($feed.Count -eq 2) 'Both plugins in feed'
$oracle = $feed | Where-Object InternalName -eq 'Oracle'
$combo = $feed | Where-Object InternalName -eq 'XIVComboExpandedest'
Assert ($oracle.AssemblyVersion -eq '1.2.3.0') 'Oracle version comes from DLL, not stale source JSON'
Assert ($combo.AssemblyVersion -eq '1.5.0.96') 'Combo version matches DLL'
Assert ($oracle.DalamudApiLevel -eq 15) 'API level normalized'
Assert ($oracle.Author -eq 'mirage') 'Original attribution retained'
Assert ($oracle.RepoUrl -eq 'https://github.com/WayneXXDD/Oracle') 'Source URL points at own fork'
foreach ($entry in $feed) {
    Assert ($entry.DownloadLinkInstall -eq $entry.DownloadLinkUpdate) 'Install and update point at same package'
    Assert ($entry.DownloadLinkInstall.StartsWith('https://raw.githubusercontent.com/WayneXXDD/Combo/plugin-repo/plugins/')) 'Download hosted in own repository'
    $zip = [IO.Compression.ZipFile]::OpenRead((Join-Path $feedOutput "plugins/$($entry.InternalName)/latest.zip"))
    try {
        $names = @($zip.Entries.FullName | ForEach-Object { $_.Replace('\','/') })
        Assert ($names -contains "$($entry.InternalName).dll") 'DLL at archive root'
        Assert ($names -contains "$($entry.InternalName).json") 'Manifest at archive root'
        Assert (-not ($names | Where-Object { $_.EndsWith('.zip') })) 'No nested ZIP'
        if ($entry.InternalName -eq 'Oracle') {
            Assert ($names -contains 'MirageUI.dll') 'Oracle shared UI dependency included'
            Assert ($names -contains 'Data/I18n/en.json') 'Oracle localization included'
            Assert ($names -contains 'licenses/MirageUI-NOTICE.txt') 'MirageUI README license fallback'
        } else {
            Assert ($names -contains 'licenses/WrathCombo-BSD-3-Clause.txt') 'BST attribution retained'
        }
    } finally { $zip.Dispose() }
}
Assert (@(Get-ChildItem -LiteralPath $testRoot -Directory -Filter 'package-stage-*').Count -eq 0) 'Package staging cleaned'
$sourceRepo = Join-Path $testRoot 'git-guard'
New-Item -ItemType Directory -Path $sourceRepo -Force | Out-Null
& git -C $sourceRepo init -b master | Out-Null
& git -C $sourceRepo remote add origin 'https://github.com/wrong/Repo.git'
$blocked = $false
try {
    & (Join-Path $PSScriptRoot 'Publish-PluginRepository.ps1') -SourceCheckout $sourceRepo -FeedDirectory $feedOutput -ExpectedRepository 'WayneXXDD/Combo'
} catch { $blocked = $_.Exception.Message -like '*Unexpected Git origin*' }
Assert $blocked 'Publication rejects an unexpected repository'
Assert ((& git -C $sourceRepo branch --show-current) -eq 'master') 'Source branch untouched on rejection'
$missingDependency = Join-Path $oracleRelease 'MirageUI.dll'
Remove-Item -LiteralPath $missingDependency
$blocked = $false
try {
    & (Join-Path $PSScriptRoot 'New-PluginRepository.ps1') -ComboDirectory $fixtureCombo -OracleDirectory $fixtureOracle -OutputDirectory (Join-Path $testRoot 'incomplete-feed')
} catch { $blocked = $_.Exception.Message -like '*missing MirageUI.dll*' }
Assert $blocked 'Missing Oracle dependency blocks publication package'
Assert (-not (Test-Path -LiteralPath (Join-Path $testRoot 'incomplete-feed/build-manifest.json'))) 'Incomplete package never exposes a feed'
Write-Output "Plugin repository: $assertions assertions passed."
