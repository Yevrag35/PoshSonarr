using MG.Sonarr.Functionality;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Results
{
    [JsonObject(MemberSerialization.OptIn)]
    public abstract class SizedResult : BaseResult, ICanCalculate
    {
        internal const decimal KB = 1024M;
        internal const decimal MB = 1048576M;           //1,048,576
        internal const decimal GB = 1073741824M;        //‭1,073,741,824‬
        internal const decimal TB = 1099511627776M;     //‭1,099,511,627,776‬

        private const int DEFAULT_PLACES = 2;
        private const MidpointRounding ROUND = MidpointRounding.AwayFromZero;

        [JsonProperty("sizeOnDisk")]
        public virtual long SizeOnDisk { get; protected set; }

        public decimal ToSize(ByteUnit inUnit)
        {
            return this.ToSize(inUnit, -1);
        }
        public virtual decimal ToSize(ByteUnit inUnit, int decimalPlaces)
        {
            switch (inUnit)
            {
                case ByteUnit.MB:
                    return Calculate(this.SizeOnDisk, MB, decimalPlaces);

                case ByteUnit.KB:
                    return Calculate(this.SizeOnDisk, KB, decimalPlaces);

                case ByteUnit.TB:
                    return Calculate(this.SizeOnDisk, TB, decimalPlaces);

                default:
                    return Calculate(this.SizeOnDisk, GB, decimalPlaces);
            }
        }

        internal static decimal Calculate(long sizeInBytes, decimal divideBy, int numberOfDecimalPlaces)
        {
            if (numberOfDecimalPlaces > -1)
            {
                return Math.Round(
                    sizeInBytes / divideBy,
                    numberOfDecimalPlaces,
                    ROUND
                );
            }
            else
            {
                return sizeInBytes / divideBy;
            }
        }
    }
}
