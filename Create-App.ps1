#Install-Module AzureAD -Scope CurrentUser
#Import-Module AzureAD


Connect-AzureAD

# App regisztráció
$appName = "EmailTest"
$appReplyURLs = @("http://localhost:5001/signin-microsoft", "https://localhost:44329/signin-microsoft")
$appReplyURLs = @("http://localhost")
if(!($myApp = Get-AzureADApplication -Filter "DisplayName eq '$($appName)'"  -ErrorAction SilentlyContinue))
{
    $myApp = New-AzureADApplication -DisplayName $appName -ReplyUrls $appReplyURLs -AvailableToOtherTenants $true
}

# Létrehozott app adatai
$myApp

# App secret létrehozása
#$objectId = (Get-AzureADApplication -SearchString $appName)[0].ObjectId

#$startDate = Get-Date
#$endDate = $startDate.AddYears(15)
#$aadAppsecret01 = New-AzureADApplicationPasswordCredential -ObjectId $objectId -CustomKeyIdentifier "SecretKey" -StartDate $startDate -EndDate $endDate

#$aadAppsecret01


$exchangeApi = Get-AzureADServicePrincipal -Filter "DisplayName eq 'Office 365 Exchange Online'"

$resourceAccess = New-Object -TypeName Microsoft.Open.AzureAD.Model.ResourceAccess
#https://learn.microsoft.com/en-us/graph/permissions-reference
#$resourceAccess.Id = "C87F6A14-2B6E-4C4C-8E6B-8C9A1C1C9C3C"  #
$resourceAccess.Id = "258f6531-6087-4cc4-bb90-092c5fb3ed3f" # SMTP.Send
$resourceAccess.Type = "Scope"
$requiredResourceAccess = New-Object -TypeName Microsoft.Open.AzureAD.Model.RequiredResourceAccess
$requiredResourceAccess.ResourceAppId = $exchangeApi.AppId 
$requiredResourceAccess.ResourceAccess = @($resourceAccess2)

$resourceAccessList = New-Object 'System.Collections.Generic.List[Microsoft.Open.AzureAD.Model.ResourceAccess]'
$resourceAccessList.Add($resourceAccess2)
$requiredResourceAccess.ResourceAccess = $resourceAccessList

Set-AzureADApplication -ObjectId $myApp.ObjectId -RequiredResourceAccess $requiredResourceAccess