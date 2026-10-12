[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$SourceCheckout,
    [Parameter(Mandatory)][string]$FeedDirectory,
    [Parameter(Mandatory)][string]$ExpectedRepository,
    [string]$DistributionBranch = 'plugin-repo'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
if ($DistributionBranch -ne 'plugin-repo') { throw 'This publisher only writes the plugin-repo branch.' }
$checkoutRoot = (Resolve-Path -LiteralPath $SourceCheckout).ProviderPath
$feedRoot = (Resolve-Path -LiteralPath $FeedDirectory).ProviderPath
if ($feedRoot -eq $checkoutRoot -or $feedRoot.StartsWith($checkoutRoot + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'The feed must be staged outside the source checkout.'
}
$newEntries = @(Get-Content -LiteralPath (Join-Path $feedRoot 'build-manifest.json') -Raw | ConvertFrom-Json)
if ($newEntries.Count -ne 2 -or ((@($newEntries.InternalName | Sort-Object) -join ',') -ne 'Oracle,XIVComboExpandedest')) {
    throw 'Expected Combo and Oracle in the generated feed.'
}
function Invoke-RepoGit([string[]]$Arguments) {
    & git -C $checkoutRoot @Arguments
    if ($LASTEXITCODE -ne 0) { throw "Git operation failed: $($Arguments[0])" }
}
$origin = & git -C $checkoutRoot remote get-url origin
if ($LASTEXITCODE -ne 0 -or $origin.TrimEnd('/') -notin @("https://github.com/$ExpectedRepository", "https://github.com/$ExpectedRepository.git")) {
    throw 'Unexpected Git origin; publication stopped.'
}
$remoteBranch = & git -C $checkoutRoot ls-remote --heads origin "refs/heads/$DistributionBranch"
if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect the distribution branch.' }
if ($remoteBranch) {
    Invoke-RepoGit @('fetch','--depth','1','origin',"refs/heads/${DistributionBranch}:refs/remotes/origin/$DistributionBranch")
    Invoke-RepoGit @('checkout','-B',$DistributionBranch,"refs/remotes/origin/$DistributionBranch")
} else {
    Invoke-RepoGit @('checkout','--orphan',$DistributionBranch)
    # Only the disposable runner checkout is cleared; source branches are never changed.
    Invoke-RepoGit @('rm','-r','-f','--ignore-unmatch','--','.')
}
if ((& git -C $checkoutRoot branch --show-current) -ne $DistributionBranch) { throw 'Refusing to write a source branch.' }

$previousFeed = Join-Path $checkoutRoot 'build-manifest.json'
if (-not (Test-Path -LiteralPath $previousFeed)) {
    $previousFeed = Join-Path $checkoutRoot 'pluginmaster.json'
}
if (Test-Path -LiteralPath $previousFeed) {
    $oldEntries = @(Get-Content -LiteralPath $previousFeed -Raw | ConvertFrom-Json)
    foreach ($entry in $newEntries) {
        $old = $oldEntries | Where-Object InternalName -eq $entry.InternalName | Select-Object -First 1
        if ($old -and [version]$entry.AssemblyVersion -lt [version]$old.AssemblyVersion) {
            throw "Refusing to publish an older version of $($entry.InternalName)."
        }
    }
}
Invoke-RepoGit @('rm','-f','--ignore-unmatch','--','pluginmaster.json')
foreach ($feedItem in (Get-ChildItem -LiteralPath $feedRoot -Force)) {
    Copy-Item -LiteralPath $feedItem.FullName -Destination $checkoutRoot -Recurse -Force
}
Invoke-RepoGit @('config','user.name','github-actions[bot]')
Invoke-RepoGit @('config','user.email','41898282+github-actions[bot]@users.noreply.github.com')
Invoke-RepoGit @('add','--all')
& git -C $checkoutRoot diff --cached --quiet
if ($LASTEXITCODE -eq 0) { Write-Output 'Repository already up to date.'; exit 0 }
if ($LASTEXITCODE -ne 1) { throw 'Cannot inspect staged changes.' }
Invoke-RepoGit @('commit','-m','Publish Combo and Oracle plugin repository')
Invoke-RepoGit @('push','origin',"HEAD:refs/heads/$DistributionBranch")
