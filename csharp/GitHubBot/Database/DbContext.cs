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
using TLinkAddress = System.UInt64;
using System.IO;
using System.Collections.Generic;
using Platform.Collections.Lists;
using System;

namespace Database
{
    class DBContext
    {
        private readonly TLinkAddress _unicodeSequenceMarker;

        private readonly TLinkAddress _meaningRoot;
        
        private readonly AddressToRawNumberConverter<TLinkAddress> _addressToNumberConverter;
        
        private readonly RawNumberToAddressConverter<TLinkAddress> _numberToAddressConverter;
        
        private readonly IConverter<string, TLinkAddress> _stringToUnicodeSequenceConverter;
        
        private readonly IConverter<TLinkAddress, string> _unicodeSequenceToStringConverter;

        private readonly UnitedMemoryLinks<UInt64> Links;

        private readonly TLinkAddress _unicodeSymbolMarker;

        private readonly string DBFilename;

        private TLinkAddress GetOrCreateNextMapping(TLinkAddress currentMappingIndex) => Links.Exists(currentMappingIndex) ? currentMappingIndex : Links.CreateAndUpdate(_meaningRoot, Links.Constants.Itself);
       
        private TLinkAddress GetOrCreateMeaningRoot(TLinkAddress meaningRootIndex) => Links.Exists(meaningRootIndex) ? meaningRootIndex : Links.CreatePoint();

        public DBContext(string DBFilename)
        {
            Links = new UnitedMemoryLinks<UInt64>(DBFilename);
            var link = Links.Create();
            link = Links.Update(link, newSource: link, newTarget: link);
            this.DBFilename = DBFilename;
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

        public string GetFile(string Addres)
        {
            if (Links.Exists(_stringToUnicodeSequenceConverter.Convert(Addres)))
            {
                var nameLink = _stringToUnicodeSequenceConverter.Convert(Addres);
                var any = Links.Constants.Any;
                var query = new Link<UInt64>(index: any, source: nameLink, target: any);
                var list = new List<IList<TLinkAddress>>();
                var listFiller = new ListFiller<IList<TLinkAddress>, TLinkAddress>(list, Links.Constants.Continue);
                Links.Each(listFiller.AddAndReturnConstant,query);
                return _unicodeSequenceToStringConverter.Convert(list.First()[Links.Constants.TargetPart]);
            }
            else
            {
                throw new FileNotFoundException();
            }
        }
    }
}
