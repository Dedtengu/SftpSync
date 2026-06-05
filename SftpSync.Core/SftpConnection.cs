using System;

namespace SftpSync.Core
{
    public class SftpConnection
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 22;
        public string Username { get; set; } = string.Empty;
        public string EncryptedPassword { get; set; } = string.Empty;
        public string RemoteRootPath { get; set; } = "/";
        public string ServerFingerprint { get; set; } = string.Empty;
    }
}