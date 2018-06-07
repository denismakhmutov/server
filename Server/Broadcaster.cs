#region Либы
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Newtonsoft.Json;
using System.Web;
using Models;
using System.Collections.Concurrent;
#endregion
namespace Server
{
	public class Broadcaster
	{
		#region ПЕРЕМЕННЫЕ
		private readonly static Lazy<Broadcaster> instance = new Lazy<Broadcaster>(() => new Broadcaster());
		private readonly TimeSpan BroadcastInterval = TimeSpan.FromMilliseconds(100);
		private readonly TimeSpan MapUpdateIntervar = TimeSpan.FromMilliseconds(100);
		private readonly TimeSpan ChangeIntervar = TimeSpan.FromMilliseconds(100);//Задержка --
		private readonly TimeSpan CheckConnectedPlayersInterval = TimeSpan.FromMilliseconds(5050);
		Timer checkplayersPos;
		Timer ChangeDalay;
		private readonly TimeSpan CheckPosInterval = TimeSpan.FromMilliseconds(500);//отправка позиции для синхронизации
		private readonly IHubContext hubContext;
		private Timer loop;
		private Timer MapUpdateTimer;
		//private Timer CheckConnectedPlayers;
		public Chunk chunk = new Chunk();//Класс отвечающий за весь перепиздец который связан с картой
		public Skill skill = new Skill();
		public bool nosend = false;
		private MoveModel model;
		//Потокозащищенные листы
		ConcurrentQueue<MoveModel> oneFrameModels = new ConcurrentQueue<MoveModel>();
		public ConcurrentQueue<PlayerModel> oneFrameSendGetMap = new ConcurrentQueue<PlayerModel>();
		public ConcurrentQueue<PlayerModel> ConnectedPlayersConnectionIds = new ConcurrentQueue<PlayerModel>();
		
		public List<PlayerModel> players = new List<PlayerModel>();
		public List<PlayerModel> addplayers = new List<PlayerModel>();

		//public  List<SendChunk> testsend = new List<SendChunk>();
		#endregion
		#region СТАРТАП
		public Broadcaster()
		{

			chunk.MapLoadFromFile();
			hubContext = GlobalHost.ConnectionManager.GetHubContext<Game>();
			model = new MoveModel();
			// modelUpdated = false;

			// chankMapCopy = chunk.chankMap;
			loop = new Timer(BroadcastModel, null, BroadcastInterval, BroadcastInterval);
			MapUpdateTimer = new Timer(UpdateMap, null, MapUpdateIntervar, MapUpdateIntervar);
			//checkplayersPos = new Timer(CheckPos, null, CheckPosInterval, CheckPosInterval);
			ChangeDalay = new Timer(ChangeDelayPlayers, null, ChangeIntervar, ChangeIntervar);
			//CheckConnectedPlayers = new Timer(CheckPlayers, null, CheckConnectedPlayersInterval, CheckConnectedPlayersInterval);
		}
		#endregion
		#region Задержка
		void ChangeDelayPlayers(object state)
		{
			foreach (var item in players)
			{
				if (item.Delay > 0)
					item.Delay--;
			}
		}

		#endregion
		#region ОТПРАВКА ПОЗИЦИЙ ИГРОКОВ В РАДИУСЕ
		public void BroadcastModel(object state)
		{
			// if (!nosend)
			{
				try
				{
					foreach (var item in players)
					{
						foreach (var SendPlayer in players.TakeWhile(x => x.x < item.x + 64 && x.x > item.x - 64 && x.y < item.y + 64 && x.y > item.y - 64))
						{
							//if (item.x+64  > SendPlayer.x && item.x-64 < SendPlayer.x &&
							//    item.y+64  > SendPlayer.y && item.y-64 < SendPlayer.y)

							//{

							if (item.ConnectionId != SendPlayer.ConnectionId)
								hubContext.Clients.Client(item.ConnectionId).updateModel(SendPlayer.x, SendPlayer.y,SendPlayer.angle, SendPlayer.Id);
							

							//}



						}


					}
				}
				catch { }
			}
			//oneFrameModels=new ConcurrentQueue<MoveModel>();


		}
		#endregion
		#region Синхронизация координат Отключенно
		/*
		void CheckPos(object state)
		{
			foreach (var item in players)
			{
			
				Vec2 SpawnPos = new Vec2();
				SpawnPos.x = item.x;
				SpawnPos.y = item.y;
				hubContext.Clients.Client(item.ConnectionId).Pos(SpawnPos);
			}
		}
		*/
		#endregion
		#region МУСОР НО УДАЛЯТЬ ПОКА РАНО, МОЖЕТ ПРИГОДИТСЯ
		public void UpdateModel(MoveModel p_model)
		{

			// oldpos = p_model;


			oneFrameModels.Enqueue(p_model);
			//if (preFrameModels.Count > 0)

			//checkposition();

			//preFrameModels = new ConcurrentQueue<MoveModel>();
			// preFrameModels = oneFrameModels;
		}
		public void RegisterModel(PlayerModel p_model)
		{
			//model = p_model;
			p_model.Id = players.Count();
			players.Add(p_model);
			hubContext.Clients.All.createModel(p_model);
			//modelUpdated = true;

		}
		public void AddPlayerInGame(PlayerModel playermodel)
		{
			int playerCount = players.Count();
			playermodel.Id = playerCount;
			players.Add(playermodel);
			foreach (var item in players)
			{
				hubContext.Clients.Client(playermodel.ConnectionId).createModel(item.x, item.y, item.Id);
			}

		}

		//private void checkposition()
		//{
		//    if(preFrameModels.FirstOrDefault().X+1<oneFrameModels.FirstOrDefault().X ||

		//        preFrameModels.FirstOrDefault().X - 1 > oneFrameModels.FirstOrDefault().X ||

		//       preFrameModels.FirstOrDefault().Y + 1 < oneFrameModels.FirstOrDefault().Y ||

		//       preFrameModels.FirstOrDefault().Y - 1 > oneFrameModels.FirstOrDefault().Y 
		//       )
		//    {
		//        oneFrameModels.FirstOrDefault().X = preFrameModels.FirstOrDefault().X;
		//        oneFrameModels.FirstOrDefault().Y = preFrameModels.FirstOrDefault().Y;
		//        hubContext.Clients.Client(oneFrameModels.FirstOrDefault().Auth).debug(preFrameModels.FirstOrDefault());



		//    }


		//}
		//public void restart()
		//{
		//    players = new ConcurrentQueue<PlayerModel>();
		//    registerModels = new ConcurrentQueue<MoveModel>();
		//    oneFrameModels = new ConcurrentQueue<MoveModel>();
		//    preFrameModels = new ConcurrentQueue<MoveModel>();
		//}
		#endregion
		#region ОБНОВЛЕНИЕ КАРТЫ
		public byte[,] chankMapCopy = new byte[64, 64];//пустая копия карты для определения обновленных чанков
		private void UpdateMap(object state)
		{
			int[] x = new int[2048];//макс кол-во чанков x
			int[] y = new int[2048];//макс кол-во чанков y
			int i = 0;// счетчик обновленных чанков
			try
			{
				foreach (var item in players)
				{
					#region 5x5 Рефактор
					#region 3x3 Рефактор
					if (chankMapCopy[item.x / 32, item.y / 32] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x, item.y);

						x[i] = item.x / 32;
						y[i] = item.y / 32;
						i++;
					}
					if (chankMapCopy[item.x / 32 + 1, item.y / 32 + 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 32, item.y + 32);
						x[i] = item.x / 32 + 1;
						y[i] = item.y / 32 + 1;
						i++;
					}
					//моя остановочка, отошел поссать
					if (chankMapCopy[item.x / 32 - 1, item.y / 32 - 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 32, item.y - 32);
						x[i] = item.x / 32 - 1;
						y[i] = item.y / 32 - 1;
						i++;
					}
					if (chankMapCopy[item.x / 32 + 1, item.y / 32 - 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 32, item.y - 32);
						x[i] = item.x / 32 + 1;
						y[i] = item.y / 32 - 1;
						i++;
					}
					//отошел покурить, ебать меня колбасит
					if (chankMapCopy[item.x / 32 - 1, item.y / 32 + 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 32, item.y + 32);
						x[i] = item.x / 32 - 1;
						y[i] = item.y / 32 + 1;
						i++;
					}
					if (chankMapCopy[item.x / 32 + 1, item.y / 32] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 32, item.y);
						x[i] = item.x / 32 + 1;
						y[i] = item.y / 32;
						i++;
					}
					if (chankMapCopy[item.x / 32 - 1, item.y / 32] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 32, item.y);
						x[i] = item.x / 32 - 1;
						y[i] = item.y / 32;
						i++;
					}
					//до сюда готово
					if (chankMapCopy[item.x / 32, item.y / 32 + 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x, item.y + 32);
						x[i] = item.x / 32;
						y[i] = item.y / 32 + 1;
						i++;
					}
					if (chankMapCopy[item.x / 32, item.y / 32 - 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x, item.y - 32);
						x[i] = item.x / 32;
						y[i] = item.y / 32 - 1;
						i++;
					}
					#endregion
					//ТУТ НАЧАЛО 5х5 РАЗМЕТКИ
					if (chankMapCopy[item.x / 32 + 2, item.y / 32] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 64, item.y);
						x[i] = item.x / 32 + 2;
						y[i] = item.y / 32;
						i++;
					}
					if (chankMapCopy[item.x / 32 + 2, item.y / 32 - 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 64, item.y - 32);
						x[i] = item.x / 32 + 2;
						y[i] = item.y / 32 - 1;
						i++;
					}
					if (chankMapCopy[item.x / 32 + 2, item.y / 32 - 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 64, item.y - 64);
						x[i] = item.x / 32 + 2;
						y[i] = item.y / 32 - 2;
						i++;
					}
					if (chankMapCopy[item.x / 32 + 2, item.y / 32 + 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 64, item.y + 32);
						x[i] = item.x / 32 + 2;
						y[i] = item.y / 32 + 1;
						i++;
					}
					if (chankMapCopy[item.x / 32 + 2, item.y / 32 + 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 64, item.y + 64);
						x[i] = item.x / 32 + 2;
						y[i] = item.y / 32 + 2;
						i++;
					}
					if (chankMapCopy[item.x / 32 - 2, item.y / 32 + 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 64, item.y + 64);
						x[i] = item.x / 32 - 2;
						y[i] = item.y / 32 + 2;
						i++;
					}
					if (chankMapCopy[item.x / 32 - 2, item.y / 32 + 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 64, item.y + 32);
						x[i] = item.x / 32 - 2;
						y[i] = item.y / 32 + 1;
						i++;
					}
					if (chankMapCopy[item.x / 32 - 2, item.y / 32] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 64, item.y);
						x[i] = item.x / 32 - 2;
						y[i] = item.y / 32;
						i++;
					}
					if (chankMapCopy[item.x / 32 - 2, item.y / 32 - 1] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 64, item.y - 32);
						x[i] = item.x / 32 - 2;
						y[i] = item.y / 32 - 1;
						i++;
					}
					if (chankMapCopy[item.x / 32 - 2, item.y / 32 - 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 64, item.y - 64);
						x[i] = item.x / 32 - 2;
						y[i] = item.y / 32 - 2;
						i++;
					}
					if (chankMapCopy[item.x / 32 - 1, item.y / 32 + 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 32, item.y + 64);
						x[i] = item.x / 32 - 1;
						y[i] = item.y / 32 + 2;
						i++;
					}
					if (chankMapCopy[item.x / 32, item.y / 32 + 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x, item.y + 64);
						x[i] = item.x / 32;
						y[i] = item.y / 32 + 2;
						i++;
					}
					if (chankMapCopy[item.x / 32 + 1, item.y / 32 + 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 32, item.y + 64);
						x[i] = item.x / 32 + 1;
						y[i] = item.y / 32 + 2;
						i++;
					}
					if (chankMapCopy[item.x / 32, item.y / 32 - 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x, item.y - 64);
						x[i] = item.x / 32;
						y[i] = item.y / 32 - 2;
						i++;
					}
					if (chankMapCopy[item.x / 32 + 1, item.y / 32 - 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x + 32, item.y - 64);
						x[i] = item.x / 32 + 1;
						y[i] = item.y / 32 - 2;
						i++;
					}
					if (chankMapCopy[item.x / 32 - 1, item.y / 32 - 2] > 0)
					{

						chunk.SendChunk(item.ConnectionId, item.x - 32, item.y - 64);
						x[i] = item.x / 32 - 1;
						y[i] = item.y / 32 - 2;
						i++;
					}
					#endregion
					//chankMapCopy[item.x / 32, 64 - item.y / 32 - 1] = new byte[32, 32];
					//chankMapCopy[item.x / 32, 63 - item.y / 32] = chunk.chankMap[item.x / 32, 63 - item.y / 32];

				}
				for (int k = 0; k < i; k++)
				{
					chankMapCopy[x[k], y[k]] = 0;
				}
				foreach (var item in oneFrameSendGetMap)//Отправка запрошенной карты
				{
					chunk.SendGetChunk(item.ConnectionId, item.x * 32, item.y * 32);

				}
			}
			catch { }
			oneFrameSendGetMap = new ConcurrentQueue<PlayerModel>();
		}
		#endregion
		#region ПОЛУЧЕНИЕ КАРТЫ
		public void GetChunk(string cid, int x, int y)
		{
			PlayerModel oneChunk = new PlayerModel();
			oneChunk.x = x / 32;
			oneChunk.y = y / 32;
			oneChunk.ConnectionId = cid;
			oneFrameSendGetMap.Enqueue(oneChunk);
			//if (oneFrameSendGetMap.Any(p => p.x != oneChunk.x&&p.y!=oneChunk.y))

			// else
			{
				//иначе просто идешь нахуй, хацкер ипаный
			}
			// oneFrameSendGetMap.Enqueue(oneChunk);
		}
		#endregion
		#region ТУТ БУДЕТ АНТИЧИТ
		//Я тебя наебал, он уже в другом месте
		#endregion
		#region ИНСТАНС
		public static Broadcaster Instance
		{
			get
			{
				return instance.Value;
			}
		}

		#endregion
		#region Сохранение данных игрока
		BDDataContext BD = new BDDataContext();
		public void Save(PlayerModel player)
		{
			var User = (from u in BD.UsersData where u.Id == player.Id select u).ToArray();
			if (User.Length > 0)// Тут еще проверочку на пасс
			{
				User[0].SkillData = JsonConvert.SerializeObject(player.skillsInfoArray);

			}
			BD.SubmitChanges();
		}
		#endregion
	}
}