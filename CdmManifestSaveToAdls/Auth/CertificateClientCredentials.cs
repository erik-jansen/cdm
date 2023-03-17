namespace CdmManifestSaveToAdls.Auth
{
    using Azure.Core;
    using Azure.Identity;
    using Azure.Security.KeyVault.Certificates;
    using System;
    using System.Security.Cryptography.X509Certificates;

    public class CertificateClientCredentials
    {
        private readonly TokenCredential tokenCredential;

        public CertificateClientCredentials()
        {
            this.tokenCredential = new DefaultAzureCredential();
        }

        public ClientCertificateCredential Create()
        {
            try
            {
                // get this from whereever
                string akvUri = "";
                string certName = "";
                string tenantId = "";
                string clientId = "";

                var client = new CertificateClient(new Uri(akvUri), this.tokenCredential);
                X509Certificate2 cert = client.DownloadCertificate(certName);
                ClientCertificateCredentialOptions clientCertificateCredentialOptions = new ClientCertificateCredentialOptions();

                // Additionally Allowed Tenants is a list of Tenants for which the ClientCertificateCredential can be used to connect to. Since the client is created per run, and the connection is outgoing into an external tenant, it is fine to set this value as '*' allowing connections to all tenants.
                clientCertificateCredentialOptions.AdditionallyAllowedTenants.Add("*");

                // This is needed to include a x5c header in client claims when acquiring a token to enable subject name / issuer (S+NI) based authentication for the ClientCertificateCredential.
                clientCertificateCredentialOptions.SendCertificateChain = true;

                var clientSecretCredential = new ClientCertificateCredential(tenantId, clientId, cert, clientCertificateCredentialOptions);

                return clientSecretCredential;
            }
            catch(Exception e)
            {
                Console.WriteLine($"Error fetching ClientCertificateCredential with error: '{e.Message}'");
                throw;
            }
        }
    }
}
