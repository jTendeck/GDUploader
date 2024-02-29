# Google Drive Uploader

## Usage

### GitHub Actions

1. Add credentials as GitHub Actions Secrets, located: `<repo-url>/settings/secrets/actions`
   1. Accepted credentials can be viewed in the `Input Variables` section.
   2. You must rename the keys to be `SCREAMING_SNAKE_CASE` instead of `camelCase`. For instance, when entering in the `clientId` key, the Action Secret key name should be `CLIENT_ID`.
2. Modify the `.github\dotnet.yml` as necessary
   1. For instance, to change the name of the file you want to upload, or to change the destination folder in Google Drive, change the `inputFileName` and `googleDriveDestFolder` values respectively.
   2. You can also change when the workflow is triggered by modifying the `on` section. For instance, `on.push.branches` specifies the names of the branches that will trigger the workflow after pushing to them.
3. Place the file you want to upload in the project root (typically, the location of the `.sln` file). Ensure the file name matches the `inputFileName` in the workflow file.
4. Trigger the workflow by performing one of the actions in the `on` section. Eg, by pushing to a specified branch.
5. View the status of the Action at `<repo-url>/actions`

### Running the Code

1. You will need to supply a number of input variables before running. These are specified in the `Input Variables` section.
2. These can be supplied by a number of ways, including:
   1. a `secrets.json` file
      1. An `example.secrets.json` file is provided. Copy this file, rename to `secrets.json` and update the values as necessary. `secrets.json` will not be checked into version control.
   2. Environment variables
   3. Command line arguments
      1. Arguments must be added as key value pairs, separated by `=`... eg: `clientId=abc123 clientSecret=verySecret`, etc.
3. Once those have been added, run as usual!

#### Input Variable Order of Precedence

The following is the order of precedence for input variable usage, ordered from highest to lowest:

1. Command line arguments
2. Environment variables
3. `secrets.json` file

For instance, if you have a `clientId` in the `secrets.json` file, but you pass in a `clientId` as a command line argument, the command line argument value will be used instead.

You can mix and match input sources. For instance, you could pass in the `clientId` using the `secrets.json` file, the `clientSecret` as an environment variable, and `refreshTok` as a command line argument. Note that order or precedence will still apply.

## Input Variables

| Key                     | Description                                                           |
|-------------------------|-----------------------------------------------------------------------|
| `clientId`\*            | OAuth 2.0 Client ID created in Google Cloud.                          |
| `clientSecret`\*        | Client Secret associated with the `clientId` created in Google Cloud. |
| `user`\*                | Email for a test user for the Google Cloud application    .           |
| `appName`\*             | Google Cloud project name         .                                   |
| `refreshTok`\*          | Refresh token created in Google Cloud .                               |
| `googleDriveDestFolder` | Folder in Google Drive where the file will be saved.                  |
| `inputFileName`         | Name of the file that will be uploaded to Google Drive.               |
| `pathToFile` | Path to input file.                                                   |

\* Must be saved as a GitHub Actions Secret. Remember to rename keys to `SCREAMING_SNAKE_CASE`!

## Release

- Create a new tag:

```sh
git tag v<tag-name> main
```

- Push:

> [!WARNING]
> For some reason, one of the releases failed. Rerunning the job fixed it.

```sh
git push origin v<tag-name>
```

## Commands to Download Release

First, need asset ID of the release. Send a request to:

```sh
set TAG=v1.0.0
curl -L "https://api.github.com/repos/jtendeck/GDUploader/releases/tags/%TAG%"
```

Find the asset ID for the tag to download. This can be found in `[n].assets[n].id`... or use the assets URL.

Then download using:

```sh
set ASSET_ID=151185595
set OUTPUT_FILE=GoogleDriveTest.zip

curl -H "Accept: application/octet-stream" -L https://api.github.com/repos/jtendeck/GDUploader/releases/assets/%ASSET_ID% -o %OUTPUT_FILE%
```

