# parameters for Azure subscription
$publishSettingsFile = "C:\Users\MikePet\Documents\Visual Studio 2012\Projects\ULTIMATE-1-9-2013-credentials.publishsettings"
$subscriptionName = "ULTIMATE"
$currentStorageAccount = "petersen"
$azureModule = "C:\Program Files (x86)\Microsoft SDKs\Windows Azure\PowerShell\Azure\Azure.psd1" 

# parameters for IOT partition
$serviceBusLocation = "North Europe"
$serviceBusNamespace = "mcs-iot-01-test"

# import Azure publish settings file
Import-AzurePublishSettingsFile -PublishSettingsFile $publishSettingsFile -ErrorAction Stop
Set-AzureSubscription -SubscriptionName $subscriptionName -CurrentStorageAccount $currentStorageAccount -ErrorAction Stop

# import Azure PowerShell module
Import-Module $azureModule -ErrorAction Stop

# select Azure subscription
Select-AzureSubscription -SubscriptionName $subscriptionName -ErrorAction Stop

# create Service Bus namespace
New-AzureSBNamespace -Location $serviceBusLocation -Name $serviceBusNamespace -ErrorAction Stop
