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
using TLinkAddress = System.Byte;
using System.IO;
using System.Collections.Generic;
using Platform.Collections.Lists;

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

        private UnitedMemoryLinks<byte> Links { get; set; }

        private readonly TLinkAddress _unicodeSymbolMarker;

        private readonly string DBFilename;

        private TLinkAddress GetOrCreateNextMapping(TLinkAddress currentMappingIndex) => Links.Exists(currentMappingIndex) ? currentMappingIndex : Links.CreateAndUpdate(_meaningRoot, Links.Constants.Itself);
       
        private TLinkAddress GetOrCreateMeaningRoot(TLinkAddress meaningRootIndex) => Links.Exists(meaningRootIndex) ? meaningRootIndex : Links.CreatePoint();

        public DBContext(string DBFilename)
        {
            if (!File.Exists(DBFilename))
            {
                using var links = new UnitedMemoryLinks<byte>(DBFilename);
                Links = links;
                var link = links.Create();
                link = links.Update(link, newSource: link, newTarget: link);
            }
            else
            {
                using var links = new UnitedMemoryLinks<byte>(DBFilename);
                Links = links;
            }
            this.DBFilename = DBFilename;
            byte currentMappingLinkIndex = 1;
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
                var query = new Link<byte>(index: any, source: nameLink, target: any);
                var list = new List<IList<TLinkAddress>>();
                var listFiller = new ListFiller<IList<TLinkAddress>, TLinkAddress>(list, Links.Constants.Continue);
                Links.Each(listFiller.AddAndReturnConstant);
                return _unicodeSequenceToStringConverter.Convert(list.First()[Links.Constants.TargetPart]);
            }
            else
            {
                throw new FileNotFoundException();
            }
        }

        public string GetOrLoadFile(string PathToFile, string Addres)
        {

            var nameLink = _stringToUnicodeSequenceConverter.Convert(Addres);
            var any = Links.Constants.Any;
            var query = new Link<byte>(index: any, source: nameLink, target: any);
            var list = new List<IList<TLinkAddress>>();
            var listFiller = new ListFiller<IList<TLinkAddress>, TLinkAddress>(list, Links.Constants.Continue);
            Links.Each(listFiller.AddAndReturnConstant);
            if (list != null)
            {
                return _unicodeSequenceToStringConverter.Convert(list.First()[Links.Constants.TargetPart]);
            }
            else
            {
                var content = LoadСontent(PathToFile);
                AddFile(Addres, content);
                return content;
            }
        }

        private string LoadСontent(string PathToFile)
        {
            using StreamReader sr = new StreamReader(PathToFile);
            return sr.ReadToEnd();
        }

        public TLinkAddress AddFile(string name, string content)
        {
            var source = _stringToUnicodeSequenceConverter.Convert(name);
            var target = _stringToUnicodeSequenceConverter.Convert(LoadСontent(content));
            return Links.GetOrCreate(source, target);
        }

        public void Delete(TLinkAddress link) => Links.Delete(link);

    }
}
