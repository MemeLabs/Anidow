
$tool = "dotnet-project-licenses"
function Install() {
    Write-Output "Installing.."
    dotnet.exe tool install --global $tool
}

function Uninstall() {
    Write-Output "Uninstalling..."
    dotnet.exe tool uninstall --global $tool
}

function Run() {
    Write-Output "Generating licenses.json..."
    dotnet-project-licenses -i .\anidow\anidow.csproj -j -f .\anidow\Properties
}

try { dotnet --version }
catch {
    Write-Output "No dotnet installed"
    exit 1
}

Install;
Run;
Uninstall;

Write-Output "finished"