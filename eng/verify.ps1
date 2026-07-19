[CmdletBinding()]
param([switch] $SkipDocker)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path -LiteralPath (Join-Path $PSScriptRoot '..')).Path
$solution = Join-Path $repositoryRoot 'Gma.Modules.Administration.slnx'

& (Join-Path $PSScriptRoot 'check-boundaries.ps1')
& dotnet restore $solution
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& dotnet build $solution --no-restore -m:1
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& (Join-Path $PSScriptRoot 'check-migrations.ps1') -NoBuild
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& dotnet test (Join-Path $repositoryRoot 'tests\Gma.Modules.Administration.Tests\Gma.Modules.Administration.Tests.csproj') --no-build --logger 'console;verbosity=minimal'
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
& dotnet list $solution package --vulnerable --include-transitive
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

if (-not $SkipDocker) {
    $env:GMA_REQUIRE_DOCKER_TESTS = 'true'
    & dotnet test (Join-Path $repositoryRoot 'tests\Gma.Modules.Administration.IntegrationTests\Gma.Modules.Administration.IntegrationTests.csproj') --no-build --logger 'console;verbosity=minimal'
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

Write-Host 'Administration verification passed.'
