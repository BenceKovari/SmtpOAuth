Connect-AzureAD

$appName = "Webportalok-fejlesztese"
$objectId = (Get-AzureADApplication -SearchString $appName)[0].ObjectId

$startDate = Get-Date
$endDate = $startDate.AddYears(15)
$aadAppsecret01 = New-AzureADApplicationPasswordCredential -ObjectId $objectId -CustomKeyIdentifier "SecretKey" -StartDate $startDate -EndDate $endDate

$aadAppsecret01