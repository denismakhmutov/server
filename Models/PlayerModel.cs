using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Models
{
    public class PlayerModel
    {
       


        public string ConnectionId { get; set; }
        public string Name { get; set; }
        public int Id { get; set; }
        public int x { get; set; }
        public int y { get; set; }
		public char angle { get; set; }//поворот
		public int[] cri { get; set; }//массив кристаллов
		public short Delay { get; set; }//массив кристаллов
		public int skin { get; set; }//скин
		public List<GeoModel> Geo { get; set; }//геология
		public int[][] skillsInfoArray { get; set; }//массив скиллов персонажа
		public float[] skillsProperties { get; set; }//Массив рассчитанных парраметров
	}
}
