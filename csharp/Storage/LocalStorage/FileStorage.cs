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
using Storage.AST;
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
        private readonly TLinkAddress _astNodeMarker;
        private readonly TLinkAddress _astNodeTypeMarker;
        private readonly TLinkAddress _astPositionMarker;
        private readonly CSharpAstTransformer _astTransformer;
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
            _astNodeMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _astNodeTypeMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
            _astPositionMarker = GetOrCreateNextMapping(currentMappingLinkIndex++);
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
            _astTransformer = new CSharpAstTransformer();
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

        /// <summary>
        /// Transforms code into AST and stores it in the links store.
        /// Each AST node is mapped to its exact position in the code text.
        /// </summary>
        /// <param name="code">The code to transform into AST.</param>
        /// <returns>The link address of the root AST node.</returns>
        public TLinkAddress TransformCodeToAst(string code)
        {
            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Code cannot be null or empty.", nameof(code));
            }

            var rootAstNode = _astTransformer.TransformCode(code);
            return StoreAstNodeInLinks(rootAstNode);
        }

        /// <summary>
        /// Stores an AST node and its children in the links store.
        /// </summary>
        /// <param name="astNode">The AST node to store.</param>
        /// <returns>The link address of the stored AST node.</returns>
        private TLinkAddress StoreAstNodeInLinks(AstNode astNode)
        {
            // Create a link for the AST node type
            var nodeTypeLink = CreateString(astNode.NodeType);
            
            // Create a link for the node text
            var nodeTextLink = CreateString(astNode.Text);
            
            // Create position information as a sequence
            var positionData = new List<TLinkAddress>
            {
                CreateBigInteger(astNode.StartPosition),
                CreateBigInteger(astNode.EndPosition),
                CreateBigInteger(astNode.StartLine),
                CreateBigInteger(astNode.StartColumn),
                CreateBigInteger(astNode.EndLine),
                CreateBigInteger(astNode.EndColumn)
            };
            var positionLink = _listToSequenceConverter.Convert(positionData);
            
            // Create the main AST node link
            // Structure: AST_NODE_MARKER -> (NODE_TYPE -> (TEXT -> POSITION))
            var nodeContentLink = _synchronizedLinks.GetOrCreate(nodeTextLink, positionLink);
            var nodeWithTypeLink = _synchronizedLinks.GetOrCreate(nodeTypeLink, nodeContentLink);
            var astNodeLink = _synchronizedLinks.GetOrCreate(_astNodeMarker, nodeWithTypeLink);
            
            // Store children and link them to this node
            foreach (var child in astNode.Children)
            {
                var childLink = StoreAstNodeInLinks(child);
                _synchronizedLinks.GetOrCreate(astNodeLink, childLink);
            }
            
            return astNodeLink;
        }

        /// <summary>
        /// Gets all AST nodes from the links store.
        /// </summary>
        /// <returns>List of all AST node link addresses.</returns>
        public List<TLinkAddress> GetAllAstNodes()
        {
            var astNodes = new List<TLinkAddress>();
            foreach (var astNode in _synchronizedLinks.All(new Link<UInt64>(index: Any, source: _astNodeMarker, target: Any)))
            {
                if (astNode != null && astNode.Count > 0)
                {
                    astNodes.Add(astNode[0]); // Index is at position 0
                }
            }
            return astNodes;
        }

        /// <summary>
        /// Gets the AST node information for a given link address.
        /// </summary>
        /// <param name="astNodeLink">The AST node link address.</param>
        /// <returns>A dictionary containing node information.</returns>
        public Dictionary<string, object> GetAstNodeInfo(TLinkAddress astNodeLink)
        {
            var nodeInfo = new Dictionary<string, object>();
            
            try
            {
                var astNodeData = _synchronizedLinks.GetLink(astNodeLink);
                if (_synchronizedLinks.GetSource(astNodeData) != _astNodeMarker)
                {
                    throw new InvalidOperationException("Link is not an AST node.");
                }
                
                var nodeWithTypeLink = _synchronizedLinks.GetTarget(astNodeData);
                var nodeWithType = _synchronizedLinks.GetLink(nodeWithTypeLink);
                
                var nodeTypeLink = _synchronizedLinks.GetSource(nodeWithType);
                var nodeContentLink = _synchronizedLinks.GetTarget(nodeWithType);
                var nodeContent = _synchronizedLinks.GetLink(nodeContentLink);
                
                var nodeTextLink = _synchronizedLinks.GetSource(nodeContent);
                var positionLink = _synchronizedLinks.GetTarget(nodeContent);
                
                nodeInfo["NodeType"] = GetString(nodeTypeLink);
                nodeInfo["Text"] = GetString(nodeTextLink);
                nodeInfo["LinkAddress"] = astNodeLink;
                
                // Extract position information
                // This is a simplified extraction - in a real implementation,
                // you'd need to properly deserialize the position sequence
                nodeInfo["HasPositionInfo"] = true;
                
                return nodeInfo;
            }
            catch (Exception ex)
            {
                nodeInfo["Error"] = ex.Message;
                return nodeInfo;
            }
        }

        // public void SetLastGithubMigrationTimeStamp()

        protected override void Dispose(bool manual, bool wasDisposed)
        {
            _disposableLinks.Dispose();
        }
    }
}
