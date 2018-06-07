#region Всякие либы
using Models;
using MG;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Microsoft.AspNet.SignalR;
using Newtonsoft.Json;
using System.Threading;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Drawing.Imaging;
#endregion

namespace Server
{
	public class Skill
	{

		#region Настройка хаба
		private readonly IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<Game>();
		#endregion
		/// <summary>
		/// Метод отвечающий за прокачку скилла
		/// </summary>
		/// <param name="skillId"> Ид скилла</param>
		/// <param name="cid"> ConnectionId</param>
		public void PrSkill(int skillId, string cid)//прокачка скила
		{
			if (Broadcaster.Instance.players.Any(p => p.ConnectionId == cid))
			{
				PlayerModel player = Broadcaster.Instance.players.First(p => p.ConnectionId == cid);
				if (player.skillsInfoArray[skillId][3] < player.skillsInfoArray[skillId][4])
				{
					player.skillsInfoArray[skillId][3] += 1;
				}
				Broadcaster.Instance.players.First(p => p.ConnectionId == cid).skillsInfoArray[skillId] = player.skillsInfoArray[skillId];
				SendSkill(skillId, player.skillsInfoArray[skillId], cid);
			}
		}
		public void SendSkill(int skillId, int[] skillData, string cid)
		{
			SkillModel skillModel = new SkillModel();
			skillModel.SkillId = skillId;
			skillModel.SkillData = skillData;
			hubContext.Clients.Client(cid).skilldata(skillModel);
		}

	}
}
