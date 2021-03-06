﻿using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Linq;

namespace Covid19Radar.Services
{
    public class CryptionService : ICryptionService
    {
        private readonly ILogger<CryptionService> Logger;
        private readonly SymmetricAlgorithm symmetric;
        private readonly SymmetricAlgorithm symmetric2;
        private readonly HashAlgorithm hash;
        private const int Length = 256;

        public CryptionService(
            Microsoft.Extensions.Configuration.IConfiguration config,
            ILogger<CryptionService> logger)
        {
            this.Logger = logger;
            symmetric = Aes.Create();
            symmetric.Mode = CipherMode.CBC;
            symmetric.Padding = PaddingMode.ISO10126;
            symmetric.KeySize = 256;
            symmetric.Key = Convert.FromBase64String(config.GetSection("CRYPTION_KEY").Value);
            symmetric.IV = Convert.FromBase64String(config.GetSection("CRYPTION_IV").Value);
            hash = new HMACSHA512(Convert.FromBase64String(config.GetSection("CRYPTION_HASH").Value));
            symmetric2 = Aes.Create();
            symmetric2.Mode = CipherMode.CBC;
            symmetric2.Padding = PaddingMode.ISO10126;
            symmetric2.KeySize = 256;
            symmetric2.Key = Convert.FromBase64String(config.GetSection("CRYPTION_KEY2").Value);
            symmetric2.IV = Convert.FromBase64String(config.GetSection("CRYPTION_IV2").Value);
        }


        private byte[] Random()
        {
            byte[] r = new byte[Length];
            RNGCryptoServiceProvider.Fill(r);
            return r;
        }

        public string CreateSecret(string userUuid)
        {
            var val = Random().Concat(Encoding.ASCII.GetBytes(userUuid));
            byte[] hashValue = hash.ComputeHash(val.ToArray());
            var secret = val.Concat(hashValue).ToArray();
            using (var c = symmetric2.CreateEncryptor())
            {
                return Convert.ToBase64String(c.TransformFinalBlock(secret, 0, secret.Length));
            }
        }

        public bool ValidateSecret(string userUuid, string secret)
        {
            var buf = Convert.FromBase64String(secret);
            using (var c = symmetric2.CreateDecryptor())
            {
                var result = c.TransformFinalBlock(buf, 0, buf.Length);
                if (userUuid != Encoding.ASCII.GetString(result, 256, result.Length - 256 - 64))
                {
                    return false;
                }
                byte[] hashValue = hash.ComputeHash(result, 0, result.Length - 64);
                return hashValue.SequenceEqual(result.TakeLast(64));
            }

        }

        public string Protect(string secret)
        {
            var buf = Convert.FromBase64String(secret);
            using (var c = symmetric.CreateEncryptor())
            {
                var result = c.TransformFinalBlock(buf, 0, buf.Length);
                return Convert.ToBase64String(result);
            }
        }

        public string Unprotect(string protectSecret)
        {
            var buf = Convert.FromBase64String(protectSecret);
            using (var c = symmetric.CreateDecryptor())
            {
                var result = c.TransformFinalBlock(buf, 0, buf.Length);
                return Convert.ToBase64String(result);
            }
        }
    }
}
