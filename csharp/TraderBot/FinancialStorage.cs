using Platform.Collections.Stacks;
using Platform.Converters;
using Platform.Data;
using Platform.Data.Doublets;
using Platform.Data.Doublets.CriterionMatchers;
using Platform.Data.Doublets.Memory.United.Generic;
using Platform.Data.Doublets.Numbers.Rational;
using Platform.Data.Doublets.Numbers.Raw;
using Platform.Data.Doublets.Sequences.Converters;
using Platform.Data.Doublets.Sequences.Walkers;
using Platform.Data.Doublets.Unicode;
using Platform.Data.Numbers.Raw;
using Platform.Memory;
using Platform.Numbers;
using TLinkAddress = System.UInt64;

namespace TraderBot;

// TODO: Under construction

public class FinancialStorage
{
    public readonly ILinks<TLinkAddress> Storage;
    public readonly CachingConverterDecorator<string, TLinkAddress> StringToUnicodeSequenceConverter;
    public readonly CachingConverterDecorator<TLinkAddress, string> UnicodeSequenceToStringConverter;
    public readonly AddressToRawNumberConverter<TLinkAddress> AddressToNumberConverter;
    public readonly RawNumberSequenceToBigIntegerConverter<TLinkAddress> RawNumberSequenceToBigIntegerConverter;
    public readonly BigIntegerToRawNumberSequenceConverter<TLinkAddress> BigIntegerToRawNumberSequenceConverter;
    public readonly TLinkAddress NegativeNumberMarker;
    public readonly DecimalToRationalConverter<TLinkAddress> DecimalToRationalConverter;
    public readonly RationalToDecimalConverter<TLinkAddress> RationalToDecimalConverter;
    public readonly TLinkAddress Type;
    public readonly TLinkAddress SequenceType;
    public readonly TLinkAddress EtfType;
    public readonly TLinkAddress AssetType;
    public readonly TLinkAddress BalanceType;
    public readonly TLinkAddress OperationCurrencyFieldType;
    public readonly TLinkAddress AmountType;
    public readonly TLinkAddress RubType;
    public readonly TLinkAddress OperationType;
    public readonly TLinkAddress OperationFieldType;
    public readonly TLinkAddress IdOperationFieldType;
    public readonly TLinkAddress ParentOperationIdOperationFieldType;
    public readonly TLinkAddress PaymentOperationFieldType;
    public readonly TLinkAddress PriceOperationFieldType;
    public readonly TLinkAddress StateOperationFieldType;
    public readonly TLinkAddress QuantityOperationFieldType;
    public readonly TLinkAddress QuantityRestOperationFieldType;
    public readonly TLinkAddress FigiOperationFieldType;
    public readonly TLinkAddress InstrumentTypeOperationFieldType;
    public readonly TLinkAddress DateOperationFieldType;
    public readonly TLinkAddress TypeAsStringOperationFieldType;
    public readonly TLinkAddress TypeAsEnumOperationFieldType;
    public readonly TLinkAddress TradesOperationFieldType;

    public FinancialStorage()
    {
        HeapResizableDirectMemory memory = new();
        UnitedMemoryLinks<TLinkAddress> storage = new (memory);
        SynchronizedLinks<TLinkAddress> synchronizedStorage = new(storage);
        Storage = synchronizedStorage;
        Storage = storage;
        TLinkAddress zero = default;
        TLinkAddress one = Arithmetic.Increment(zero);
        var typeIndex = one;
        Type = Storage.GetOrCreate(typeIndex, typeIndex);
        var typeId = Storage.GetOrCreate(Type, Arithmetic.Increment(ref typeIndex));
        var meaningRoot = Storage.GetOrCreate(Type, Type);
        var unicodeSymbolMarker = Storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref Type));
        var unicodeSequenceMarker = Storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref Type));
        NegativeNumberMarker = Storage.GetOrCreate(meaningRoot, Arithmetic.Increment(ref Type));
        BalancedVariantConverter<TLinkAddress> balancedVariantConverter = new(Storage);
        RawNumberToAddressConverter<TLinkAddress> NumberToAddressConverter = new();
        AddressToNumberConverter = new();
        TargetMatcher<TLinkAddress> unicodeSymbolCriterionMatcher = new(Storage, unicodeSymbolMarker);
        TargetMatcher<TLinkAddress> unicodeSequenceCriterionMatcher = new(Storage, unicodeSequenceMarker);
        CharToUnicodeSymbolConverter<TLinkAddress> charToUnicodeSymbolConverter =
            new(Storage, AddressToNumberConverter, unicodeSymbolMarker);
        UnicodeSymbolToCharConverter<TLinkAddress> unicodeSymbolToCharConverter =
            new(Storage, NumberToAddressConverter, unicodeSymbolCriterionMatcher);
        StringToUnicodeSequenceConverter = new CachingConverterDecorator<string, TLinkAddress>(
            new StringToUnicodeSequenceConverter<TLinkAddress>(Storage, charToUnicodeSymbolConverter,
                balancedVariantConverter, unicodeSequenceMarker));
        RightSequenceWalker<TLinkAddress> sequenceWalker =
            new(Storage, new DefaultStack<TLinkAddress>(), unicodeSymbolCriterionMatcher.IsMatched);
        UnicodeSequenceToStringConverter = new CachingConverterDecorator<TLinkAddress, string>(
            new UnicodeSequenceToStringConverter<TLinkAddress>(Storage, unicodeSequenceCriterionMatcher, sequenceWalker, unicodeSymbolToCharConverter, unicodeSequenceMarker));
        BigIntegerToRawNumberSequenceConverter =
            new(Storage, AddressToNumberConverter, balancedVariantConverter, NegativeNumberMarker);
        RawNumberSequenceToBigIntegerConverter = new(Storage, NumberToAddressConverter, NegativeNumberMarker);
        DecimalToRationalConverter = new(Storage, BigIntegerToRawNumberSequenceConverter);
        RationalToDecimalConverter = new(Storage, RawNumberSequenceToBigIntegerConverter);
        SequenceType = GetOrCreateType(Type, nameof(SequenceType));
        OperationType = GetOrCreateType(Type, nameof(OperationType));
        OperationFieldType = GetOrCreateType(OperationType, nameof(OperationFieldType));
        IdOperationFieldType = GetOrCreateType(OperationFieldType, nameof(IdOperationFieldType));
        ParentOperationIdOperationFieldType = GetOrCreateType(OperationFieldType, nameof(ParentOperationIdOperationFieldType));
        OperationCurrencyFieldType = GetOrCreateType(OperationFieldType, nameof(OperationCurrencyFieldType));
        PaymentOperationFieldType = GetOrCreateType(OperationFieldType, nameof(PaymentOperationFieldType));
        PriceOperationFieldType = GetOrCreateType(OperationFieldType, nameof(PriceOperationFieldType));
        StateOperationFieldType = GetOrCreateType(OperationFieldType, nameof(StateOperationFieldType));
        QuantityOperationFieldType = GetOrCreateType(OperationFieldType, nameof(QuantityOperationFieldType));
        QuantityRestOperationFieldType = GetOrCreateType(OperationFieldType, nameof(QuantityRestOperationFieldType));
        FigiOperationFieldType = GetOrCreateType(OperationFieldType, nameof(FigiOperationFieldType));
        InstrumentTypeOperationFieldType = GetOrCreateType(OperationFieldType, nameof(InstrumentTypeOperationFieldType));
        DateOperationFieldType = GetOrCreateType(OperationFieldType, nameof(DateOperationFieldType));
        TypeAsStringOperationFieldType = GetOrCreateType(OperationFieldType, nameof(TypeAsStringOperationFieldType));
        TypeAsEnumOperationFieldType = GetOrCreateType(OperationFieldType, nameof(TypeAsEnumOperationFieldType));
        TradesOperationFieldType = GetOrCreateType(OperationFieldType, nameof(TradesOperationFieldType));
        AssetType = GetOrCreateType(Type, nameof(AssetType));
        BalanceType = GetOrCreateType(Type, nameof(BalanceType));
        EtfType = GetOrCreateType(AssetType, nameof(EtfType));
        OperationCurrencyFieldType = GetOrCreateType(AssetType, nameof(OperationCurrencyFieldType));
        RubType = GetOrCreateType(OperationCurrencyFieldType, nameof(RubType));
        AmountType = GetOrCreateType(Type, nameof(AmountType));

        // var amountAddress = Storage.GetOrCreate(AmountType, DecimalToRationalConverter.Convert(RubBalance));
        // var rubAmountAddress = Storage.GetOrCreate(RubType, amountAddress);
        // var runBalanceAddress = Storage.GetOrCreate(BalanceType, rubAmountAddress);

        // var InstrumentTickerLink = StringToUnicodeSequenceConverter.Convert(InstrumentTicker);

        // foreach (var portfolioPosition in investApi.Operations.GetPortfolio(new PortfolioRequest(){AccountId = Account.Id}).Positions)
        // {
        //     if (portfolioPosition.Figi != Instrument.Figi)
        //     {
        //         continue;
        //     }
        //     InstrumentQuantity = portfolioPosition.Quantity;
        //     amountAddress = Storage.GetOrCreate(AmountType, DecimalToRationalConverter.Convert(InstrumentQuantity));
        //     var etfAmountAddress = Storage.GetOrCreate(EtfType, amountAddress);
        //     var etfBalanceAddress = Storage.GetOrCreate(BalanceType, etfAmountAddress);
        //     _logger.LogInformation($"[{portfolioPosition.Figi} {Instrument.Ticker}] quantity: {portfolioPosition.Quantity}");
        // }

        // Storage.Each(new Link<TLinkAddress>(any, any, any), link =>
        // {
        //     var balance = Storage.GetSource(link);
        //     if (!EqualityComparer.Equals(balance, Balance))
        //     {
        //         return @continue;
        //     }
        //     var balanceValue = Storage.GetTarget(link);
        //     var balanceValueType = Storage.GetSource(balanceValue);
        //     if (EqualityComparer.Equals(balanceValueType, Rub))
        //     {
        //         var amountAddress = Storage.GetTarget(balanceValue);
        //         var amountValue = GetAmountValueOrDefault(amountAddress);
        //         if (!amountValue.HasValue)
        //         {
        //             return @continue;
        //         }
        //         rubBalance = amountValue;
        //         _logger.LogInformation($"Rub amount: {amountValue}");
        //     }
        //     else if (EqualityComparer.Equals(balanceValueType, Etf))
        //     {
        //         var amountAddress = Storage.GetTarget(balanceValue);
        //         var amountValue = GetAmountValueOrDefault(amountAddress);
        //         if (!amountValue.HasValue)
        //         {
        //             return @continue;
        //         }
        //         _logger.LogInformation($"{EtfTicker} amount: {amountValue}");
        //     }
        //     return @continue;
        // });

        // var @continue = Storage.Constants.Continue;
        // var any = Storage.Constants.Any;
        // TLinkAddress operationFieldsSequenceLinkAddress = default;
        //  Storage.Each(new Link<TLinkAddress>(any, OperationType, any), linkAddress =>
        // {
        //     var sequence = Storage.GetTarget(linkAddress);
        //     var sequenceType = Storage.GetSource(sequence);
        //     if (!EqualityComparer.Equals(sequenceType, SequenceType))
        //     {
        //         return @continue;
        //     }
        //     operationFieldsSequenceLinkAddress = sequence;
        //     return Storage.Constants.Break;
        // });
        //  if (EqualityComparer.Equals(operationFieldsSequenceLinkAddress, default))
        //  {
        //      return default;
        //  }
        // RightSequenceWalker<TLinkAddress> rightSequenceWalker = new(Storage, new DefaultStack<TLinkAddress>(), linkAddress =>
        // {
        //     var operationFieldTypeSubtype = Storage.GetSource(linkAddress);
        //     var operationFieldType = Storage.GetSource(operationFieldTypeSubtype);
        //     return EqualityComparer.Equals(operationFieldType, OperationFieldType);
        // });
        // var operationFieldLinkAddresses = rightSequenceWalker.Walk(operationFieldsSequenceLinkAddress);
        // foreach (var operationFieldLinkAddress in operationFieldLinkAddresses)
        // {
        //     Tinkoff.InvestApi.V1.Operation operation = new();
        //     if (EqualityComparer.Equals(operationFieldLinkAddress, IdOperationFieldType))
        //     {
        //         var idLink = Storage.GetTarget(operationFieldLinkAddress);
        //         operation.Id = UnicodeSequenceToStringConverter.Convert(idLink);
        //     }
        // }
    }

    public decimal? GetAmountValueOrDefault(TLinkAddress amountAddress)
    {
        var amountType = Storage.GetSource(amountAddress);
        if (amountType != AmountType)
        {
            return null;
        }
        var amountValueAddress = Storage.GetTarget(amountAddress);
        return RationalToDecimalConverter.Convert(amountValueAddress);
    }

    public TLinkAddress GetOrCreateType(TLinkAddress baseType, string typeId)
    {
        return Storage.GetOrCreate(baseType, StringToUnicodeSequenceConverter.Convert(typeId));
    }
}