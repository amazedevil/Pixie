﻿using DryIoc;
using Pixie.Core.Exceptions;
using Pixie.Core.Services;
using Pixie.Core.StreamWrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace Pixie.Toolbox.StreamWrappers
{
    public class PXSSLStreamWrapper : IPXStreamWrapper
    {
        public const string ENV_PARAM_CERTIFICATE_PATH = "PX_SSL_CERT_PATH";

        private string certificatePath = null;

        public PXSSLStreamWrapper(string certificatePath) {
            this.certificatePath = certificatePath;
        }

        public Stream Wrap(Stream stream) {
            var result = new SslStream( stream, false );

            var serverCertificate = X509Certificate.CreateFromCertFile(this.certificatePath);

            result.AuthenticateAsServer(
                serverCertificate, 
                clientCertificateRequired: false, 
                enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls12, 
                checkCertificateRevocation: true
            );

            return result;
        }
    }
}
