// <copyright file="RsaHelpers.cs" company="3M">
// Copyright (c) 3M. All rights reserved.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Org.BouncyCastle.Crypto; // Because why wouldnt you use a bouncy castle??? #NeverTooOld
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Security;

namespace Mmm.Iot.IdentityGateway.Services.Helpers
{
    public class RsaHelpers : IRsaHelpers
    {
        private readonly ILogger logger;

        public RsaHelpers(ILogger<RsaHelpers> logger)
        {
            this.logger = logger;
        }

        public RSA DecodeRsa(string privateRsaKey)
        {
            RSAParameters rsaParams;
            using (var tr = new StringReader(privateRsaKey.Replace("\\n", "\n")))
            {
                var pemReader = new PemReader(tr);
                var keyPair = pemReader.ReadObject() as AsymmetricCipherKeyPair;
                if (keyPair == null)
                {
                    string errorMessage = "Could not read RSA private key";
                    Exception exception = new Exception(errorMessage);
                    this.logger.LogError(exception, errorMessage);
                    throw exception;
                }

                var privateRsaParams = keyPair.Private as RsaPrivateCrtKeyParameters;
                rsaParams = DotNetUtilities.ToRSAParameters(privateRsaParams);
            }

            RSA rsa = RSA.Create();
            rsa.ImportParameters(rsaParams);
            return rsa;
        }

        public JsonWebKeySet GetJsonWebKey(string key)
        {
            var publicKey = key.Replace("\\n", "\n");

            JsonWebKeySet jsonWebKeySet = new JsonWebKeySet();
            using (var textReader = new StringReader(publicKey))
            {
                var pubkeyReader = new PemReader(textReader);
                RsaKeyParameters keyParameters = (RsaKeyParameters)pubkeyReader.ReadObject();
                var e = Base64UrlEncoder.Encode(keyParameters.Exponent.ToByteArrayUnsigned());
                var n = Base64UrlEncoder.Encode(keyParameters.Modulus.ToByteArrayUnsigned());
                var dict = new Dictionary<string, string>()
                {
                    { "e", e },
                    { "kty", "RSA" },
                    { "use", "sig" },
                    { "n", n },
                };
                var hash = SHA256.Create();
                var hashBytes = hash.ComputeHash(System.Text.Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(dict)));
                JsonWebKey jsonWebKey = new JsonWebKey()
                {
                    Kid = Base64UrlEncoder.Encode(hashBytes),
                    Kty = "RSA",
                    Use = "sig",
                    E = e,
                    N = n,
                };
                jsonWebKeySet.Keys.Add(jsonWebKey);
            }

            return jsonWebKeySet;
        }
    }
}