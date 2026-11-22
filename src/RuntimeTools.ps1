function Get-MediaM8RuntimeToolPaths {
    param([string[]]$SearchDirectories)

    [pscustomobject]@{
        YtDlp  = Find-Executable -Name 'yt-dlp.exe' -SearchDirectories $SearchDirectories
        FFmpeg = Find-Executable -Name 'ffmpeg.exe' -SearchDirectories $SearchDirectories
        FFprobe = Find-Executable -Name 'ffprobe.exe' -SearchDirectories $SearchDirectories
        Deno   = Find-Executable -Name 'deno.exe' -SearchDirectories $SearchDirectories
    }
}

function Get-MediaM8MissingRuntimeTools {
    param([Parameter(Mandatory = $true)]$ToolPaths)

    $missing = @()
    if (-not $ToolPaths.YtDlp)  { $missing += 'yt-dlp' }
    if (-not $ToolPaths.FFmpeg) { $missing += 'FFmpeg' }
    if (-not $ToolPaths.FFprobe) { $missing += 'ffprobe' }
    if (-not $ToolPaths.Deno)   { $missing += 'Deno' }
    return $missing
}

function Show-MediaM8InstallPrompt {
    param(
        [string[]]$MissingTools,
        [System.Windows.Forms.IWin32Window]$Owner
    )

    $dialog = New-Object System.Windows.Forms.Form
    $dialog.Text = 'Required tools are missing'
    $dialog.StartPosition = 'CenterParent'
    $dialog.FormBorderStyle = 'FixedDialog'
    $dialog.MaximizeBox = $false
    $dialog.MinimizeBox = $false
    $dialog.ShowInTaskbar = $false
    $dialog.ClientSize = New-Object System.Drawing.Size(465, 190)
    $dialog.Font = New-Object System.Drawing.Font('Segoe UI', 9)

    $label = New-Object System.Windows.Forms.Label
    $label.Location = New-Object System.Drawing.Point(20, 18)
    $label.Size = New-Object System.Drawing.Size(425, 105)
    $label.Text = @(
        'Media M8 needs the following tools:',
        '',
        ($MissingTools -join ', '),
        '',
        'Install them now from their official project releases?'
    ) -join [Environment]::NewLine
    $dialog.Controls.Add($label)

    $installButton = New-Object System.Windows.Forms.Button
    $installButton.Text = 'Install'
    $installButton.Location = New-Object System.Drawing.Point(275, 142)
    $installButton.Size = New-Object System.Drawing.Size(80, 30)
    $installButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
    $dialog.AcceptButton = $installButton
    $dialog.Controls.Add($installButton)

    $cancelButton = New-Object System.Windows.Forms.Button
    $cancelButton.Text = 'Cancel'
    $cancelButton.Location = New-Object System.Drawing.Point(365, 142)
    $cancelButton.Size = New-Object System.Drawing.Size(80, 30)
    $cancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
    $dialog.CancelButton = $cancelButton
    $dialog.Controls.Add($cancelButton)

    return ($dialog.ShowDialog($Owner) -eq [System.Windows.Forms.DialogResult]::OK)
}

function Start-MediaM8RuntimeToolInstall {
    param(
        [Parameter(Mandatory = $true)][string]$ToolsDirectory,
        [Parameter(Mandatory = $true)][string]$LogFile
    )

    $installerScript = Join-Path $PSScriptRoot 'InstallRuntimeTools.ps1'
    if (-not (Test-Path -LiteralPath $installerScript -PathType Leaf)) {
        throw "Installer script is missing: $installerScript"
    }

    $powerShellExe = Join-Path $PSHOME 'powershell.exe'
    $argumentLine = @(
        '-NoLogo',
        '-NoProfile',
        '-ExecutionPolicy', 'Bypass',
        '-File', ('"' + $installerScript + '"'),
        '-ToolsDirectory', ('"' + $ToolsDirectory + '"'),
        '-LogFile', ('"' + $LogFile + '"')
    ) -join ' '

    return Start-Process -FilePath $powerShellExe -ArgumentList $argumentLine -WindowStyle Hidden -PassThru
}
