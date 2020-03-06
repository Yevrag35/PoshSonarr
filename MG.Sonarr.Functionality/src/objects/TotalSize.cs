using MG.Sonarr.Results;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MG.Sonarr.Functionality
{
    public class TotalSize
    {
        private const decimal MB = 1048576.00M;
        private const decimal GB = 1073741824.00M;
        private decimal _total;

        public decimal ByteSize => _total;
        public decimal MegabyteSize => Math.Round(_total / MB, 2);
        public decimal GigabyteSize => Math.Round(_total / GB, 2);

        private TotalSize(ICanCalculate calculate) => _total = calculate.GetTotalFileSize();

        public static TotalSize Get(ICanCalculate obj) => new TotalSize(obj);
    }
}
