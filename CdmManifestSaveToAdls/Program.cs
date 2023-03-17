using CdmManifestSaveToAdls.Auth;
using Microsoft.CommonDataModel.ObjectModel.Cdm;
using Microsoft.CommonDataModel.ObjectModel.Enums;
using Microsoft.CommonDataModel.ObjectModel.Storage;
using Microsoft.CommonDataModel.ObjectModel.Utilities;
using System.Configuration;
using System.Reflection;

namespace CdmManifestSaveToAdls
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var version = "V1";
            var date = DateTime.Now.ToString("yyyy-MM-dd");
            var parquetFileName = Guid.NewGuid();
            var partnerId = "Customer";

            Console.WriteLine("Hello, World!");

            var storageAccount = ConfigurationManager.AppSettings["storageAccount"];
            var container = ConfigurationManager.AppSettings["container"] + "/erik";

            var cdmTokenProvider = new CdmTokenProvider();

            var cdmCorpus = new CdmCorpusDefinition();

            cdmCorpus.SetEventCallback(new EventCallback
            {
                Invoke = (CdmStatusLevel statusLevel, string message) =>
                {
                    Console.WriteLine($"{statusLevel}: {message}");
                    // ... or send the log to your own logging component
                }
            },
            CdmStatusLevel.Progress);

            var binDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var rootDirectory = Path.GetFullPath(Path.Combine(binDirectory, ".."));

            var tmpPath = Path.Combine(Path.GetTempPath(), version);
            var cdmDirectory = Path.Combine(tmpPath, "CdmModel");

            DirectoryCopy(Path.Combine(rootDirectory, version), tmpPath, true);

            cdmCorpus.Storage.Mount("dataflow", new LocalAdapter(cdmDirectory));
            cdmCorpus.Storage.DefaultNamespace = "dataflow";

            CdmManifestDefinition manifest = await cdmCorpus.FetchObjectAsync<CdmManifestDefinition>($"{partnerId}.manifest.cdm.json");

            var done = true;
            foreach (var e in manifest.Entities)
            {
                var entityName = e.EntityName;

                cdmCorpus.Storage.Mount("adls", new ADLSAdapter(
                    storageAccount,
                    $"container/{entityName}",
                    cdmTokenProvider
                ));

                CdmManifestDefinition manifestAbstract = cdmCorpus.MakeObject<CdmManifestDefinition>(CdmObjectType.ManifestDef, $"tempAbstract-{entityName}");
                manifestAbstract.Entities.Add(entityName, $"dataflow:/{entityName}.cdm.json/{entityName}");

                var dataflowRoot = cdmCorpus.Storage.FetchRootFolder("dataflow");
                dataflowRoot.Documents.Add(manifestAbstract);

                CopyOptions abstractCopyOptions = new CopyOptions() { SaveConfigFile = false };
                _ = await manifestAbstract.SaveAsAsync(Path.Combine(cdmDirectory, $"tempAbstract-{entityName}.manifest.cdm.json"), true, abstractCopyOptions);

                CdmLocalEntityDeclarationDefinition assetDeclaration = await cdmCorpus.FetchObjectAsync<CdmLocalEntityDeclarationDefinition>($"tempAbstract-{entityName}.manifest.cdm.json/{entityName}");

                var ppd = assetDeclaration.DataPartitionPatterns.Add("NewPartition0");
                ppd.RootLocation = $"/{entityName}/{date}/";
                ppd.GlobPattern = $"{entityName}-{parquetFileName}*.parquet";

                var localRoot = cdmCorpus.Storage.FetchRootFolder("adls");
                localRoot.Documents.Add(manifestAbstract);

                var manifestResolved = await manifestAbstract.CreateResolvedManifestAsync("Latest", "{f}/{n}.cdm.json");

                CopyOptions copyOptions = new CopyOptions() { SaveConfigFile = true };
                done = done && await manifestResolved.SaveAsAsync("Latest.manifest.cdm.json", true, copyOptions);

                cdmCorpus.Storage.Unmount("adls");
            }

            Console.WriteLine(done);
            Console.ReadLine();
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, true);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }
}