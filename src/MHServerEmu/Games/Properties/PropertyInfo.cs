﻿using MHServerEmu.Common.Extensions;
using MHServerEmu.Common.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;

namespace MHServerEmu.Games.Properties
{
    public class PropertyInfo
    {
        private static readonly Logger Logger = LogManager.CreateLogger();

        private readonly PropertyParamType[] _paramTypes = new PropertyParamType[PropertyConsts.MaxParamCount];
        private readonly AssetTypeId[] _paramAssetTypes = new AssetTypeId[PropertyConsts.MaxParamCount];
        private readonly BlueprintId[] _paramPrototypeBlueprints = new BlueprintId[PropertyConsts.MaxParamCount];
        private readonly int[] _paramBitCounts = new int[PropertyConsts.MaxParamCount];
        private readonly int[] _paramOffsets = new int[PropertyConsts.MaxParamCount];
        private readonly int[] _paramMaxValues = new int[PropertyConsts.MaxParamCount];

        private ulong _defaultValue;
        private int _paramCount;
        private int[] _paramDefaultValues;

        private bool _updatedInfo = false;

        public bool IsFullyLoaded { get; set; }

        public PropertyId PropertyId { get; }
        public string PropertyName { get; }

        public string PropertyInfoName { get; }
        public PrototypeId PropertyInfoPrototypeRef { get; }
        public PropertyInfoPrototype PropertyInfoPrototype { get; set; }

        public BlueprintId PropertyMixinBlueprintRef { get; set; } = BlueprintId.Invalid;

        public PropertyId DefaultCurveIndex { get; set; }

        public PropertyDataType DataType { get => PropertyInfoPrototype.Type; }

        public PropertyInfo(PropertyEnum @enum, string propertyInfoName, PrototypeId propertyInfoPrototypeRef)
        {
            PropertyId = new(@enum);
            PropertyInfoName = propertyInfoName;
            PropertyName = $"{PropertyInfoName}Prop";
            PropertyInfoPrototypeRef = propertyInfoPrototypeRef;

            // Initialize param arrays
            for (int i = 0; i < PropertyConsts.MaxParamCount; i++)
            {
                _paramTypes[i] = PropertyParamType.Invalid;
                _paramAssetTypes[i] = AssetTypeId.Invalid;
                _paramPrototypeBlueprints[i] = BlueprintId.Invalid;
                _paramOffsets[i] = 0;
                _paramBitCounts[i] = 0;
            }
        }

        public int[] DecodeParameters(PropertyId propertyId)
        {
            if (_paramCount == 0) return new int[PropertyConsts.MaxParamCount];

            ulong encodedParams = propertyId.Raw & PropertyConsts.ParamMask;
            int[] decodedParams = new int[PropertyConsts.MaxParamCount];

            for (int i = 0; i < _paramCount; i++)
                decodedParams[i] = (int)((encodedParams >> _paramOffsets[i]) & ((1ul << _paramBitCounts[i]) - 1));

            return decodedParams;
        }

        public int GetParamBitCount(int paramIndex)
        {
            if (paramIndex >= PropertyConsts.MaxParamCount)
                return Logger.ErrorReturn(0, $"GetParamBitCount(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] == PropertyParamType.Invalid)
                return Logger.WarnReturn(0, "GetParamBitCount(): param is not set");

            return _paramMaxValues[paramIndex].HighestBitSet() + 1;
        }

        public BlueprintId GetParamPrototypeBlueprint(int paramIndex)
        {
            // NYI
            return BlueprintId.Invalid;
        }

        public bool SetParamTypeInteger(int paramIndex, int maxValue)
        {
            if (paramIndex >= PropertyConsts.MaxParamCount)
                return Logger.ErrorReturn(false, $"SetParamTypeInteger(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] != PropertyParamType.Invalid)
                return Logger.WarnReturn(false, "SetParamTypeInteger(): param is already set");

            _paramTypes[paramIndex] = PropertyParamType.Integer;
            _paramMaxValues[paramIndex] = maxValue;
            return true;
        }

        public bool SetParamTypeAsset(int paramIndex, AssetTypeId assetTypeId)
        {
            if (paramIndex >= PropertyConsts.MaxParamCount)
                return Logger.ErrorReturn(false, $"SetParamTypeAsset(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] != PropertyParamType.Invalid)
                return Logger.WarnReturn(false, "SetParamTypeAsset(): param is already set");

            var assetType = GameDatabase.GetAssetType(assetTypeId);

            if (assetType == null)
                return Logger.ErrorReturn(false, $"SetParamTypeAsset(): failed to get asset type for id {assetTypeId}");

            _paramTypes[paramIndex] = PropertyParamType.Asset;
            _paramMaxValues[paramIndex] = assetType.MaxEnumValue;
            _paramAssetTypes[paramIndex] = assetTypeId;
            return true;
        }

        public bool SetParamTypePrototype(int paramIndex, BlueprintId blueprintId)
        {
            if (paramIndex >= PropertyConsts.MaxParamCount)
                return Logger.ErrorReturn(false, $"SetParamTypePrototype(): param index {paramIndex} out of range");

            if (_paramTypes[paramIndex] != PropertyParamType.Invalid)
                return Logger.WarnReturn(false, "SetParamTypePrototype(): param is already set");

            _paramTypes[paramIndex] = PropertyParamType.Prototype;
            _paramMaxValues[paramIndex] = GameDatabase.DataDirectory.GetPrototypeMaxEnumValue(blueprintId);
            _paramPrototypeBlueprints[paramIndex] = blueprintId;
            return true;
        }

        /// <summary>
        /// Validates params and calculates their bit offsets.
        /// </summary>
        public bool SetPropertyInfo(ulong defaultValue, int paramCount, int[] paramDefaultValues)
        {
            // NOTE: these checks mirror the client, we might not actually need all of them
            if (_updatedInfo) Logger.ErrorReturn(false, "Failed to SetPropertyInfo(): already set");
            if (paramCount > PropertyConsts.MaxParamCount) Logger.ErrorReturn(false, $"Failed to SetPropertyInfo(): invalid param count {paramCount}");

            // Checks to make sure all param types have been set up prior to this
            for (int i = 0; i < PropertyConsts.MaxParamCount; i++)
            {
                if (i < paramCount)
                {
                    if (_paramTypes[i] == PropertyParamType.Invalid)
                        return Logger.ErrorReturn(false, $"Failed to SetPropertyInfo(): param types have not been set up");
                }
                else
                {
                    if (_paramTypes[i] != PropertyParamType.Invalid)
                        return Logger.ErrorReturn(false, $"Failed to SetPropertyInfo(): param count does not match set up params");
                }
            }

            // Set default values
            _defaultValue = defaultValue;
            _paramCount = paramCount;
            _paramDefaultValues = paramDefaultValues;

            // Calculate bit offsets for params
            int offset = PropertyConsts.ParamBitCount;
            for (int i = 0; i < _paramCount; i++)
            {
                _paramBitCounts[i] = _paramMaxValues[i].HighestBitSet() + 1;
                
                offset -= _paramBitCounts[i];
                if (offset < 0) return Logger.ErrorReturn(false, "Param bit overflow!");    // Make sure there are enough bits for all params
                _paramOffsets[i] = offset;
            }

            // NOTE: the client also sets the values of the rest of _paramBitCounts and _paramOffsets to 0 that we don't need to do... probably

            _updatedInfo = true;
            return true;
        }
    }
}
