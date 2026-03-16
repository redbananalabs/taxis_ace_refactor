param(
    [string]$Context = "TaxiDispatchContext",
    [string]$Project = "TaxiDispatch.Lib/TaxiDispatch.Lib.csproj",
    [string]$StartupProject = "TaxiDispatch.API/TaxiDispatch.API.csproj",
    [string]$Environment = "Production"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$env:ASPNETCORE_ENVIRONMENT = $Environment

dotnet ef database update `
    --context $Context `
    --project $Project `
    --startup-project $StartupProject

if ($LASTEXITCODE -ne 0) {
    throw "dotnet ef database update failed with exit code $LASTEXITCODE."
}

