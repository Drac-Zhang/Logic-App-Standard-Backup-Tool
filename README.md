# LAAutoBackup
## Usage
For Logic App Standard, all the workflow definitions are stored in Azure Storage Table. 

This application is used for reading all the definitions from the Azure Storage Table and save them in a target Azure Blob Container.

## Prerequisite
1. Create an Azure Function to host the application.
2. The Azure Storage Account can be protected via Private Endpoint or firewall, make sure that the Azure Function can access the source/target Azure Storage.

## Configuration for Azure Function


## Limitation
1. The workflow definitions will be deleted after 90 days if it is not the current version. So this application can only backup for the definitions within 90 days.
2. The application recently using Http trigger, so it need to be called by other applications/services. If you would like to only use Function App, you need to modify the code to switch to Schedule trigger.
