param(
    [switch]$DemoMode
)

$ProjectRoot = $PSScriptRoot
$ApiPath = Join-Path $ProjectRoot "src\LearnFlow.Api"
$ClientPath = Join-Path $ProjectRoot "client"

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw ".NET 8 SDK was not found. Install it before starting LearnFlow."
}

if (-not (Get-Command npm -ErrorAction SilentlyContinue)) {
    throw "Node.js and npm were not found. Install them before starting LearnFlow."
}

if (-not (Test-Path (Join-Path $ClientPath "node_modules"))) {
    Write-Host "Installing client packages..." -ForegroundColor Cyan
    Push-Location $ClientPath
    npm install
    Pop-Location
}

$CamundaEnabled = if ($DemoMode) { "false" } else { "true" }

Start-Process powershell -WorkingDirectory $ApiPath -ArgumentList @(
    "-NoExit",
    "-Command",
    "`$env:Camunda__Enabled='$CamundaEnabled'; dotnet run"
)

Start-Sleep -Seconds 2

Start-Process powershell -WorkingDirectory $ClientPath -ArgumentList @(
    "-NoExit",
    "-Command",
    "npm run dev"
)

Write-Host ""
Write-Host "LearnFlow is starting:" -ForegroundColor Green
Write-Host "  Application: http://localhost:5173"
Write-Host "  API/Swagger: http://localhost:5192/swagger"
if ($DemoMode) {
    Write-Host "  Workflow: local demo mode"
} else {
    Write-Host "  Workflow: Camunda 8 at http://localhost:8080"
}
