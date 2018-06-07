using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Models
{
    public class BuildingData
    {

        public byte packType { get; set; }
        public ushort[] paramSArray { get; set; }
        public uint[] paramIArray { get; set; }
        public ulong[] paramLArray { get; set; }
        public byte buildingType { get; set; }//тип здания 
        public int buildingID { get; set; }//ID здаания 
        public int ownerID { get; set; }//ID владельца 
        public long money { get; set; }
        public ushort x { get; set; }//координата здания по горизонтали 
        public ushort y { get; set; }//координата здания по вертикали 

        

    }
}
