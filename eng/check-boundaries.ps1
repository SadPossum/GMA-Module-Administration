[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$errors = [System.Collections.Generic.List[string]]::new()
$projectFiles = @(Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src') -Recurse -Filter '*.csproj' -File)

function Get-RelativePath {
    param([string] $BasePath, [string] $TargetPath)
    $baseUri = [Uri]::new($BasePath.TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar)
    $targetUri = [Uri]::new($TargetPath)
    return [Uri]::UnescapeDataString($baseUri.MakeRelativeUri($targetUri).ToString()).Replace('/', '\')
}

foreach ($projectFile in $projectFiles) {
    [xml] $project = Get-Content -LiteralPath $projectFile.FullName -Raw
    $relativeProject = Get-RelativePath -BasePath $repositoryRoot -TargetPath $projectFile.FullName
    foreach ($reference in $project.SelectNodes('//ProjectReference')) {
        $include = $reference.GetAttribute('Include')
        if ($include -match '\$\(GmaModule(?!AdministrationRoot\))') {
            $errors.Add("$relativeProject references another reusable module through '$include'.")
        }

        if ($projectFile.BaseName -eq 'Gma.Modules.Administration.Contracts' -and
            $include -notmatch 'Gma\.Framework\.(?:Modules|Permissions)') {
            $errors.Add("$relativeProject gives base Contracts a non-metadata dependency through '$include'.")
        }

        if ($projectFile.BaseName -eq 'Gma.Modules.Administration.Admin.Contracts' -and
            $include -notmatch 'Gma\.Modules\.Administration\.Contracts|Gma\.Framework\.Administration') {
            $errors.Add("$relativeProject gives Admin.Contracts an invalid dependency through '$include'.")
        }
    }
}

$sourceFiles = @(Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src') -Recurse -Filter '*.cs' -File |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' })
foreach ($sourceFile in $sourceFiles) {
    $source = Get-Content -LiteralPath $sourceFile.FullName -Raw
    $relativePath = Get-RelativePath -BasePath $repositoryRoot -TargetPath $sourceFile.FullName
    if ($source -match 'Gma\.Modules\.(?!Administration(?:\.|;))') {
        $errors.Add("$relativePath names another reusable module implementation or contract.")
    }

    if ($source -match '(?:BunkFy|StayQuest)\.') {
        $errors.Add("$relativePath contains product-specific source.")
    }

    if ($relativePath -match '^src\\Gma\.Modules\.Administration\.(?:AdminApi|AdminCli)\\' -and
        $source -match 'Gma\.Modules\.Administration\.Persistence\.(?:Entities|Repositories)') {
        $errors.Add("$relativePath crosses the front-door to persistence-internals boundary.")
    }
}

$applicationPorts = @(Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src\Gma.Modules.Administration.Application\Ports') -Filter '*.cs' -File)
foreach ($portFile in $applicationPorts) {
    $source = Get-Content -LiteralPath $portFile.FullName -Raw
    if ($source -match 'public\s+interface\s+I\w*Repository') {
        $relativePath = Get-RelativePath -BasePath $repositoryRoot -TargetPath $portFile.FullName
        $errors.Add("$relativePath exposes a persistence repository as a public consumer API.")
    }
}

if ($errors.Count -gt 0) {
    throw "Administration boundary checks failed:`n - $($errors -join "`n - ")"
}

Write-Host 'Administration boundary checks passed.'
