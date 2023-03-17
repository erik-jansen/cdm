namespace CdmManifestSaveToAdls.Auth
{
    using Microsoft.CommonDataModel.ObjectModel.Utilities.Network;
    using System;

    public class CdmTokenProvider : TokenProvider
    {
        private readonly CertificateClientCredentials certificateClientCredentials;
        private string token;
        private DateTime cacheDate;

        public CdmTokenProvider()
        { 
            this.certificateClientCredentials = new CertificateClientCredentials();
        }

        public string GetToken()
        {
            // Simple caching technique so that the token is only requested once per minute.
            // todo: we might want to implement a lock to make sure that when multiple threads call this singleton, we still only request the token once per minute.
            TimeSpan span = DateTime.Now.Subtract(cacheDate);

            if (span.TotalSeconds > 60)
            {
                cacheDate = DateTime.Now;

                var clientCertificateCredential = this.certificateClientCredentials.Create();

                var token = clientCertificateCredential.GetToken(new Azure.Core.TokenRequestContext(new[] { "https://storage.azure.com/.default" }), default);
                this.token = $"Bearer {token.Token}";
            }

            return this.token;
        }
    }
}
