using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerLibs
{
	public class NoobSkillStarterPack
	{
		public int[][] NoobSkill()
		{
		
				int[][] skillsInfoArray = new int[47][];
				for (int i = 0; i < skillsInfoArray.Length; i++)
				{
					skillsInfoArray[i] = new int[6];
				}
				for (int i = 0; i < skillsInfoArray.Length; i++)
				{
					skillsInfoArray[i][0] = i;
					skillsInfoArray[i][4] = 1000;
				}
				return skillsInfoArray;
			
		}
	}
}
