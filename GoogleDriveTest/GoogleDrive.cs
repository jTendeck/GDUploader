using System;
using System.Collections.Generic;
using System.IO;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Upload;
using Google.Apis.Util.Store;
using static Google.Apis.Drive.v3.DriveService;


namespace GoogleDriveTest
{
    public class GoogleDrive
    {
        private DriveService _ds;

        public string AccessTok { get; set; }
        public string RefreshTok { get; set; }
        public string AppName { get; set; }
        public string User { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }

        public GoogleDrive(
            string refreshTok,
            string appName,
            string user,
            string clientId,
            string clientSecret,
            string accessTok = null
        )
        {
            AccessTok = accessTok;
            RefreshTok = refreshTok;
            AppName = appName;
            User = user;
            ClientId = clientId;
            ClientSecret = clientSecret;

            _ds = InitializeDriveService();
        }

        private DriveService InitializeDriveService()
        {
            TokenResponse tokenResponse = new TokenResponse
            {
                AccessToken = AccessTok,
                RefreshToken = RefreshTok
            };

            GoogleAuthorizationCodeFlow apiCodeFlow = new GoogleAuthorizationCodeFlow(
                new GoogleAuthorizationCodeFlow.Initializer
                {
                    ClientSecrets = new ClientSecrets
                    {
                        ClientId = ClientId,
                        ClientSecret = ClientSecret
                    },
                    Scopes = new[] { Scope.Drive },
                    DataStore = new FileDataStore(AppName)
                }
            );

            UserCredential credential = new UserCredential(apiCodeFlow, User, tokenResponse);
            return new DriveService(
                new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = AppName
                }
            );
        }

        public string CreateFolder(string folderName, string parent)
        {
            Google.Apis.Drive.v3.Data.File driveFolder = new Google.Apis.Drive.v3.Data.File();
            driveFolder.Name = folderName;
            driveFolder.MimeType = "application/vnd.google-apps.folder";
            driveFolder.Parents = new string[] { parent };
            FilesResource.CreateRequest cmd = _ds.Files.Create(driveFolder);
            Google.Apis.Drive.v3.Data.File file = cmd.Execute();
            return file.Id;
        }

        public string CreateFile(
            Stream fileStream,
            string fileName,
            string fileMime = null,
            string folder = null,
            string description = null
        )
        {
            Google.Apis.Drive.v3.Data.File driveFile = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Description = description,
                MimeType = fileMime,
                Parents = new List<string> { folder }
            };

            FilesResource.CreateMediaUpload req = _ds.Files.Create(driveFile, fileStream, fileMime);
            req.Fields = "id";

            IUploadProgress res = req.Upload();
            if (res.Status != UploadStatus.Completed)
                throw res.Exception;

            return req.ResponseBody.Id;
        }

        public string UpsertFile(
            string localFilePath,
            string fileName,
            string folder = null,
            string description = null,
            string fileMime = null
        )
        {
            string gdFileId = GetFileId(fileName);
            string gdFolderId = null;
            
            if (folder != null)
                gdFolderId = UpsertFolder(folder);
            
            string returnId;
            using (FileStream fs = File.OpenRead(localFilePath + fileName))
            {
                if (gdFileId == null)
                {
                    returnId = CreateFile(fs, fileName, fileMime, gdFolderId, description);
                }
                else
                {
                    returnId = UpdateFile(
                        fs,
                        gdFileId,
                        fileName,
                        fileMime,
                        gdFolderId,
                        description
                    );
                }
            }
            return returnId;
        }

        public string UpsertFolder(string folder, string parent = null)
        {
            string gdFolderId = GetFolderId(folder);

            if (gdFolderId == null)
            {
                gdFolderId = CreateFolder(folder, parent);
            }

            return gdFolderId;
        }

        public string UpdateFile(
            Stream fileStream,
            string fileId,
            string fileName = null,
            string fileMime = null,
            string folder = null,
            string description = null
        )
        {
            Google.Apis.Drive.v3.Data.File driveFile = new Google.Apis.Drive.v3.Data.File
            {
                Name = fileName,
                Description = description,
                MimeType = fileMime
            };

            FilesResource.UpdateMediaUpload req = _ds.Files.Update(
                driveFile,
                fileId,
                fileStream,
                fileMime
            );

            req.AddParents = folder;

            IUploadProgress res = req.Upload();

            if (res.Status != UploadStatus.Completed)
                throw res.Exception;

            return req.ResponseBody.Id;
        }

        public string GetFileId(string fileName, string folder = null)
        {
            List<Google.Apis.Drive.v3.Data.File> existingFiles = GetFiles(fileName, folder);

            if (existingFiles.Count > 1)
                throw new Exception(
                    $"More than one element with filename `{fileName}` was found. Unable to return fileID"
                );

            return existingFiles.Count >= 1 ? existingFiles[0].Id : null;
        }

        public string GetFolderId(string folderName)
        {
            List<Google.Apis.Drive.v3.Data.File> existingFolders = ExcecuteFileSearchQuery(
                $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}'"
            );

            if (existingFolders.Count > 1)
                throw new Exception(
                    $"More than one element with foldername `{folderName}` was found. Unable to return fileID"
                );

            return existingFolders.Count >= 1 ? existingFolders[0].Id : null;
        }

        public List<Google.Apis.Drive.v3.Data.File> GetFiles(string fileName, string folder = null)
        {
            string folderParam = folder == null ? "" : $" and '{folder}' in parents";
            string query =
                $"mimeType!='application/vnd.google-apps.folder' and name = '{fileName}'{folderParam}";

            return ExcecuteFileSearchQuery(query);
        }

        public List<Google.Apis.Drive.v3.Data.File> GetAllFiles()
        {
            return ExcecuteFileSearchQuery(null);
        }

        public List<Google.Apis.Drive.v3.Data.File> GetFolders(string folderName)
        {
            string query =
                $"mimeType = 'application/vnd.google-apps.folder' and name = '{folderName}'";

            return ExcecuteFileSearchQuery(query);
        }

        public List<Google.Apis.Drive.v3.Data.File> ExcecuteFileSearchQuery(string qString)
        {
            FilesResource.ListRequest fileList = _ds.Files.List();
            fileList.Q = qString;

            List<Google.Apis.Drive.v3.Data.File> result =
                new List<Google.Apis.Drive.v3.Data.File>();
            string pageToken = null;
            do
            {
                fileList.PageToken = pageToken;
                Google.Apis.Drive.v3.Data.FileList filesResult = fileList.Execute();
                IList<Google.Apis.Drive.v3.Data.File> files = filesResult.Files;
                pageToken = filesResult.NextPageToken;
                result.AddRange(files);
            } while (pageToken != null);

            return result;
        }

        public string DeleteFile(string fileId)
        {
            FilesResource.DeleteRequest cmd = _ds.Files.Delete(fileId);
            return cmd.Execute();
        }

        public List<string> DeleteAllFiles(List<Google.Apis.Drive.v3.Data.File> files)
        {
            List<string> deletedIds = new List<string>();
            foreach (Google.Apis.Drive.v3.Data.File f in files)
            {
                deletedIds.Add(DeleteFile(f.Id));
            }
            return deletedIds;
        }
    }
}
