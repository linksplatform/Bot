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
using System.Numerics;
using System.Text;
using Platform.Data.Doublets.Numbers.Raw;
using Platform.Disposables;
using TLinkAddress = System.UInt64;

namespace Storage.Local
{
    /// <summary>
    /// <para>
    /// Represents the file storage.
    /// </para>
    /// <para></para>
    /// </summary>
    public class FileStorage : DisposableBase
    {
        private readonly TLinkAddress _unicodeSequenceMarker;
        private readonly TLinkAddress _meaningRoot;
        private readonly AddressToRawNumberConverter<TLinkAddress> _addressToNumberConverter;
        private readonly RawNumberToAddressConverter<TLinkAddress> _numberToAddressConverter;
        private readonly IConverter<string, TLinkAddress> _stringToUnicodeSequenceConverter;
        private readonly IConverter<TLinkAddress, string> _unicodeSequenceToStringConverter;
        private readonly IConverter<IList<TLinkAddress>, TLinkAddress> _listToSequenceConverter;
        private readonly IConverter<BigInteger, TLinkAddress> _bigIntederToRawNumberConverter;
        private readonly IConverter<TLinkAddress, BigInteger> _rawNumberToBigIntegerConverter;
        private readonly TLinkAddress _negativeNumberIndex;
        private readonly UnitedMemoryLinks<TLinkAddress> _disposableLinks;
        private readonly SynchronizedLinks<TLinkAddress> _synchronizedLinks;
        private readonly TLinkAddress _unicodeSymbolMarker;
        private readonly TLinkAddress _setMarker;
        private readonly TLinkAddress _fileMarker;
        private readonly TLinkAddress _gitHubLastMigrationTimestampMarker;
        private readonly TLinkAddress Any;
        private TLinkAddress GetOrCreateNextMapping(TLinkAddress currentMappingIndex) => _synchronizedLinks.Exists(currentMappingIndex) ? currentMappingIndex : _synchronizedLinks.CreateAndUpdate(_meaningRoot, _synchronizedLinks.Constants.Itself);
        private TLinkAddress GetOrCreateMeaningRoot(TLinkAddress meaningRootIndex) => _synchronizedLinks.Exists(meaningRootIndex) ? meaningRootIndex : _synchronizedLinks.CreatePoint();

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
            _disposableLinks = new UnitedMemoryLinks<TLinkAddress>(dataMemory, UnitedMemoryLinks<UInt64>.DefaultLinksSizeStep, linksConstants, IndexTreeType.Default);
            _synchronizedLinks = new SynchronizedLinks<TLinkAddress>(_disposableLinks);
            var link = _synchronizedLinks.Create();
            link = _synchronizedLinks.Update(link, newSource: link, newTarget: link);
            ushort currentMappingLinkIndex = 1;
            Any = _synchronizedLinks.Constants.Any;
            _meaningRoot = GetOrCreateMeaningRoot(currentMappingLinkIndex++);
            _unicodeSymbolMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _unicodeSequenceMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _negativeNumberIndex = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _setMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _fileMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _gitHubLastMigrationTimestampMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _addressToNumberConverter = new AddressToRawNumberConverter<TLinkAddress>();
            _numberToAddressConverter = new RawNumberToAddressConverter<TLinkAddress>();
            var balancedVariantConverter = new BalancedVariantConverter<TLinkAddress>(_synchronizedLinks);
            var unicodeSymbolCriterionMatcher = new TargetMatcher<TLinkAddress>(_synchronizedLinks, _unicodeSymbolMarker);
            var unicodeSequenceCriterionMatcher = new TargetMatcher<TLinkAddress>(_synchronizedLinks, _unicodeSequenceMarker);
            var charToUnicodeSymbolConverter = new CharToUnicodeSymbolConverter<TLinkAddress>(_synchronizedLinks, _addressToNumberConverter, _unicodeSymbolMarker);
            var unicodeSymbolToCharConverter = new UnicodeSymbolToCharConverter<TLinkAddress>(_synchronizedLinks, _numberToAddressConverter, unicodeSymbolCriterionMatcher);
            var sequenceWalker = new RightSequenceWalker<TLinkAddress>(_synchronizedLinks, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
            _stringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(new StringToUnicodeSequenceConverter<TLinkAddress>(_synchronizedLinks, charToUnicodeSymbolConverter, balancedVariantConverter, _unicodeSequenceMarker));
            var unicodeSequenceToStringConverter = new UnicodeSequenceToStringConverter<TLinkAddress>(_synchronizedLinks, unicodeSequenceCriterionMatcher, sequenceWalker, unicodeSymbolToCharConverter, _unicodeSequenceMarker);
            _unicodeSequenceToStringConverter = new CachingConverterDecorator<TLinkAddress, string>(unicodeSequenceToStringConverter);
            _listToSequenceConverter = new BalancedVariantConverter<TLinkAddress>(_synchronizedLinks);
            _bigIntederToRawNumberConverter = new BigIntegerToRawNumberSequenceConverter<TLinkAddress>(_synchronizedLinks, _addressToNumberConverter, _listToSequenceConverter, _negativeNumberIndex);
            _rawNumberToBigIntegerConverter = new RawNumberSequenceToBigIntegerConverter<TLinkAddress>(_synchronizedLinks, _numberToAddressConverter, _negativeNumberIndex);
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
        public TLinkAddress CreateString(string str) => _stringToUnicodeSequenceConverter.Convert(str);

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
        public string GetString(TLinkAddress address) => _unicodeSequenceToStringConverter.Convert(address);

        /// <summary>
        ///
        /// </summary>
        /// <param name="bigInteger"></param>
        /// <returns></returns>
        public TLinkAddress CreateBigInteger(BigInteger bigInteger) => _bigIntederToRawNumberConverter.Convert(bigInteger);

        /// <summary>
        ///
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public BigInteger GetBigInteger(TLinkAddress address) => _rawNumberToBigIntegerConverter.Convert(address);

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
            var link = _synchronizedLinks.GetLink(address);
            if (_synchronizedLinks.GetSource(link) == _fileMarker)
            {
                return GetString(_synchronizedLinks.GetTarget(link));
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
        public void Delete(TLinkAddress link) => _synchronizedLinks.Delete(link);

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
            foreach (var file in _synchronizedLinks.All(new Link<UInt64>(index: Any, source: _fileMarker, target: Any)))
            {
                files.Add(new File { Path = file.ToString(), Content = GetString(_synchronizedLinks.GetTarget(file)) });
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
            _synchronizedLinks.Each(link =>
            {
                builder.AppendLine(_synchronizedLinks.Format(link));
                return _synchronizedLinks.Constants.Continue;
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
        public TLinkAddress AddFile(string content) => _synchronizedLinks.GetOrCreate(_fileMarker, _stringToUnicodeSequenceConverter.Convert(content));

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
        public TLinkAddress CreateFileSet(string fileSetName) => _synchronizedLinks.GetOrCreate(_setMarker, CreateString(fileSetName));

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
        public TLinkAddress AddFileToSet(TLinkAddress set, TLinkAddress file, string path) => _synchronizedLinks.GetOrCreate(set, _synchronizedLinks.GetOrCreate(CreateString(path), file));

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
        public TLinkAddress GetFileSet(string fileSetName) => _synchronizedLinks.SearchOrDefault(_setMarker, CreateString(fileSetName));
        private IList<IList<TLinkAddress>?> GetFilesLinksFromSet(string set)
        {
            var fileSet = GetFileSet(set);
            var list = _synchronizedLinks.All(new Link<UInt64>(index: Any, source: fileSet, target: Any));
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
                var pathAndFile = _synchronizedLinks.GetTarget(file);
                files.Add(new File()
                {
                    Path = GetString(_synchronizedLinks.GetSource(pathAndFile)),
                    Content = GetFileContent(_synchronizedLinks.GetTarget(pathAndFile))
                });
            }
            return files;
        }

        // public void SetLastGithubMigrationTimeStamp()

        protected override void Dispose(bool manual, bool wasDisposed)
        {
            _disposableLinks.Dispose();
        }
    }
}
