#region Либы
using Microsoft.AspNet.SignalR;
using Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using ServerLibs;
#endregion
namespace Server
{

	public class Game : Hub
	{
		#region СТАРТАП
		VkAPI VkAPI;
		public Broadcaster _broadcaster;
		//Timer checkplayersPos;
		//private readonly TimeSpan CheckPosInterval = TimeSpan.FromMilliseconds(1000);
		BDDataContext BD = new BDDataContext();
		public Game()
			: this(Broadcaster.Instance)
		{
		}
		public Game(Broadcaster broadcaster)
		{
			//GlobalHost.Configuration.ConnectionTimeout = new TimeSpan(0, 0, 10);
			GlobalHost.Configuration.DefaultMessageBufferSize = 3000;
			GlobalHost.Configuration.DisconnectTimeout = new TimeSpan(0, 0, 7);
			//GlobalHost.Configuration.KeepAlive = new TimeSpan(0, 0, 2);
			_broadcaster = broadcaster;
			//checkplayersPos = new Timer(SendPos, null, CheckPosInterval, CheckPosInterval);
		}
		
		#endregion
		#region ИГРОВАЯ МЕХАНИКА
		//public PlayerModel RegisterPlayer(PlayerModel player)
		//{
		//    player.ConnectionId = Context.ConnectionId;
		//    _broadcaster.AddPlayerInGame(player);
		//    return player;
		//}
		//public void RegisterObject(MoveModel obj)
		//{
		//    obj.Auth = Context.ConnectionId;

		//    _broadcaster.RegisterModel(obj);
		//}
		//public void restart()
		//{


		//    _broadcaster.restart();
		//}

		// void SendPos(object state)
		//{
		//	if (_broadcaster.nosend)
		//		return;
		//	if (Broadcaster.Instance.players.Any(p => p.ConnectionId == Context.ConnectionId))
		//	{
		//		PlayerModel player1 = Broadcaster.Instance.players.First(p => p.ConnectionId == Context.ConnectionId);
		//		SendPosition(player1.x, player1.y);
		//	}

				
			
		//}
		public void GetChunk(int x, int y)
		{
			_broadcaster.GetChunk(Context.ConnectionId, x, y);

		}
		
		public void ChangeChunk(char param, char dir)
		{
			if (Broadcaster.Instance.players.First(p => p.ConnectionId == Context.ConnectionId).Delay>0) return;
			//Broadcaster.Instance.players.First(p => p.ConnectionId == Context.ConnectionId).Delay = false;
			if (Broadcaster.Instance.players.Any(p => p.ConnectionId == Context.ConnectionId))
			{
				int x;
				int y;
				PlayerModel player = Broadcaster.Instance.players.First(p => p.ConnectionId == Context.ConnectionId);
				x = 0;
				y = 0;
				switch (dir)
				{
					case 'U':
						x = player.x;
						y = player.y + 1;
						break;
					case 'D':
						x = player.x;
						y = player.y - 1;
						break;
					case 'R':
						x = player.x + 1;
						y = player.y;
						break;
					case 'L':
						x = player.x - 1;
						y = player.y;
						break;
				}
				_broadcaster.chunk.ChangeChunk(x, y, param, Context.ConnectionId);

			}
			else
			{
				//тут будем посылать нахуй, ибо сюда не попасть просто так
			}
		}
		public void CheckConnection()
			{
				if (!_broadcaster.ConnectedPlayersConnectionIds.Any(p => p.ConnectionId == Context.ConnectionId))
				{
					PlayerModel check = new PlayerModel();
					check.ConnectionId = Context.ConnectionId;
					//   _broadcaster.ConnectedPlayersConnectionIds.Enqueue(check);
				}
			
		}

		private static readonly TimeSpan _disconnectThreshold = TimeSpan.FromSeconds(10);
		private void DisconnectClient(string clientId, bool useThreshold = false)
		{
			string userId = Context.ConnectionId;

			if (String.IsNullOrEmpty(userId))
			{
				//_logger.Log("Failed to disconnect {0}. No user found", clientId);
				return;
			}

			if (useThreshold)
			{
				Thread.Sleep(_disconnectThreshold);
			}

			// Query for the user to get the updated status
			PlayerModel user = _broadcaster.players.FirstOrDefault(p => p.ConnectionId == Context.ConnectionId);//_repository.GetUserById(userId);

			// There's no associated user for this client id
			if (user == null)
			{
				// _logger.Log("Failed to disconnect {0}:{1}. No user found", userId, clientId);
				return;
			}

			// _repository.Reload(user);

			// _logger.Log("{0}:{1} disconnected", user.Name, Context.ConnectionId);

			// The user will be marked as offline if all clients leave
			if (user.ConnectionId != Context.ConnectionId)
			{
				//_logger.Log("Marking {0} offline", user.Name);

				_broadcaster.ConnectedPlayersConnectionIds.Enqueue(user);

				//Clients.AllExcept(room.Name).leave(userViewModel, room.Name);

			}

		}
		#endregion
		#region СЕССИИ
		#region АВТОРИЗАЦИЯ ЧЕРЕЗ ВК
		public void VKAuth()
		{



			VkAPI = new VkAPI();
			string token = VkAPI.GetToken();
			Clients.Caller.debug(token);
		}
		#endregion
		#region ПОДКЛЮЧЕНИЕ
		public Task Connect(int id)// дисконнект конкретного игрока с задержкой 7 секунд
		{
			//BD.UsersVK
			// int id = 0;
			id = Broadcaster.Instance.players.Count();
			var User = (from u in BD.UsersData where u.Id == id select u).ToArray();
			
			if (User.Length > 0)// Тут еще проверочку на пасс
			{





				if (LeavePlayers.Any(p => p.Id == id))
				{
					LeavePlayers.First(p => p.Id == id).ConnectionId = Context.ConnectionId;
					_broadcaster.players.Add(LeavePlayers.First(p => p.Id == id));
					LeavePlayers.Remove(LeavePlayers.First(p => p.Id == id));
				}
				else if (_broadcaster.players.Any(p => p.Id == id))
				{
					_broadcaster.players.First(p => p.Id == id).ConnectionId = Context.ConnectionId;

				}
				else
				{
					PlayerModel newPlayer = oldplayer(Context.ConnectionId,id,User[0]);
					SendPosition(newPlayer.x, newPlayer.y);
					_broadcaster.players.Add(newPlayer);
				}
			}
			else
			{
				PlayerModel newPlayer = newplayer(Context.ConnectionId, "dEND", BD.UsersData.Count());
				#region Добавление юзера в бд
				UsersData RegModel = new UsersData();
				RegModel.Id = BD.UsersData.Count();
				RegModel.Name = newPlayer.Name;
				RegModel.Money = 10000;
				RegModel.Credits = 2;
				RegModel.SkillData = JsonConvert.SerializeObject(newPlayer.skillsInfoArray);
				BD.UsersData.InsertOnSubmit(RegModel);
				BD.SubmitChanges();
				#endregion
				SendPosition(newPlayer.x, newPlayer.y);//кидаем его на эту точку
				_broadcaster.players.Add(newPlayer);//добавляем в список
			}
			return base.OnConnected();
		}
		#endregion
		#region Тут добавление игроков
		public PlayerModel newplayer(string ConnectionId, string Name,int id)
		{
			PlayerModel newPlayer = new PlayerModel();
			newPlayer.ConnectionId = ConnectionId;//ид соединения
			newPlayer.Id = id;// ИД
			newPlayer.Name = Name;
			newPlayer.cri = new int[6];//кол-во типов кри
			#region СтартерПак
			NoobSkillStarterPack noobSkillStarterPack = new NoobSkillStarterPack();
			newPlayer.skillsInfoArray = noobSkillStarterPack.NoobSkill();
			#endregion
			newPlayer.x = 330;
			newPlayer.y = 310;
			newPlayer.angle = 'u';
			return newPlayer;
		}
		public PlayerModel oldplayer(string ConnectionId,  int id, UsersData User)
		{
			PlayerModel newPlayer = new PlayerModel();
			newPlayer.ConnectionId = ConnectionId;//ид соединения
			newPlayer.Id = id;// ИД
			newPlayer.Name = User.Name;
			newPlayer.cri = new int[6];//кол-во типов кри
			newPlayer.skillsInfoArray = JsonConvert.DeserializeObject<int[][]>(User.SkillData);
			newPlayer.x = 325;
			newPlayer.y = 310;
			newPlayer.angle = 'u';
			return newPlayer;
		}
		#endregion
		#region ОТКЛЮЧЕНИЕ
		public override Task OnDisconnected()// дисконнект конкретного игрока с задержкой 7 секунд
		{
			if (_broadcaster.players.Any(p => p.ConnectionId == Context.ConnectionId))
			{
				LeavePlayers.Add(_broadcaster.players.First(p => p.ConnectionId == Context.ConnectionId));
				Thread mythread = new Thread(WaitLeavePlayerTimer);
				mythread.Start(LeavePlayers.First(p => p.ConnectionId == Context.ConnectionId).Id);
				//_logger.Log("OnDisconnected({0})", Context.ConnectionId);
				Clients.All.playerleave(_broadcaster.players.First(p => p.ConnectionId == Context.ConnectionId).Id);
				_broadcaster.players.Remove(_broadcaster.players.First(p => p.ConnectionId == Context.ConnectionId));
			}
				return base.OnDisconnected();
			
		}
		#endregion
		#region ОЖИДАНИЕ ОТКЛЮЧЕННОГО ИГРОКА
		List<PlayerModel> LeavePlayers = new List<PlayerModel>();
		void WaitLeavePlayerTimer(object ido)
		{
			int id = int.Parse(ido.ToString());//преобразование объекта в строку

			Thread.Sleep(20*1000);//время для переподключения, если не успеваем, проебываем все что можно(в секундах)
			if (LeavePlayers.Any(p => p.Id == id))
			{
				//и тут надо будет вызвать метод который создает бокс
				LeavePlayers.Remove(LeavePlayers.First(p => p.Id == id));
				
			}
			Thread.CurrentThread.Abort();
		}
		#endregion
		#endregion
		#region КОНТРОЛЛЕР
		public void SendPos(int x,int y)
		{
			if (Broadcaster.Instance.players.Any(p => p.ConnectionId == Context.ConnectionId))
			{
				PlayerModel player = Broadcaster.Instance.players.First(p => p.ConnectionId == Context.ConnectionId);
				if (player.x != x || player.y != y)
				{
					SendPosition(player.x, player.y);
				}
			}

		}
		void SendPosition(int x, int y)
		{
			Vec2 SpawnPos = new Vec2();
			SpawnPos.x = x;
			SpawnPos.y = y;
			Clients.Client(Context.ConnectionId).Pos(SpawnPos);
		}
		public void ChangePos(char param, char dir)
		{
			if (Broadcaster.Instance.players.Any(p => p.ConnectionId == Context.ConnectionId))
			{
				PlayerModel player = Broadcaster.Instance.players.First(p => p.ConnectionId == Context.ConnectionId);

				if (param == 'm')
				{
					switch (dir)
					{
						case 'U':
							if (_broadcaster.chunk.WorldMap[player.x, player.y + 1] < 6)
							{
								player.y += 1;
								_broadcaster.skill.PrSkill(0, Context.ConnectionId);
							}
							player.angle = 'u';
							break;
						case 'D':
							if (_broadcaster.chunk.WorldMap[player.x, player.y - 1] < 6)
							{
								player.y -= 1;
								_broadcaster.skill.PrSkill(0, Context.ConnectionId);
							}
							player.angle = 'd';
							break;
						case 'R':
							if (_broadcaster.chunk.WorldMap[player.x + 1, player.y] < 6)
							{
								player.x += 1;
								_broadcaster.skill.PrSkill(0, Context.ConnectionId);
							}
							player.angle = 'r';
							break;
						case 'L':
							if (_broadcaster.chunk.WorldMap[player.x - 1, player.y] < 6)
							{
								player.x -= 1;
								_broadcaster.skill.PrSkill(0, Context.ConnectionId);
							}
							player.angle = 'l';
							break;
					}
				}
				if (param == 'r')
				{
					switch (dir)
					{
						case 'U':
							player.angle = 'u';
							break;
						case 'D':
							
							player.angle = 'd';
							break;
						case 'R':
							
							player.angle = 'r';
							break;
						case 'L':
							player.angle = 'l';
							break;
					}
				}
				if (param == 'o')
				{
					player.y -= 2;
					player.angle = 'd';
				}
				Broadcaster.Instance.players.First(p => p.ConnectionId == Context.ConnectionId).x = player.x;
				Broadcaster.Instance.players.First(p => p.ConnectionId == Context.ConnectionId).y = player.y;
			}
				
			
		}
		#endregion
	}
}
