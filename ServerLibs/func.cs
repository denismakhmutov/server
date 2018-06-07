using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerLibs
{
	public class func
	{
		public int Cri(float cel,float man,float min)//Функция рассчета кри за удар
		{
			Random rand=new Random();
			//float cel = 10;//целая часть от добычи кристалла 
			//float man = 0.5f;//мантисса от добычи 
			//float min = 1;//Минимальное кол-во, зависит от кристалки 
			int prof = 0;//Это число кристаллов, которое бот выбьет 

			//тут мы получаем профит из целой части 
			prof = rand.Next((int)min, (int)cel);

			//тут мы смотрим, максимальное ли число енам выпало, если да, то в дело вступает мантисса 
			if (prof == (int)cel)
			{
				if (rand.Next(0, 100) < (man * 100))
				{
					prof++;//если рандом от нуля до 1 меньше вероятности, профицит растет на 1 кристалл 
				}
			}
			return prof;
		}
	}
}
