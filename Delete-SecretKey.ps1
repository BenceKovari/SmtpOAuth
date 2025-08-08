Connect-AzureAD

$appName = "Webportalok-fejlesztese"
$objectId = (Get-AzureADApplication -SearchString $appName)[0].ObjectId

$keyIDs = Get-AzureADApplicationPasswordCredential -ObjectId $objectId

Remove-AzureADApplicationPasswordCredential -ObjectId $objectId -KeyId $keyIDs[0].KeyId