using Microsoft.CommonDataModel.ObjectModel.Cdm;
using Microsoft.CommonDataModel.ObjectModel.Storage;
using Microsoft.CommonDataModel.ObjectModel.Utilities;
using System.Configuration;

namespace CdmManifestSaveToAdls
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            
            var storageAccount = ConfigurationManager.AppSettings["storageAccount"];
            var container = ConfigurationManager.AppSettings["container"];
            var storageKey = ConfigurationManager.AppSettings["storageKey"];

            var cdmCorpus = new CdmCorpusDefinition();
            cdmCorpus.Storage.Mount("cdm", new LocalAdapter("CdmModel"));
            cdmCorpus.Storage.DefaultNamespace = "cdm";
            //cdmCorpus.Storage.Mount("dataflow", new CdmStandardsAdapter());
            cdmCorpus.Storage.Mount("dataflow", new ADLSAdapter(
                storageAccount,
                container,
                storageKey
                ));
            cdmCorpus.SetEventCallback(new EventCallback
            {
                Invoke = (CdmStatusLevel statusLevel, string message) =>
                {
                    Console.WriteLine($"{statusLevel}: {message}");
                    // ... or send the log to your own logging component
                }
            },
            CdmStatusLevel.Progress);

            var readAs = "Manifest";
            var done = false;
            if (readAs == "Manifest")
            {
                CdmManifestDefinition manifest = await cdmCorpus.FetchObjectAsync<CdmManifestDefinition>("Customer.manifest.cdm.json");

                CdmFolderDefinition adlsFolder = cdmCorpus.Storage.FetchRootFolder("dataflow");
                adlsFolder.Documents.Add(manifest);
                var resolved = await manifest.CreateResolvedManifestAsync("dataflow", "{f}dataflow/{n}/{n}.cdm.json");

                // try 1:
                //var ppd = new CdmDataPartitionPatternDefinition(manifest.Ctx, "NewPartition0");
                // try 2:
                //var ppd = new CdmDataPartitionPatternDefinition(resolved.Ctx, "NewPartition0");
                // try 3:
                var ppd = new CdmDataPartitionPatternDefinition(resolved.Entities[0].Ctx, "NewPartition0");
                ppd.RootLocation = "/Asset/2023-02-14/";
                ppd.GlobPattern = "Asset-7c5ea9a5355d4b43b7c0765d8442ff1f*.parquet";

                //done = await resolved.Entities[0].InDocument.SaveAsAsync("Asset.cdm.json", true); // <- see if we can just save the Entity, but it saves more (and doesnt save config and *.manifest.cdm.json for the asset)
                done = await resolved.SaveAsAsync("NewCustomer.cdm.json", true);
            }
            else if (readAs == "CdmEntityDefinition")
            {
                // trying to read the Entity directly through CdmDocumentDefinition

                var entity = await cdmCorpus.FetchObjectAsync<CdmEntityDefinition>("Asset.cdm.json"); // <- this results in a NullRef on the next statement

                var resolved = await entity.CreateResolvedEntityAsync("Asset");

                //CdmFolderDefinition adlsFolder = cdmCorpus.Storage.FetchRootFolder("dataflow"); // <- also no clue on how to configure the Storage in this scenario
                //adlsFolder.Documents.Add(entity);
            }
            else if (readAs == "CdmDocumentDefinition")
            {
                // trying to read the Entity directly through CdmDocumentDefinition

                var documentDefinition = await cdmCorpus.FetchObjectAsync<CdmDocumentDefinition>("Asset.cdm.json");

                //var resolved = await documentDefinition.CreateResolvedEntityAsync("Asset"); // <- documentDefinition does not have a 'resolve' method
            }

            Console.WriteLine(done);
            Console.ReadLine();
        }
    }
}