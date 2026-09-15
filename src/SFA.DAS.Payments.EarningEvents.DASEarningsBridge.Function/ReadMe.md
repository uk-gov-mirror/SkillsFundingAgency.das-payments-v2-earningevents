# Payments V2 Earnings Bridge

The Payments V2 Earnings Bridge function is responsible for caching and processing Growth and Skills Earning Events.

## How it works

The Azure Function listens on a queue in the DAS service bus namespace, stores the earnings messages to a local SQL cache, and as appropriate, will propogate messages out to the relevant Payments V2 applications.

## Installation

### Pre-Requisites

Visual Studio 2022 or above

SQL Server

Azure Service Bus queues configured for Payments V2 as per the instructions here: https://skillsfundingagency.atlassian.net/wiki/spaces/NDL/pages/4948754681/DAS+Payments+-+Developer+Onboarding+2025

### Config

For local running, create a file called 'local.settings.json'

Populate as follows:

```
{
  "IsEncrypted": false,
  "Values": {
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
        "AzureWebJobsStorage": "UseDevelopmentStorage=true",
        "AzureWebJobs.HttpExample.Disabled": "true",        
        "DASServiceBusConnectionString": "<< connection string for DAS service bus namespace >>",
        "DASServiceBusQueueName": "<< name of queue to listen on in DAS service bus namespace >>",
        "ReprocessPendingEarningsSchedule": "<< CRON expression for the ReprocessPendingEarnings timer trigger, e.g. 0 30 1 * * * >>"
  },
   "Host": {
        "LocalHttpPort": 7071,
        "CORS": "*",
        "CORSCredentials": false
    },
    "PaymentsConnectionString": "<< SQL connection string for your local DASPayments database instance >>",
    "ServiceBusConnectionString": "<< Payments service bus connection string for your local instance - will be prefixed Endpoint=sb://das-pv2-dev- followed by initials >>",
    "CollectionPeriodApiBaseAddress": "<< base address of Collection Period API when running locally (look in the das-payments-v2-collectionperiod repo for this) >>",    
    "CollectionPeriodApiKey": "<< add dummy key for local testing >>"
}
```
