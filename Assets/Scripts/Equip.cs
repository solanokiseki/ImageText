using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ImageText
{
    public class Equip
    {
        public Equip(string name,bool isEquip,int dataID,int count,ulong servID,int atk)
        {
            Name = name;
            IsEquip = isEquip;
            DataID = dataID;
            Count = count;
            ServID = servID;
            ATK = atk;
        }
        public string Name = "绝世好剑";
        public bool IsEquip;
        public int DataID = 123;
        public int Count = 1;
        public ulong ServID = 15151666447;
        public int ATK = 10;
       
    }
}
