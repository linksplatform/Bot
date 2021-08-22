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
    /// <summary>
    /// <para>
    /// Represents the file storage.
    /// </para>
    /// <para></para>
    /// </summary>
    public class FileStorage
    {
        /// <summary>
        /// <para>
        /// The unicode sequence marker.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly TLinkAddress _unicodeSequenceMarker;

        /// <summary>
        /// <para>
        /// The meaning root.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly TLinkAddress _meaningRoot;

        /// <summary>
        /// <para>
        /// The address to number converter.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly AddressToRawNumberConverter<TLinkAddress> _addressToNumberConverter;

        /// <summary>
        /// <para>
        /// The number to address converter.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly RawNumberToAddressConverter<TLinkAddress> _numberToAddressConverter;

        /// <summary>
        /// <para>
        /// The string to unicode sequence converter.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly IConverter<string, TLinkAddress> _stringToUnicodeSequenceConverter;

        /// <summary>
        /// <para>
        /// The unicode sequence to string converter.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly IConverter<TLinkAddress, string> _unicodeSequenceToStringConverter;

        /// <summary>
        /// <para>
        /// The links.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly UnitedMemoryLinks<UInt64> Links;

        /// <summary>
        /// <para>
        /// The unicode symbol marker.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly TLinkAddress _unicodeSymbolMarker;

        /// <summary>
        /// <para>
        /// The set marker.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly TLinkAddress _setMarker;

        /// <summary>
        /// <para>
        /// The file marker.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly TLinkAddress _fileMarker;

        /// <summary>
        /// <para>
        /// The any.
        /// </para>
        /// <para></para>
        /// </summary>
        private readonly TLinkAddress Any;

        /// <summary>
        /// <para>
        /// Gets the or create next mapping using the specified current mapping index.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="currentMappingIndex">
        /// <para>The current mapping index.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The link address</para>
        /// <para></para>
        /// </returns>
        private TLinkAddress GetOrCreateNextMapping(TLinkAddress currentMappingIndex) => Links.Exists(currentMappingIndex) ? currentMappingIndex : Links.CreateAndUpdate(_meaningRoot, Links.Constants.Itself);

        /// <summary>
        /// <para>
        /// Gets the or create meaning root using the specified meaning root index.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="meaningRootIndex">
        /// <para>The meaning root index.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The link address</para>
        /// <para></para>
        /// </returns>
        private TLinkAddress GetOrCreateMeaningRoot(TLinkAddress meaningRootIndex) => Links.Exists(meaningRootIndex) ? meaningRootIndex : Links.CreatePoint();

        /// <summary>
        /// <para>
        /// Initializes a new <see cref="FileStorage"/> instance.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="DBFilename">
        /// <para>A db filename.</para>
        /// <para></para>
        /// </param>
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
            _unicodeSequenceMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _setMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _fileMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
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

        /// <summary>
        /// <para>
        /// Converts the str.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="str">
        /// <para>The str.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The link address</para>
        /// <para></para>
        /// </returns>
        public TLinkAddress Convert(string str) => _stringToUnicodeSequenceConverter.Convert(str);

        /// <summary>
        /// <para>
        /// Converts the address.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="address">
        /// <para>The address.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The string</para>
        /// <para></para>
        /// </returns>
        public string Convert(TLinkAddress address) => _unicodeSequenceToStringConverter.Convert(address);

        /// <summary>
        /// <para>
        /// Gets the file content using the specified address.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="address">
        /// <para>The address.</para>
        /// <para></para>
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// <para>Link is not a file.</para>
        /// <para></para>
        /// </exception>
        /// <returns>
        /// <para>The string</para>
        /// <para></para>
        /// </returns>
        public string GetFileContent(TLinkAddress address)
        {
            var link = Links.GetLink(address);
            if (Links.GetSource(link) == _fileMarker)
            {
                return Convert(Links.GetTarget(link));
            }
            throw new InvalidOperationException("Link is not a file.");
        }

        /// <summary>
        /// <para>
        /// Deletes the link.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="link">
        /// <para>The link.</para>
        /// <para></para>
        /// </param>
        public void Delete(TLinkAddress link) => Links.Delete(link);

        /// <summary>
        /// <para>
        /// Gets the all files.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>The files.</para>
        /// <para></para>
        /// </returns>
        public List<File> GetAllFiles()
        {
            List<File> files = new() { };
            foreach (var file in Links.All(new Link<UInt64>(index: Any, source: _fileMarker, target: Any)))
            {
                files.Add(new File { Path = file.ToString(), Content = Convert(Links.GetTarget(file)) });
            }
            return files;
        }

        /// <summary>
        /// <para>
        /// Alls the links to string.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <returns>
        /// <para>The string</para>
        /// <para></para>
        /// </returns>
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

        /// <summary>
        /// <para>
        /// Adds the file using the specified content.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="content">
        /// <para>The content.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The link address</para>
        /// <para></para>
        /// </returns>
        public TLinkAddress AddFile(string content) => Links.GetOrCreate(_fileMarker, _stringToUnicodeSequenceConverter.Convert(content));

        /// <summary>
        /// <para>
        /// Creates the file set using the specified file set name.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="fileSetName">
        /// <para>The file set name.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The link address</para>
        /// <para></para>
        /// </returns>
        public TLinkAddress CreateFileSet(string fileSetName) => Links.GetOrCreate(_setMarker, Convert(fileSetName));

        /// <summary>
        /// <para>
        /// Adds the file to set using the specified set.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="set">
        /// <para>The set.</para>
        /// <para></para>
        /// </param>
        /// <param name="file">
        /// <para>The file.</para>
        /// <para></para>
        /// </param>
        /// <param name="path">
        /// <para>The path.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The link address</para>
        /// <para></para>
        /// </returns>
        public TLinkAddress AddFileToSet(TLinkAddress set, TLinkAddress file, string path) => Links.GetOrCreate(set, Links.GetOrCreate(Convert(path), file));

        /// <summary>
        /// <para>
        /// Gets the file set using the specified file set name.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="fileSetName">
        /// <para>The file set name.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The link address</para>
        /// <para></para>
        /// </returns>
        public TLinkAddress GetFileSet(string fileSetName) => Links.SearchOrDefault(_setMarker, Convert(fileSetName));

        /// <summary>
        /// <para>
        /// Gets the files links from set using the specified set.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="set">
        /// <para>The set.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The list.</para>
        /// <para></para>
        /// </returns>
        private IList<IList<TLinkAddress>> GetFilesLinksFromSet(string set)
        {
            var fileSet = GetFileSet(set);
            var list = Links.All(new Link<UInt64>(index: Any, source: fileSet, target: Any));
            return list;
        }

        /// <summary>
        /// <para>
        /// Gets the files from set using the specified set.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="set">
        /// <para>The set.</para>
        /// <para></para>
        /// </param>
        /// <returns>
        /// <para>The files.</para>
        /// <para></para>
        /// </returns>
        public List<File> GetFilesFromSet(string set)
        {
            List<File> files = new();
            foreach (var file in GetFilesLinksFromSet(set))
            {
                var pathAndFile = Links.GetTarget(file);
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
