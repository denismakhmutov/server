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
#region не тограем
namespace Server
{
	public class Chunk
	{
		#endregion
		#region Настройка хаба
		private readonly IHubContext hubContext = GlobalHost.ConnectionManager.GetHubContext<Game>();
		#endregion
		bool USE_GEOLOGY = true;
		#region ПЕРЕМЕННЫЕ

		const int chanksX = 64;
		const int chanksY = 64;
		[System.NonSerialized]
		public int chanks_X = chanksX;
		[System.NonSerialized]
		public int chanks_Y = chanksY;

		Image mapFile = Image.FromFile(@"C:\test.png");//подгрузка файла карты
													//Битмап, для использования GetPixel
		Bitmap mapMainBitMap;

		//Массив чанков 32:32. Используется для более экономного использования памяти, чем если бы использовался массив с заранее определенными ячейками,
		//т.к. многие всё равно не будут использованы.
		public byte[,][,] chankMap = new byte[chanksX, chanksY][,];//Старая карта
		public byte[,] WorldMap = new byte[chanksX * 32, chanksY * 32];//Карта мира
		public byte[,] StrMap = new byte[chanksX * 32, chanksY * 32];//Карта прочности



		//Массив списков зданий, установленных в чанки с соответствующими координатами
		public List<BuildingData>[,] chankBuildingsDataArray = new List<BuildingData>[chanksX, chanksY];
		#endregion
		#region ГЕОЛОГИЯ
		byte[,] roadMap = new byte[chanksX * 32, chanksY * 32];//ГОРОД ДОРОГ
		byte[,] UpdatedChunks = new byte[chanksX, chanksY];//ГОРОД ДОРОГ
		bool[,] liveChunks = new bool[chanksX, chanksY];//ЖИВЫЕ ЧАНКИ
		byte[,] LivCrysRadiation = new byte[chanksX, chanksY];
		Timer GeologyTimer;
		Timer JivkiTimer;
		private readonly TimeSpan GeologyInterval = TimeSpan.FromMilliseconds(500);
		private readonly TimeSpan JivkiInterval = TimeSpan.FromMilliseconds(500);
		GeologyFallingBlocks geologyFallingBlocks;
		GeologyLivingCrystalls geologyLivingCrystalls;//класс с живками
		void Jivki_Update(object state)
		{
			geologyLivingCrystalls.Update();
			for (int x = 0; x < chanksX; x++)
			{
				for (int y = 0; y < chanksY; y++)
				{
					if (UpdatedChunks[x, y] != 0)
					{
						UpdateMapMark(x, y, UpdatedChunks[x, y]);
						//Broadcaster.Instance.chankMapCopy[x, y] = UpdatedChunks[x, y];
						UpdatedChunks[x, y] = 0;
					}
				}
			}
		}
		void Geo_Update(object state)
		{
			geologyFallingBlocks.Update();
			for (int x = 0; x < chanksX; x++)
			{
				for (int y = 0; y < chanksY; y++)
				{
					if (UpdatedChunks[x, y] != 0)
					{
						UpdateMapMark(x, y, UpdatedChunks[x, y]);
						//Broadcaster.Instance.chankMapCopy[x, y] = UpdatedChunks[x, y];
						UpdatedChunks[x, y] = 0;
					}
				}
			}
			//UpdatedChunks = new byte[chanksX,chanksY];
		}
		void UpdateMapMark(int x,int y,byte value)
		{
			Broadcaster.Instance.chankMapCopy[x, y] = value;
		}
		#endregion

		#region Подгрузка мира из файла
		public void MapLoadFromFile()
		{
			

			mapMainBitMap = new Bitmap(mapFile);//Определяем Битмап из файла карты
			#region Какой то ебаный колор для считывания каналов в пнг 
			Color[,] colorIDs = new Color[chanksX * 32, chanksY * 32];
			int mX = chanksX * 32;
			int mY = chanksY * 32;
			for (int y = 0; y < (chanksY * 32); y++)
				for (int x = 0; x < (chanksX * 32); x++)
					colorIDs[x, y] = mapMainBitMap.GetPixel(x, mY - y - 1);
			#endregion

			#region Двоичная подгрузка в рот она ебись 
			for (int y = 0; y < mY; y++)
				for (int x = 0; x < mX; x++)
				{
					roadMap[x, y] = 1;
					if (colorIDs[x, y].B < 6)
					{
						WorldMap[x, y] = 1;
						roadMap[x, y] = colorIDs[x, y].B;
					}
					else
					{
						WorldMap[x, y] = colorIDs[x, y].B;
					}
					StrMap[x, y] = colorIDs[x, y].R;
				}

			#endregion
			#region Старый четырехмерный массив с которым я так не хотел расставаться RIP(Земля ему пухом)
			//for (int y = 0; y < chanksY; y++)
			//    for (int x = 0; x < chanksX; x++)
			//    {
			//        chankMap[x, y] = new byte[32, 32];
			//        for (int y2 = 0; y2 < 32; y2++)
			//            for (int x2 = 0; x2 < 32; x2++)
			//                chankMap[x, y][x2, y2] = (byte)(colorIDs[x * 32 + x2, y * 32 + y2].B);//* 256);
			//    }
			#endregion
			// chankMapCopy = chankMap;
			if (USE_GEOLOGY)
			{
				#region Еще кусок геологии
				#region Заполняем ливчанки
				for (int x = 0; x < chanksX; x++)
					for (int y = 0; y < chanksY; y++)
					{
						if (x == 31 || y == 0 || y == 31)
							liveChunks[x, y] = false;
						else
							liveChunks[x, y] = true;
					}
				#endregion
				geologyFallingBlocks = new GeologyFallingBlocks(ref WorldMap, ref roadMap, ref liveChunks, chanksX, chanksY, ref UpdatedChunks);
				geologyLivingCrystalls = new GeologyLivingCrystalls(ref WorldMap, ref StrMap, ref liveChunks, chanksX, chanksY, ref UpdatedChunks, ref LivCrysRadiation);
				GeologyTimer = new Timer(Geo_Update, null, GeologyInterval, GeologyInterval);
				JivkiTimer = new Timer(Jivki_Update, null, JivkiInterval, JivkiInterval);
				#endregion
			}
			#region Запуск Таймера сохранения (пока отключил)
			SaveWorld = new Timer(SaveMap, null, SaveWorldInterval, SaveWorldInterval);
			#endregion
		}
		#endregion
		#region Отправка чанков
		#region отправка обновленного чанка RIP(Земля ему пухом)
		//public void SendChunk1(string Cid, int x, int y)//connection id 1 убрать добавил для тестов версии 2 0
		//{


		//    ChunkModel SyncChunk = new ChunkModel();

		//    SyncChunk.x = x / 32;
		//    SyncChunk.y = y / 32;


		//        SyncChunk.data = new List<string>(32);
		//        for (int k = 0; k < 32; k++)
		//        {
		//            string result = null;
		//            for (int i = 0; i < 32; i++)
		//            {
		//                if (i == 0)
		//                    result = JsonConvert.SerializeObject(chankMap[SyncChunk.x, chanks_Y - SyncChunk.y - 1][k, i]);
		//                else
		//                    result += "," + JsonConvert.SerializeObject(chankMap[SyncChunk.x, chanks_Y - SyncChunk.y - 1][k, i]);
		//                // SyncChunk.data.Add(JsonConvert.SerializeObject(chankMap[x / 32, y / 32][0, i]));
		//            }
		//            SyncChunk.data.Add(result);




		//        //chankMapCopy[SyncChunk.x, SyncChunk.y] = chankMap[SyncChunk.x, SyncChunk.y];
		//        }
		//    hubContext.Clients.Client(Cid).debug(SyncChunk);
		//}
		#endregion
		#region отправка обновленного чанка версия два ноль
		public void SendChunk(string Cid, int x, int y)//connection id
		{


			ChunkModel SyncChunk = new ChunkModel();

			SyncChunk.x = x / 32;
			SyncChunk.y = y / 32;
			byte[,] chunk = new byte[32, 32];
			for (int xc = 0; xc < 32; xc++)
			{
				for (int yc = 0; yc < 32; yc++)
				{
					
					if (WorldMap[SyncChunk.x * 32 + xc, SyncChunk.y * 32 + yc] < 6)
					{
						chunk[xc, yc] = roadMap[SyncChunk.x * 32 + xc, SyncChunk.y * 32 + yc];
					}
					else
					{
						chunk[xc, yc] = WorldMap[SyncChunk.x * 32 + xc, SyncChunk.y * 32 + yc];
					}
				}
			}
			SyncChunk.data = new List<byte[]>(32);
			for (int yc = 0; yc < 32; yc++)
			{
				byte[] bufferchunk = new byte[32];
				for (int xc = 0; xc < 32; xc++)
				{
					bufferchunk[xc] = chunk[xc, yc];
				}
				SyncChunk.data.Add(bufferchunk);
			}



			//chankMapCopy[SyncChunk.x, SyncChunk.y] = chankMap[SyncChunk.x, SyncChunk.y];

			hubContext.Clients.Client(Cid).debug(SyncChunk);
		}
		#endregion
		#region Отправка запрошенного чанка
		public void SendGetChunk(string Cid, int x, int y)//connection id
		{


			ChunkModel SyncChunk = new ChunkModel();

			SyncChunk.x = x / 32;
			SyncChunk.y = y / 32;
			byte[,] chunk = new byte[32, 32];
			for (int xc = 0; xc < 32; xc++)
			{
				for (int yc = 0; yc < 32; yc++)
				{
					if (WorldMap[SyncChunk.x * 32 + xc, SyncChunk.y * 32 + yc] < 6)
					{
						chunk[xc, yc] = roadMap[SyncChunk.x * 32 + xc, SyncChunk.y * 32 + yc];
					}
					else
					{
						chunk[xc, yc] = WorldMap[SyncChunk.x * 32 + xc, SyncChunk.y * 32 + yc];
					}
				}
			}
			SyncChunk.data = new List<byte[]>(32);
			for (int yc = 0; yc < 32; yc++)
			{
				byte[] bufferchunk = new byte[32];
				for (int xc = 0; xc < 32; xc++)
				{
					bufferchunk[xc] = chunk[xc, yc];
				}
				SyncChunk.data.Add(bufferchunk);
			}
			//SyncChunk.data = new List<string>(32);
			//for (int k = 0; k < 32; k++)
			//{
			//	string result = null;
			//	for (int i = 0; i < 32; i++)
			//	{
			//		if (i == 0)
			//			result = JsonConvert.SerializeObject(chunk[k, i]);
			//		else
			//			result += "," + JsonConvert.SerializeObject(chunk[k, i]);
			//		// SyncChunk.data.Add(JsonConvert.SerializeObject(chankMap[x / 32, y / 32][0, i]));
			//	}
			//	SyncChunk.data.Add(result);

			//SyncChunk.chunks = JsonConvert.SerializeObject(chunk);


			//	//chankMapCopy[SyncChunk.x, SyncChunk.y] = chankMap[SyncChunk.x, SyncChunk.y];
			//}
			//SyncChunk.chunk = new byte[32, 32];
			//SyncChunk.chunk = chunk;




			hubContext.Clients.Client(Cid).debug(SyncChunk);
		}
		#endregion
		#endregion
		#region Изменение чанков
		public void ChangeChunk(int x, int y, char param,string cid)
		{
			PlayerModel player = Broadcaster.Instance.players.First(p => p.ConnectionId == cid);
			if (param == 'd')
			{
				Broadcaster.Instance.players.First(p => p.ConnectionId == cid).Delay = 3;
				if (StrMap[x, y] > 0)
				{

					#region Кри
					int[] cribuf = player.cri;//переопределяем массив кри
					#region Зеленый
					if (WorldMap[x, y] == 12)//Зеленый
					{
						cribuf[0] += 1;
						StrMap[x, y] -= 1;

						Broadcaster.Instance.skill.PrSkill(18, cid);
						Broadcaster.Instance.skill.PrSkill(19, cid);
						Broadcaster.Instance.skill.PrSkill(13, cid);

					}
					#endregion
					#region Синий
					else if (WorldMap[x, y] == 13)//Синий
					{
						cribuf[1] += 1;
						StrMap[x, y] -= 1;

						Broadcaster.Instance.skill.PrSkill(18, cid);
						Broadcaster.Instance.skill.PrSkill(19, cid);
						Broadcaster.Instance.skill.PrSkill(13, cid);
					}
					#endregion
					#region Красный
					else if (WorldMap[x, y] == 14)//Красный
					{
						cribuf[2] += 1;
						StrMap[x, y] -= 1;

						Broadcaster.Instance.skill.PrSkill(18, cid);
						Broadcaster.Instance.skill.PrSkill(13, cid);
					}
					#endregion
					#region Белый
					else if (WorldMap[x, y] == 15)//Белый
					{
						cribuf[3] += 1;
						StrMap[x, y] -= 1;

						Broadcaster.Instance.skill.PrSkill(18, cid);
						Broadcaster.Instance.skill.PrSkill(13, cid);
					}
					#endregion
					#region Фиолетовый
					else if (WorldMap[x, y] == 16)//Фиол
					{
						cribuf[4] += 1;
						StrMap[x, y] -= 1;

						Broadcaster.Instance.skill.PrSkill(18, cid);
						Broadcaster.Instance.skill.PrSkill(13, cid);
					}
					#endregion
					#region Голубой
					else if (WorldMap[x, y] == 17)//Голубой
					{
						cribuf[5] += 1;
						StrMap[x, y] -= 1;

						Broadcaster.Instance.skill.PrSkill(18, cid);
						Broadcaster.Instance.skill.PrSkill(13, cid);
					}
					#endregion
					#endregion
					#region Сыпучки
					#region Пески
					if (WorldMap[x, y] == 23|| WorldMap[x, y] == 24|| WorldMap[x, y] == 25|| WorldMap[x, y] == 23)//Ебаные пески
					{
						StrMap[x, y] -= 1;
						Broadcaster.Instance.skill.PrSkill(10, cid);

					}
					#endregion
					#endregion
					player.cri = cribuf;
					Crisend(cribuf, cid);
				}
				if (StrMap[x, y] == 0)//Уничтожение блока
				{

					Broadcaster.Instance.skill.PrSkill(9, cid);

					WorldMap[x, y] = 1;//вносим изменения в мировую карту
					Broadcaster.Instance.chankMapCopy[x / 32, y / 32] = 1;//помечаем что чанк нужно обновить
				}
			}else if (param == 'g')
			{
				
				Broadcaster.Instance.players.First(p => p.ConnectionId == cid).Delay = 5;
				int geolvl = 10;//ТЕСТ
				if (player.Geo == null)
				{
					player.Geo = new List<GeoModel>();
				}
				if (WorldMap[x, y] != 1 && WorldMap[x, y] != 2 && WorldMap[x, y] != 3)
				{
					GeoModel Geobuffer = new GeoModel();
					Geobuffer.block = WorldMap[x, y];
					Geobuffer.density = StrMap[x, y];
					try
					{
						if (player.Geo.Count < geolvl)//проверка длинны листа с геоданными
						{
						//прячем
							player.Geo.Add(Geobuffer);
							WorldMap[x, y] = 1;
							StrMap[x, y] = 0;
							Broadcaster.Instance.chankMapCopy[x / 32, y / 32] = 1;
							Broadcaster.Instance.skill.PrSkill(5, cid);
						}
					}
					catch
					{
						//Тут какое нибудь сообщение на клиент
					}
				}
				else
				{
					try
					{
						//Ставим
						GeoModel Geobuffer = player.Geo.Last();
						WorldMap[x, y] = Geobuffer.block;
						StrMap[x, y] = Geobuffer.density;
						player.Geo.Remove(player.Geo.Last());
						Broadcaster.Instance.skill.PrSkill(5, cid);
						Broadcaster.Instance.chankMapCopy[x / 32, y / 32] = 1;
					}
					catch
					{
						//для дебага на клиент
					}
				}
			}
			try
			{
				Broadcaster.Instance.players.First(p => p.ConnectionId == cid).cri = player.cri;
				Broadcaster.Instance.players.First(p => p.ConnectionId == cid).Geo = player.Geo;
				Broadcaster.Instance.players.First(p => p.ConnectionId == cid).skillsInfoArray = player.skillsInfoArray;
			}
			catch
			{

			}
		}
		
			#region Ненужные функции

			//Broadcaster.Instance.chankMapCopy[px / 32, chanks_Y - py / 32 - 1] = 1;
			//if (py / 32 > y / 32)
			//{
			//	Broadcaster.Instance.chankMapCopy[x / 32, chanks_Y - y / 32 - 3] = 1;
			//}
			//else if (py / 32 < y / 32)
			//{
			//	Broadcaster.Instance.chankMapCopy[x / 32, chanks_Y - y / 32 + 1] = 1;
			//}

			//Broadcaster.Instance.chankMapCopy[x / 32 + 1, chanks_Y - y / 32 - 1] = null;
			//Broadcaster.Instance.chankMapCopy[x / 32 - 1, chanks_Y - y / 32 - 1] = null;
			//Broadcaster.Instance.chankMapCopy[x / 32, chanks_Y - y / 32] = null;
			//Broadcaster.Instance.chankMapCopy[x / 32, chanks_Y - y / 32 - 2] = null;
			//Broadcaster.Instance.chankMapCopy[x / 32, chanks_Y - y / 32 - 1] = chankMap[x / 32, chanks_Y - y / 32 - 1];
			#endregion
		
		#region Отправка кри
		void Crisend(int[] cri,string cid)
		{
			hubContext.Clients.Client(cid).CriSend(cri);
		}
		#endregion
		#endregion
		#region Сохранение карты в файл
		Timer SaveWorld;
		private readonly TimeSpan SaveWorldInterval = TimeSpan.FromSeconds(30);//пока каждые 30 сек
		public void SaveMap(object state)
		{

			//mapFile.Save(@"C:\2.png");
			foreach (var item in Broadcaster.Instance.players)
			{
				Broadcaster.Instance.Save(item);
			}

			Bitmap bmp = new Bitmap(chanksX*32,chanksY*32, PixelFormat.Format32bppPArgb);//определяем бмп с форматом множителя альфа канала
			#region Заполнение БМП
			for (int x = 0; x <chanks_X*32 ; x++)
			{
				for (int y = 0; y < chanksY*32; y++)
				{

					bmp.SetPixel(x, y, Color.FromArgb(255, StrMap[x, chanksY*32-1-y], roadMap[x,chanksY*32-y-1], WorldMap[x, chanksY*32-1- y]));//попиксельное заполнение бмп(y инвертирован)
				}
			}
			#endregion
			#region Сохренение
			mapFile = null;
			mapFile = bmp; //Приравниваем бмп к файлу
			//mapFile.Save(@"C:\test.png", ImageFormat.Png);//сохранение карты в файл
			#endregion

		}

		#endregion
		//Все работает на ебаном двухмерном массиве, пиздеже и костылях
#region Эт тоже не трогаем
	}
}
#endregion