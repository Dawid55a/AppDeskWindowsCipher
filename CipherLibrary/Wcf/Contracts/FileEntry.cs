using System.Runtime.Serialization;

namespace CipherLibrary.Wcf.Contracts
{
    [DataContract]
    public class FileEntry
    {
        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public bool IsEncrypted { get; set; }

        [DataMember]
        public bool IsDecrypted { get; set; }

        [DataMember]
        public bool ToBeEncrypted { get; set; }

        [DataMember]
        public bool ToBeDecrypted { get; set; }
    }
}