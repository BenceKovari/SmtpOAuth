Connect-AzureAD

# App regisztráció
$appName = "EmailTest"
$appReplyURLs = @("http://localhost:5001/signin-microsoft", "https://localhost:44329/signin-microsoft")
if(!($myApp = Get-AzureADApplication -Filter "DisplayName eq '$($appName)'"  -ErrorAction SilentlyContinue))
{
    $myApp = New-AzureADApplication -DisplayName $appName -ReplyUrls $appReplyURLs -AvailableToOtherTenants $true
}

# Létrehozott app adatai
$myApp

# App secret létrehozása
$objectId = (Get-AzureADApplication -SearchString $appName)[0].ObjectId

$startDate = Get-Date
$endDate = $startDate.AddYears(15)
$aadAppsecret01 = New-AzureADApplicationPasswordCredential -ObjectId $objectId -CustomKeyIdentifier "SecretKey" -StartDate $startDate -EndDate $endDate

$aadAppsecret01