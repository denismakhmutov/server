using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Models
{
    public class ChunkModel
    {
       public byte[,] chunk { get; set; }
       public string chunks { get; set; }
		
       public int x { get; set; }
       public int y { get; set; }
       public List<byte[]> data { get; set; }


    }
}
