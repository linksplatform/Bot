using Interfaces;
using Platform.Collections.Stacks;
using Platform.Converters;
using Platform.Data;
using Platform.Data.Doublets;
using Platform.Data.Doublets.CriterionMatchers;
using Platform.Data.Doublets.Memory;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.Sequences.Walkers;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Numbers.Raw;
using Platform.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TLinkAddress = System.UInt64;

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

        private TLinkAddress GetOrCreateNextMapping(TLinkAddress currentMappingIndex)
        {
            return Links.Exists(currentMappingIndex) ? currentMappingIndex : Links.CreateAndUpdate(_meaningRoot, Links.Constants.Itself);
        }

        private TLinkAddress GetOrCreateMeaningRoot(TLinkAddress meaningRootIndex)
        {
            return Links.Exists(meaningRootIndex) ? meaningRootIndex : Links.CreatePoint();
        }

        public FileStorage(string DBFilename)
        {
            LinksConstants<ulong> linksConstants = new LinksConstants<TLinkAddress>(enableExternalReferencesSupport: true);
            FileMappedResizableDirectMemory dataMemory = new FileMappedResizableDirectMemory(DBFilename);
            Links = new UnitedMemoryLinks<UInt64>(dataMemory, UnitedMemoryLinks<UInt64>.DefaultLinksSizeStep, linksConstants, IndexTreeType.Default);
            ulong link = Links.Create();
            link = Links.Update(link, newSource: link, newTarget: link);
            ushort currentMappingLinkIndex = 1;
            Any = Links.Constants.Any;
            _meaningRoot = GetOrCreateMeaningRoot(currentMappingLinkIndex++);
            _unicodeSymbolMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _unicodeSequenceMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _setMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _fileMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _addressToNumberConverter = new AddressToRawNumberConverter<TLinkAddress>();
            _numberToAddressConverter = new RawNumberToAddressConverter<TLinkAddress>();
            BalancedVariantConverter<ulong> balancedVariantConverter = new BalancedVariantConverter<TLinkAddress>(Links);
            TargetMatcher<ulong> unicodeSymbolCriterionMatcher = new TargetMatcher<TLinkAddress>(Links, _unicodeSymbolMarker);
            TargetMatcher<ulong> unicodeSequenceCriterionMatcher = new TargetMatcher<TLinkAddress>(Links, _unicodeSequenceMarker);
            CharToUnicodeSymbolConverter<ulong> charToUnicodeSymbolConverter = new CharToUnicodeSymbolConverter<TLinkAddress>(Links, _addressToNumberConverter, _unicodeSymbolMarker);
            UnicodeSymbolToCharConverter<ulong> unicodeSymbolToCharConverter = new UnicodeSymbolToCharConverter<TLinkAddress>(Links, _numberToAddressConverter, unicodeSymbolCriterionMatcher);
            RightSequenceWalker<ulong> sequenceWalker = new RightSequenceWalker<TLinkAddress>(Links, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
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

        public string GetFileContent(TLinkAddress address)
        {
            IList<ulong> link = Links.GetLink(address);
            if (Links.GetSource(link) == _fileMarker)
            {
                return Convert(Links.GetTarget(link));
            }
            throw new InvalidOperationException("Link is not a file.");
        }

        public void Delete(TLinkAddress link)
        {
            Links.Delete(link);
        }

        public List<IFile> GetAllFiles()
        {
            List<IFile> files = new() { };
            foreach (IList<ulong> file in Links.All(new Link<UInt64>(index: Any, source: _fileMarker, target: Any)))
            {
                files.Add(new File { Path = file.ToString(), Content = Convert(Links.GetTarget(file)) });
            }
            return files;
        }

        public string AllLinksToString()
        {
            StringBuilder builder = new();
            Link<ulong> query = new Link<UInt64>(index: Any, source: Any, target: Any);
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

        public TLinkAddress AddFileToSet(TLinkAddress set, TLinkAddress file, string path)
        {
            return Links.GetOrCreate(set, Links.GetOrCreate(Convert(path), file));
        }

        public TLinkAddress GetFileSet(string fileSetName)
        {
            return Links.SearchOrDefault(_setMarker, Convert(fileSetName));
        }

        private IList<IList<TLinkAddress>> GetFilesLinksFromSet(string set)
        {
            ulong fileSet = GetFileSet(set);
            IList<IList<ulong>> list = Links.All(new Link<UInt64>(index: Any, source: fileSet, target: Any));
            return list;
        }

        public List<IFile> GetFilesFromSet(string set)
        {
            List<IFile> files = new();
            foreach (IList<ulong> file in GetFilesLinksFromSet(set))
            {
                ulong pathAndFile = Links.GetTarget(file);
                files.Add(new File()
                {
                    Path = Convert(Links.GetSource(pathAndFile)),
                    Content = GetFileContent(Links.GetTarget(pathAndFile))
                });
            }
            return files;
        }
    }
}
