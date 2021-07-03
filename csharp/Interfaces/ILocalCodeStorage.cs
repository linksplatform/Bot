using TLinkAddress = System.UInt64;

namespace Interfaces
{
    public interface ILocalCodeStorage
    {
        public void Delete(TLinkAddress link);

        public string AllLinksToString();
    }
}

