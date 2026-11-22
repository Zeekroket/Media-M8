param(
    [switch]$Sign,
    [string]$CertificateSubject = 'CN=Zeekroket',
    [string]$TimestampServer = 'http://timestamp.digicert.com'
)

$arguments = @{
    Sign = $Sign
    CertificateSubject = $CertificateSubject
    TimestampServer = $TimestampServer
}

& (Join-Path $PSScriptRoot 'build.ps1') @arguments
