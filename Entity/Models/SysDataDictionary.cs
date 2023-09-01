using System;
using System.Collections.Generic;
using System.Text;

namespace Entity.Models
{
    public class SysDataDictionary
    {
        public int DictionaryId { get; set; }
        public int CategoryKey { get; set; }
        public int Category { get; set; }

        public int Name { get; set; }

        public int Key { get; set; }

        public int Value { get; set; }

        public int SortNumber { get; set; }
        public int ParentId { get; set; }

        public int SysDefined { get; set; }
        public int RowGuid { get; set; }

        public int Creator { get; set; }
        public int CreatorTime { get; set; }

        public int Modifier { get; set; }
        public int ModifierTime { get; set; }

        public int IsEnable { get; set; }

    }
}
