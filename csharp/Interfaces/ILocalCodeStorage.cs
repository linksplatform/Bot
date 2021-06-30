using TLinkAddress = System.UInt64;

namespace Interfaces
{
    public interface ILocalCodeStorage
    {
        public string PutFile(string addres);

        public TLinkAddress AddFile(string name, string content);

        public void Delete(TLinkAddress link);

        public string AllLinksToString();

        public bool LinkExist(string addres);
    }
}

