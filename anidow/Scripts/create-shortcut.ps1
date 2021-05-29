$TargetFile = $args[0]
$ShortcutFile = $args[1]
$WorkingDir = $args[2]

if ($null -eq $TargetFile) {
    exit 1
}
if ($null -eq $WorkingDir) {
    exit 1
}

$WScriptShell = New-Object -ComObject WScript.Shell

$Shortcut = $WScriptShell.CreateShortcut($ShortcutFile)
$Shortcut.TargetPath = $TargetFile
$Shortcut.WorkingDirectory = $WorkingDir;
$Shortcut.Arguments = "/autostart";
$Shortcut.Save()