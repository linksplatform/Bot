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
using System.IO;

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

        private readonly TLinkAddress Any;

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
            Any = Links.Constants.Any;
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

        public TLinkAddress Convert(string str) => _stringToUnicodeSequenceConverter.Convert(str);

        public string Convert(TLinkAddress address) => _unicodeSequenceToStringConverter.Convert(address);

        public string GetFileContent(TLinkAddress address)
        {
            var link = Links.GetLink(address);
            if (Links.GetSource(link) == _fileMarker)
            {
                return Convert(Links.GetTarget(link));
            }
            throw new InvalidOperationException("Link is not a file.");
        }

        public void Delete(TLinkAddress link) => Links.Delete(link);

        public List<IFile> GetAllFiles()
        {
            List<IFile> files = new() { };
            for(ulong i = 1; i < Links.Count(); ++i)
            {
                try
                {
                    var content = GetFileContent(i);
                    files.Add(new File { Path = i.ToString(), Content = content });
                }
                catch (Exception e)
                {

                }
            }
            return files;
        }

        public string AllLinksToString()
        {
            StringBuilder builder = new();
            var query = new Link<UInt64>(index: Any, source: Any, target: Any);
            Links.Each(link =>
            {
                builder.AppendLine(Links.Format(link));
                return Links.Constants.Continue;
            }, query);
            return builder.ToString();
        }

        public TLinkAddress AddFile(string content)
        {
            return Links.GetOrCreate(_fileMarker, _stringToUnicodeSequenceConverter.Convert(content));
        }

        public TLinkAddress CreateFileSet(string fileSetName)
        {
            return Links.GetOrCreate(_setMarker, Convert(fileSetName));
        }

        public TLinkAddress AddFileToSet(TLinkAddress set,TLinkAddress file, string path)
        {
            return Links.GetOrCreate(set, Links.GetOrCreate(Convert(path), file));
        }

        public TLinkAddress GetFileSet(string fileSetName)
        {
            return Links.SearchOrDefault(_setMarker, Convert(fileSetName));
        }

        private List<IList<TLinkAddress>> GetFiles(string set)
        {
            var fileSet = GetFileSet(set);
            var list = new List<IList<TLinkAddress>>();
            Links.Each(new ListFiller<IList<TLinkAddress>, TLinkAddress>(list, Links.Constants.Continue).AddAndReturnConstant, new Link<UInt64>(index: Any, source: fileSet, target: Any));
            return list;
        }

        public List<IFile> GetFilesFromSet(string set)
        {
            List<IFile> files = new();
            foreach(var file in GetFiles(set))
            {
                var tmpList = new List<IList<TLinkAddress>>();
                Links.Each(new ListFiller<IList<TLinkAddress>, TLinkAddress>(tmpList, Links.Constants.Continue).AddAndReturnConstant, new Link<UInt64>(index: Any, source: Any, target: Links.GetTarget(Links.GetTarget(file.First()))));
                files.Add(new File()
                {
                    Path = Convert(Links.GetSource(tmpList.First().First())),
                    Content = Convert(Links.GetTarget(Links.GetTarget(file.First())))
                });
            }
            return files;
        }
    }
}
