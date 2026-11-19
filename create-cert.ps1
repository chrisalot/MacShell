$cert = New-SelfSignedCertificate -DnsName 'MacShell.Test' -CertStoreLocation 'Cert:\CurrentUser\My' -Type CodeSigningCert -Subject 'CN=MacShell Test' -KeyUsage DigitalSignature -FriendlyName 'MacShell Test Certificate' -NotAfter (Get-Date).AddYears(1)
$pwd = ConvertTo-SecureString -String 'test' -Force -AsPlainText
Export-PfxCertificate -Cert $cert -FilePath 'C:/Users/cperh/macshell/macshell.pfx' -Password $pwd
Write-Host "Thumbprint: $($cert.Thumbprint)"