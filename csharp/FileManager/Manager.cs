using System.Linq;
using Platform.Collections.Stacks;
using Platform.Converters;
using Platform.Data;
using Platform.Data.Numbers.Raw;
using Platform.Data.Doublets;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Doublets.Sequences.Walkers;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.CriterionMatchers;
using Platform.Data.Doublets.Memory.United.Generic;
using System.Collections.Generic;
using Platform.Collections.Lists;
using System;
using Platform.Data.Doublets.Memory;
using Platform.Memory;
using TLinkAddress = System.UInt64;
using System.Text;

namespace FileManager
{
    public class Manager
    {
        private readonly TLinkAddress _unicodeSequenceMarker;

        private readonly TLinkAddress _meaningRoot;

        private readonly AddressToRawNumberConverter<TLinkAddress> _addressToNumberConverter;

        private readonly RawNumberToAddressConverter<TLinkAddress> _numberToAddressConverter;

        private readonly IConverter<string, TLinkAddress> _stringToUnicodeSequenceConverter;

        private readonly IConverter<TLinkAddress, string> _unicodeSequenceToStringConverter;

        private readonly UnitedMemoryLinks<UInt64> Links;

        private readonly TLinkAddress _unicodeSymbolMarker;

        private TLinkAddress GetOrCreateNextMapping(TLinkAddress currentMappingIndex) => Links.Exists(currentMappingIndex) ? currentMappingIndex : Links.CreateAndUpdate(_meaningRoot, Links.Constants.Itself);

        private TLinkAddress GetOrCreateMeaningRoot(TLinkAddress meaningRootIndex) => Links.Exists(meaningRootIndex) ? meaningRootIndex : Links.CreatePoint();

        public Manager(string DBFilename)
        {
            var linksConstants = new LinksConstants<TLinkAddress>(enableExternalReferencesSupport: true);
            var dataMemory = new FileMappedResizableDirectMemory(DBFilename);
            Links = new UnitedMemoryLinks<UInt64>(dataMemory, UnitedMemoryLinks<UInt64>.DefaultLinksSizeStep , linksConstants, IndexTreeType.Default);
            var link = Links.Create();
            link = Links.Update(link, newSource: link, newTarget: link);
            ushort currentMappingLinkIndex = 1;
            _meaningRoot = GetOrCreateMeaningRoot(currentMappingLinkIndex++);
            _unicodeSymbolMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _unicodeSequenceMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _meaningRoot = GetOrCreateMeaningRoot(currentMappingLinkIndex++);
            _addressToNumberConverter = new AddressToRawNumberConverter<TLinkAddress>();
            _numberToAddressConverter = new RawNumberToAddressConverter<TLinkAddress>();
            var balancedVariantConverter = new BalancedVariantConverter<TLinkAddress>(Links);
            var unicodeSymbolCriterionMatcher = new TargetMatcher<TLinkAddress>(Links, _unicodeSymbolMarker);
            var unicodeSequenceCriterionMatcher = new TargetMatcher<TLinkAddress>(Links, _unicodeSequenceMarker);
            var charToUnicodeSymbolConverter = new CharToUnicodeSymbolConverter<TLinkAddress>(Links, _addressToNumberConverter, _unicodeSymbolMarker);
            var unicodeSymbolToCharConverter = new UnicodeSymbolToCharConverter<TLinkAddress>(Links, _numberToAddressConverter, unicodeSymbolCriterionMatcher);
            var sequenceWalker = new RightSequenceWalker<TLinkAddress>(Links, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
            _stringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(new StringToUnicodeSequenceConverter<TLinkAddress>(Links, charToUnicodeSymbolConverter, balancedVariantConverter, _unicodeSequenceMarker));
            _unicodeSequenceToStringConverter = new CachingConverterDecorator<TLinkAddress, string>(new UnicodeSequenceToStringConverter<TLinkAddress>(Links, unicodeSequenceCriterionMatcher, sequenceWalker, unicodeSymbolToCharConverter));
        }

        private TLinkAddress GetFileLink(string name)
        {
            var nameLink = _stringToUnicodeSequenceConverter.Convert(name);
            var any = Links.Constants.Any;
            var query = new Link<UInt64>(index: any, source: nameLink, target: any);
            var list = new List<IList<TLinkAddress>>();
            var listFiller = new ListFiller<IList<TLinkAddress>, TLinkAddress>(list, Links.Constants.Continue);
            Links.Each(listFiller.AddAndReturnConstant, query);
            if (list.Count != 0)
            {
                return Links.GetTarget(list.First());
            }
            else
            {
                return 0;
            }
        }

        public string PutFile(string addres)
        {
            if (GetFileLink(addres) != 0)
            {
                return _unicodeSequenceToStringConverter.Convert(GetFileLink(addres));
            }
            else
            {
                return "File does not exists";
            }
        }

        public TLinkAddress AddFile(string name, string content)
        {
            var source = _stringToUnicodeSequenceConverter.Convert(name);
            var target = _stringToUnicodeSequenceConverter.Convert(content);
            return Links.GetOrCreate(source, target);
        }

        public void Delete(TLinkAddress link) => Links.Delete(link);

        public void Delete(string addres)
        {
            if (GetFileLink(addres) != 0)
            {
                Links.Delete(GetFileLink(addres));
            }
        }

        public string AllLinksToString()
        {
            var any = Links.Constants.Any;
            StringBuilder builder = new StringBuilder();
            var query = new Link<UInt64>(index: any, source: any, target: any);
            Links.Each((link) => {
                builder.Append(Links.Format(link) + "\n");
                return Links.Constants.Continue;
            }, query);
            return builder.ToString();
        }

        public bool LinkExist(string addres)
        {
            return GetFileLink(addres) != 0;
        }
    }
}
