# LAAutoBackup
## Usage
For Logic App Standard, all the workflow definitions are stored in Azure Storage Table. 

This application is used for reading all the definitions from the Azure Storage Table and save them in a target Azure Blob Container.

## Prerequisite
1. Create an Azure Function to host the application.
2. The Azure Storage Account can be protected via Private Endpoint or firewall, make sure that the Azure Function can access the source/target Azure Storage.

## Configuration for Azure Function
We need to add 3 application settings in Azure Function configuration

### LogicAppsToBackup

All the Logic App Standard need to be backup

**Sample**
```json
[
	{
		"LogicAppName": "Logic App Name 1",
		"ConnectionString": "ConnectionString 1"
	},
	{
		"LogicAppName": "Logic App Name N",
		"ConnectionString": "ConnectionString N"
	}
]
```


### TargetBlobConnectionString
The **connection string** of the Storage Account which saving the backup files.


### ContainerName
The container name which we would like to save the backup file. 

Blob Container only allows lower case and numberic character in container name, so the application will convert all the upper case to lower.


### Sample Configuration
![image](https://user-images.githubusercontent.com/72241569/177250368-7875d590-612a-4504-89fa-00d776486868.png)


## Limitation
1. The workflow definitions will be deleted after 90 days if it is not the current version. So this application can only backup for the definitions within 90 days.
2. The application recently using Http trigger, so it need to be called by other applications/services. If you would like to only use Function App, you need to modify the code to switch to Schedule trigger.
