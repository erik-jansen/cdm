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

            CdmManifestDefinition manifest = await cdmCorpus.FetchObjectAsync<CdmManifestDefinition>("Customer.manifest.cdm.json");
            var resolved = await manifest.CreateResolvedManifestAsync("dataflow", "{f}dataflow/{n}.cdm.json");

            CdmFolderDefinition adlsFolder = cdmCorpus.Storage.FetchRootFolder("dataflow");
            adlsFolder.Documents.Add(resolved);

            var retval = await resolved.Entities[0].InDocument.SaveAsAsync("Asset.cdm.json", true);
            //var retval = await resolved.SaveAsAsync("model.json", true);

            Console.WriteLine(retval);
            Console.ReadLine();
        }
    }
}