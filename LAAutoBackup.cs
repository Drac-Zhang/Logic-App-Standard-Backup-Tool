using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Data.Tables;
using Azure.Data.Tables.Models;
using System.IO.Compression;

namespace Company.Function
{
    public class LAAutoBackup
    {
        private static List<BackupInfo> logicAppsToBackup;
        private static string targetBlobConnectionString;
        private static string containerName;
        private static string responseContent;

        //TODO: need more information for success backup response
        private static List<string> backupResult;

        [FunctionName("LAAutoBackup")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                logicAppsToBackup = JsonConvert.DeserializeObject<List<BackupInfo>>(Environment.GetEnvironmentVariable("LogicAppsToBackup"));
                targetBlobConnectionString = Environment.GetEnvironmentVariable("TargetBlobConnectionString");

                //container name is not allowed to use upper case, convert to lower
                containerName = Environment.GetEnvironmentVariable("ContainerName").ToLower();

                BlobContainerClient container = new BlobContainerClient(targetBlobConnectionString, containerName);

                //create the blob container if not exists
                if (!container.Exists())
                {
                    container.Create();
                }

                foreach (BackupInfo bi in logicAppsToBackup)
                {
                    BackupDefinitions(bi.LogicAppName, bi.ConnectionString, container, log);
                }
            }
            catch (Exception ex)
            {
                responseContent = $"{ex.Message}\r\n{ex.StackTrace.ToString()}\r\n";

                while (ex.InnerException != null)
                {
                    ex = ex.InnerException;

                    responseContent += $"========================================\r\n{ex.Message}\r\n{ex.StackTrace.ToString()}\r\n";
                }

                return GenerateResponse(responseContent, 500);
            }

            return GenerateResponse("Backup Succeeded", 200);
        }

        private static ObjectResult GenerateResponse(string content, int statusCode)
        {
            ObjectResult objectResult = new ObjectResult(content);
            objectResult.StatusCode = statusCode;

            return objectResult;
        }

        private static void BackupDefinitions(string logicAppName, string connectionString, BlobContainerClient container, ILogger log)
        {
            log.LogInformation($"Start to backup workflow definitions for Logic App Standard - {logicAppName}");
            string definitionTableName = "flow" + StoragePrefixGenerator.Generate(logicAppName) + "flows";
            log.LogInformation($"Mapped Logic App Standard name: {logicAppName} to Storage Table name: {definitionTableName}");

            TableClient tableClient = new TableClient(connectionString, definitionTableName);

            //Get the recorded last updated timestamp
            string lastUpdatedFilePath = $"{logicAppName}/LastUpdatedAt.txt";
            string lastUpdatedTime = GetBlobContent(container, lastUpdatedFilePath);

            if (string.IsNullOrEmpty(lastUpdatedTime))
            {
                //for initial run
                lastUpdatedTime = "1970-01-01T00:00:00.0000000Z";
                log.LogInformation($"No last backup time entry found, initialize with {lastUpdatedTime}");
            }

            //New definition might be added during the backup process, get the timestamp before we start
            //The backup file name is based on ChangedTime, so will not create duplicate backup files
            string utcNow = System.DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ");

            log.LogInformation($"Retrieving workflow definitions from Azure Storage Table later than {lastUpdatedTime}");
            Pageable<TableEntity> tableEntities = tableClient.Query<TableEntity>(filter: $"ChangedTime ge DateTime'{lastUpdatedTime}'");

            log.LogInformation($"Generating the backup files in Blob Container folder, {container.AccountName}/{logicAppName}");
            foreach (TableEntity entity in tableEntities)
            {
                string rowKey = entity.GetString("RowKey");
                string flowSequenceId = entity.GetString("FlowSequenceId");
                string flowName = entity.GetString("FlowName");
                string modifiedDate = ((DateTimeOffset)entity.GetDateTimeOffset("ChangedTime")).ToString("yyyy_MM_dd_HH_mm_ss");

                string blobPath = $"{logicAppName}/{flowName}/{modifiedDate}_{flowSequenceId}.json";

                //Filter for duplicate definition which is used recently
                if (!rowKey.Contains("FLOWVERSION"))
                {
                    continue;
                }

                byte[] definitionCompressed = entity.GetBinary("DefinitionCompressed");
                string kind = entity.GetString("Kind");
                string decompressedDefinition = DecompressContent(definitionCompressed);

                string outputContent = $"{{\"definition\": {decompressedDefinition},\"kind\": \"{kind}\"}}";

                UploadBlob(container, blobPath, outputContent);
            }

            log.LogInformation($"Updating last backup time");
            //update last update time
            UploadBlob(container, lastUpdatedFilePath, utcNow, true);

            log.LogInformation($"Backup for {logicAppName} succeeded");
        }

        private static string GetBlobContent(BlobContainerClient container, string blobPath)
        {
            BlobClient blob = container.GetBlobClient(blobPath);

            if (!blob.Exists())
            {
                return null;
            }

            BlobDownloadResult blobDownloadResult = blob.DownloadContent();
            string Content = blobDownloadResult.Content.ToString();

            return Content;
        }

        private static void UploadBlob(BlobContainerClient container, string blobPath, string content, bool overwrite = false)
        {
            BlobClient blob = container.GetBlobClient(blobPath);


            if (!blob.Exists() || overwrite)
            {
                using (Stream contentStream = new MemoryStream(Encoding.UTF8.GetBytes(content)))
                {
                    blob.Upload(contentStream, overwrite);
                }
            }
        }

        //The definitions saved in storage table are compressed with Deflate
        //need decompress to origin content
        private static string DecompressContent(byte[] Content)
        {
            string result = String.Empty;

            MemoryStream output = new MemoryStream();

            using (var compressStream = new MemoryStream(Content))
            {
                using (var decompressor = new DeflateStream(compressStream, CompressionMode.Decompress))
                {
                    decompressor.CopyTo(output);
                }
                output.Position = 0;
            }

            using (StreamReader reader = new StreamReader(output))
            {
                result = reader.ReadToEnd();
            }

            return result;
        }
    }
}
