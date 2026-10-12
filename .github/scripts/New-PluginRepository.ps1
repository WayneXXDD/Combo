[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$ComboDirectory,
    [Parameter(Mandatory)][string]$OracleDirectory,
    [Parameter(Mandatory)][string]$OutputDirectory,
    [string]$Repository = 'WayneXXDD/Combo',
    [string]$DistributionBranch = 'plugin-repo'
)

$ErrorActionPreference = 'Stop'
$distributionRoot = [IO.Path]::GetFullPath($OutputDirectory)
$comboRoot = (Resolve-Path -LiteralPath $ComboDirectory).ProviderPath
$oracleRoot = (Resolve-Path -LiteralPath $OracleDirectory).ProviderPath
foreach ($sourceRoot in @($comboRoot, $oracleRoot)) {
    if ($distributionRoot -eq $sourceRoot -or $distributionRoot.StartsWith($sourceRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'The distribution directory must be outside the source checkout.'
    }
}
if ($Repository -notmatch '^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$' -or $DistributionBranch -notmatch '^[A-Za-z0-9_.-]+$') {
    throw 'Invalid distribution repository or branch.'
}

$rawBase = "https://raw.githubusercontent.com/$Repository/$DistributionBranch"
$entries = [Collections.Generic.List[object]]::new()
$utf8 = [Text.UTF8Encoding]::new($false)
New-Item -ItemType Directory -Path $distributionRoot -Force | Out-Null

function Write-JsonFile($Value, [string]$Path) {
    [IO.File]::WriteAllText($Path, (ConvertTo-Json -InputObject $Value -Depth 30) + "`n", $utf8)
}

function Copy-License([string]$Source, [string]$Destination) {
    if (-not (Test-Path -LiteralPath $Source -PathType Leaf)) {
        throw "Required license not found: $Source"
    }
    New-Item -ItemType Directory -Path ([IO.Path]::GetDirectoryName($Destination)) -Force | Out-Null
    Copy-Item -LiteralPath $Source -Destination $Destination -Force
}

function Complete-PluginPackage([string]$Stage, [string]$InternalName, [string]$Name, [string]$SourceRepo, [string]$SourceBranch) {
    $dll = Join-Path $Stage "$InternalName.dll"
    $manifestPath = Join-Path $Stage "$InternalName.json"
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf) -or -not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) {
        throw "Package is missing the root DLL or manifest for $InternalName."
    }

    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json -AsHashtable
    # Oracle's source manifest has a manually maintained version. Always use DLL metadata.
    $manifest.InternalName = $InternalName
    $manifest.AssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($dll).Version.ToString()
    if ([version]$manifest.AssemblyVersion -eq [version]'0.0.0.0') {
        throw "Invalid assembly version for $InternalName."
    }
    $manifest.Name = $Name
    $manifest.RepoUrl = "https://github.com/$SourceRepo"
    $manifest.DalamudApiLevel = [int]$manifest.DalamudApiLevel
    if ($manifest.DalamudApiLevel -le 0) { throw "Missing API level for $InternalName." }

    $entryDirectory = Join-Path $distributionRoot "plugins/$InternalName"
    New-Item -ItemType Directory -Path (Join-Path $entryDirectory 'images') -Force | Out-Null
    $icon = if ($InternalName -eq 'Oracle') { Join-Path $Stage 'Data/plugin-icon.png' } else { Join-Path $Stage 'images/icon.png' }
    if (Test-Path -LiteralPath $icon -PathType Leaf) {
        Copy-Item -LiteralPath $icon -Destination (Join-Path $entryDirectory 'images/icon.png') -Force
        $manifest.IconUrl = "$rawBase/plugins/$InternalName/images/icon.png"
    }
    $manifest.ImageUrls = @() # Keep the repository independent of upstream image hosting.
    foreach ($linkName in @('DownloadLinkInstall','DownloadLinkUpdate','DownloadLinkTesting')) {
        $manifest[$linkName] = "$rawBase/plugins/$InternalName/latest.zip"
    }
    $manifest.IsHide = $false
    $manifest.IsTestingExclusive = $false
    $manifest.LastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    Write-JsonFile $manifest $manifestPath
    Write-JsonFile $manifest (Join-Path $entryDirectory "$InternalName.json")

    $zipPath = Join-Path $entryDirectory 'latest.zip'
    # The output directory is distinct from the stage, so the archive cannot include itself.
    Compress-Archive -Path (Join-Path $Stage '*') -DestinationPath $zipPath -Force
    $archive = [IO.Compression.ZipFile]::OpenRead($zipPath)
    try {
        $names = @($archive.Entries.FullName | ForEach-Object { $_.Replace('\','/') })
        foreach ($required in @("$InternalName.dll", "$InternalName.json")) {
            if ($names -notcontains $required) { throw "ZIP layout is invalid for $InternalName." }
        }
        if (-not ($names | Where-Object { $_.StartsWith('licenses/') })) {
            throw "ZIP is missing license notices for $InternalName."
        }
        if ($InternalName -eq 'Oracle' -and $names -notcontains 'MirageUI.dll') {
            throw 'Oracle ZIP is missing MirageUI.dll.'
        }
    }
    finally { $archive.Dispose() }
    $entries.Add($manifest)
}

$stagingRoot = Join-Path ([IO.Path]::GetDirectoryName($distributionRoot)) ('package-stage-' + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stagingRoot | Out-Null
try {
    $comboStage = Join-Path $stagingRoot 'Combo'
    $comboZip = Join-Path $comboRoot 'XIVComboExpanded/bin/Release/XIVComboExpandedest/latest.zip'
    Expand-Archive -LiteralPath $comboZip -DestinationPath $comboStage
    Copy-License (Join-Path $comboRoot 'LICENSE') (Join-Path $comboStage 'licenses/Combo-GPL-3.0.txt')
    Complete-PluginPackage $comboStage 'XIVComboExpandedest' 'XIV Combo Expandedest (Wayne)' 'WayneXXDD/Combo' 'master'

    $oracleStage = Join-Path $stagingRoot 'Oracle'
    New-Item -ItemType Directory -Path $oracleStage | Out-Null
    $oracleRelease = Join-Path $oracleRoot 'Oracle/bin/Release'
    foreach ($releaseItem in (Get-ChildItem -LiteralPath $oracleRelease -Force)) {
        if (-not $releaseItem.PSIsContainer -and $releaseItem.Extension -in @('.pdb','.xml')) { continue }
        Copy-Item -LiteralPath $releaseItem.FullName -Destination $oracleStage -Recurse -Force
    }
    Copy-License (Join-Path $oracleRoot 'LICENSE') (Join-Path $oracleStage 'licenses/Oracle-AGPL-3.0.txt')
    $mirageLicenses = @(Get-ChildItem -LiteralPath (Join-Path $oracleRoot 'MirageUI') -File -Filter 'LICENSE*')
    if ($mirageLicenses.Count -eq 0) {
        $mirageReadme = Join-Path $oracleRoot 'MirageUI/README.md'
        if (-not (Test-Path -LiteralPath $mirageReadme) -or
            (Get-Content -LiteralPath $mirageReadme -Raw) -notmatch 'AGPL-3\.0-or-later') {
            throw 'MirageUI license declaration not found.'
        }
        # This MirageUI revision declares AGPL-3.0-or-later in README rather than a LICENSE file.
        Copy-License (Join-Path $oracleRoot 'LICENSE') (Join-Path $oracleStage 'licenses/MirageUI-AGPL-3.0.txt')
        [IO.File]::WriteAllText((Join-Path $oracleStage 'licenses/MirageUI-NOTICE.txt'),
            "MirageUI by mirage. Source: https://github.com/exatrines/MirageUI`nThe upstream README declares AGPL-3.0-or-later; the license text is included alongside this notice.`n", $utf8)
    } else {
        foreach ($licenseFile in $mirageLicenses) {
            Copy-License $licenseFile.FullName (Join-Path $oracleStage ('licenses/MirageUI-' + $licenseFile.Name))
        }
    }
    Complete-PluginPackage $oracleStage 'Oracle' 'Oracle (Wayne)' 'WayneXXDD/Oracle' 'main'

    Write-JsonFile @($entries.ToArray() | Sort-Object InternalName) (Join-Path $distributionRoot 'build-manifest.json')
    [IO.File]::WriteAllText((Join-Path $distributionRoot 'README.md'), @"
# Wayne's Dalamud plugin repository

Add this URL in /xlsettings > Experimental > Custom Plugin Repositories:

https://raw.githubusercontent.com/WayneXXDD/DalamudPlugins/main/pluginmaster.json

Then open /xlplugins and install **XIV Combo Expandedest (Wayne)** or **Oracle (Wayne)**.

The source repositories and their original licenses are linked in each manifest and included in each ZIP.
The installer compares numeric AssemblyVersion values. Increase the source project's numeric assembly version before publishing an update.
"@, $utf8)
    Write-Output ('Repository ready: ' + (($entries | ForEach-Object { "$($_.InternalName) $($_.AssemblyVersion)" }) -join ', '))
}
finally {
    $resolvedStage = (Resolve-Path -LiteralPath $stagingRoot).ProviderPath
    if ([IO.Path]::GetFullPath($resolvedStage) -ne [IO.Path]::GetFullPath($stagingRoot) -or
        (Get-Item -LiteralPath $resolvedStage -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) {
        throw 'Unexpected package staging path; cleanup stopped.'
    }
    Remove-Item -LiteralPath $resolvedStage -Recurse -Force
}
