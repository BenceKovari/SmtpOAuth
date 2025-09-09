# PowerShell script to register application for Microsoft Graph Email sending
# Run this script with appropriate permissions

# Install the module if not already installed
# Install-Module AzureAD -Force -AllowClobber

# Connect to Azure AD
Connect-AzureAD

# Define application details
$AppName = "Graph Email Sender"
$RedirectUri = "http://localhost"

# Create the application
$App = New-AzureADApplication -DisplayName $AppName -ReplyUrls $RedirectUri -PublicClient $true

# Get the application ID
$AppId = $App.AppId

Write-Host "Application registered successfully!" -ForegroundColor Green
Write-Host "Application ID: $AppId" -ForegroundColor Yellow
Write-Host "Tenant ID: $((Get-AzureADTenantDetail).ObjectId)" -ForegroundColor Yellow

# Define required permissions for Microsoft Graph
$GraphServicePrincipal = Get-AzureADServicePrincipal -Filter "DisplayName eq 'Microsoft Graph'"

# Mail.Send delegated permission
$MailSendPermission = $GraphServicePrincipal.OAuth2Permissions | Where-Object {$_.Value -eq "Mail.Send"}

# Add required permissions
$RequiredResourceAccess = New-Object -TypeName "Microsoft.Open.AzureAD.Model.RequiredResourceAccess"
$RequiredResourceAccess.ResourceAppId = $GraphServicePrincipal.AppId
$RequiredResourceAccess.ResourceAccess = New-Object -TypeName "Microsoft.Open.AzureAD.Model.ResourceAccess" -ArgumentList $MailSendPermission.Id, "Scope"

# Update the application with required permissions
Set-AzureADApplication -ObjectId $App.ObjectId -RequiredResourceAccess $RequiredResourceAccess

Write-Host ""
Write-Host "Required permissions added:" -ForegroundColor Green
Write-Host "- Mail.Send (Delegated)" -ForegroundColor White

Write-Host ""
Write-Host "IMPORTANT: Update your C# code with these values:" -ForegroundColor Red
Write-Host "CLIENT_ID = `"$AppId`"" -ForegroundColor White
Write-Host "TENANT_ID = `"$((Get-AzureADTenantDetail).ObjectId)`"" -ForegroundColor White

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "1. Update the CLIENT_ID and TENANT_ID in your C# application" -ForegroundColor White
Write-Host "2. Run your C# application" -ForegroundColor White
Write-Host "3. The first run will require interactive authentication" -ForegroundColor White
Write-Host "4. Subsequent runs will use cached refresh tokens" -ForegroundColor White