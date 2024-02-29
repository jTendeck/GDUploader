using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GoogleDriveTest;
using Newtonsoft.Json.Linq;

namespace GoogleDriveTest
{
    class Program
    {
        static void Main(string[] args)
        {
            string localRootDir = $@"{Directory.GetCurrentDirectory()}\..\..\";
            
            string[] validKeys = { "clientId", "clientSecret", "user", "appName", "refreshTok", "pathToFile", "inputFileName", "googleDriveDestFolder", "permissions" };
            
            Dictionary<string, string> inputParams = new Dictionary<string, string>();
            
            // First check secret file...
            string secretFile = $"{localRootDir}secrets.json";
            if (File.Exists(secretFile))
            {
                inputParams = JObject.Parse(File.ReadAllText(secretFile)).ToObject<Dictionary<string, string>>();
            }
            
            // Then check environment variables...
            IDictionary envVars = Environment.GetEnvironmentVariables();

            foreach (DictionaryEntry dict in envVars)
            {
                string key = dict.Key.ToString();
                string val = dict.Value.ToString();
                if (validKeys.Contains(key))
                    inputParams[key] = val;
            }
            
            // Then look for CL args
            if (args.Length > 0)
            {
                foreach (string arg in args)
                {
                    string[] kvp = arg.Split('=');

                    if (kvp.Length == 2 && validKeys.Contains(kvp[0]))
                        inputParams[kvp[0]] = kvp[1];
                }
            }

            if (inputParams.Count == 0)
            {
                Console.WriteLine("Credentials/arguments not found!");
            }
            
            Console.WriteLine("Keys found:");
            foreach (KeyValuePair<string,string> kvp in inputParams)
            {
                Console.WriteLine(kvp.Key);
            }
            
            string refreshTok = inputParams["refreshTok"];
            string appName = inputParams["appName"];
            string user = inputParams["user"];
            string clientId = inputParams["clientId"];
            string clientSecret = inputParams["clientSecret"];

            GoogleDrive gd = new GoogleDrive(
                refreshTok,
                appName,
                user,
                clientId,
                clientSecret
            );

            string filePath = inputParams.ContainsKey("pathToFile") ? inputParams["pathToFile"] : localRootDir;
            string gdFolder = inputParams.ContainsKey("googleDriveDestFolder") ? inputParams["googleDriveDestFolder"] : null;
            string fileName = inputParams["inputFileName"];

            gd.DeleteAllFiles(gd.GetFiles(fileName));
            gd.UpsertFile(filePath, fileName, gdFolder);

            foreach (string userPerm in inputParams["permissions"].Split(','))
            {
                string[] u = userPerm.Split(':');
                gd.CreatePermission(gd.GetFileId(fileName), u[1], u[2], u[0]);
            }
            

            // string fileName = "cardData.xlsx";
            //gd.DeleteAllFiles(gd.GetFiles(fileName));
            //gd.DeleteAllFiles(gd.GetFiles(testFileName));
            //gd.DeleteAllFiles(gd.GetFolders(gdFolder));

            // gd.UpsertFile(rootDir, fileName, gdFolder);
        }
    }
}
