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
using Interfaces;

namespace Storage.Local
{
    public class FileStorage : ILocalCodeStorage
    {
        private readonly TLinkAddress _unicodeSequenceMarker;

        private readonly TLinkAddress _meaningRoot;

        private readonly AddressToRawNumberConverter<TLinkAddress> _addressToNumberConverter;

        private readonly RawNumberToAddressConverter<TLinkAddress> _numberToAddressConverter;

        private readonly IConverter<string, TLinkAddress> _stringToUnicodeSequenceConverter;

        private readonly IConverter<TLinkAddress, string> _unicodeSequenceToStringConverter;

        private readonly UnitedMemoryLinks<UInt64> Links;

        private readonly TLinkAddress _unicodeSymbolMarker;

        private readonly TLinkAddress _setMarker;

        private readonly TLinkAddress _fileMarker;

        private TLinkAddress GetOrCreateNextMapping(TLinkAddress currentMappingIndex) => Links.Exists(currentMappingIndex) ? currentMappingIndex : Links.CreateAndUpdate(_meaningRoot, Links.Constants.Itself);

        private TLinkAddress GetOrCreateMeaningRoot(TLinkAddress meaningRootIndex) => Links.Exists(meaningRootIndex) ? meaningRootIndex : Links.CreatePoint();

        public FileStorage(string DBFilename)
        {
            var linksConstants = new LinksConstants<TLinkAddress>(enableExternalReferencesSupport: true);
            var dataMemory = new FileMappedResizableDirectMemory(DBFilename);
            Links = new UnitedMemoryLinks<UInt64>(dataMemory, UnitedMemoryLinks<UInt64>.DefaultLinksSizeStep, linksConstants, IndexTreeType.Default);
            var link = Links.Create();
            link = Links.Update(link, newSource: link, newTarget: link);
            ushort currentMappingLinkIndex = 1;
            _meaningRoot = GetOrCreateMeaningRoot(currentMappingLinkIndex++);
            _unicodeSymbolMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _setMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _fileMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
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

        public TLinkAddress Convert(string str)
        {
            return _stringToUnicodeSequenceConverter.Convert(str);
        }

        public string Convert(TLinkAddress address)
        {
            return _unicodeSequenceToStringConverter.Convert(address);
        }

        public TLinkAddress GetFileLink(string name)
        {
            var nameLink = _stringToUnicodeSequenceConverter.Convert(name);
            var any = Links.Constants.Any;
            var query = new Link<UInt64>(index: any, source: nameLink, target: any);
            var list = new List<IList<TLinkAddress>>();
            var listFiller = new ListFiller<IList<TLinkAddress>, TLinkAddress>(list, Links.Constants.Continue);
            Links.Each(listFiller.AddAndReturnConstant, query);
            return Links.GetTarget(list.First());
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
            return Links.GetOrCreate(_fileMarker, _stringToUnicodeSequenceConverter.Convert(content));
        }


        public void Delete(TLinkAddress link) => Links.Delete(link);

        public string AllLinksToString()
        {
            var any = Links.Constants.Any;
            StringBuilder builder = new();
            var query = new Link<UInt64>(index: any, source: any, target: any);
            Links.Each((link) =>
            {
                builder.Append(Links.Format(link) + "\n");
                return Links.Constants.Continue;
            }, query);
            return builder.ToString();
        }

        public TLinkAddress CreateFileSet(string fileSetName)
        {
            return Links.GetOrCreate(_setMarker, _stringToUnicodeSequenceConverter.Convert(fileSetName));
        }

        public TLinkAddress AddFileToSet(TLinkAddress set,TLinkAddress file, string path)
        {
            return Links.GetOrCreate(set, Links.GetOrCreate(_stringToUnicodeSequenceConverter.Convert(path), file));
        }

        public List<IFile> GetFilesFromSet(string set)
        {
            var files = new List<IFile>();
            var any = Links.Constants.Any;
            var SetAddres = (_stringToUnicodeSequenceConverter.Convert(set));
            SetAddres++;
            var query = new Link<UInt64>(index: any, source: SetAddres, target: any);
            var list = new List<IList<TLinkAddress>>();
            var listFiller = new ListFiller<IList<TLinkAddress>, TLinkAddress>(list, Links.Constants.Continue);
            Links.Each(listFiller.AddAndReturnConstant, query); 
            foreach(var file in list)
            {
                var tmpList = new List<IList<TLinkAddress>>();
                Links.Each(new ListFiller<IList<TLinkAddress>, TLinkAddress>(tmpList, Links.Constants.Continue).AddAndReturnConstant, new Link<UInt64>(index: any, source: any, target: Links.GetTarget(Links.GetTarget(file.First()))));
                files.Add(new File()
                {
                    Path = _unicodeSequenceToStringConverter.Convert(Links.GetSource(tmpList.First().First())),
                    Content = _unicodeSequenceToStringConverter.Convert(Links.GetTarget(Links.GetTarget(file.First())))
                });
            }
            return files;
        }

        public bool LinkExist(string addres)
        {
            return GetFileLink(addres) != 0;
        }
    }
}
