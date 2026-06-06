using System;

namespace SftpSync.Core
{
    public class SyncRule
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        public string LocalFolder { get; set; } = string.Empty;
        public string RemoteFolder { get; set; } = string.Empty;
        public Guid ConnectionId { get; set; }
        public string FileFilter { get; set; } = "*.*";
        public bool IsEnabled { get; set; } = true;
        public bool ArchiveAfterSend { get; set; } = true;
        public string ArchiveFolder { get; set; } = string.Empty;
        public bool IsAutoStart { get; set; }
    }
}