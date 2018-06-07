using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MG
{
	//класс обработчика геологии типа падения сыпучек
	#region сыпучка
	public class GeologyFallingBlocks
	{
		// массив мира
		byte[,] Map;
		//слой покрытий. В нем хранится все, что являет собой проходимые персонажем блоки.
		//Возможно, в этот слой будут включены запретные зоны
		byte[,] roadMap;
		//обновленные чанки. нужно для того, чтобы сервер знал, какие чанки нужно разослать
		byte[,] UpdatedChunks;
		//массив живых чанков. в нем хранятся переменные, разрешающие или запрещающие обновление чанков.
		//В будущем надо будет переделять, чтобы в 1 переменной хранилось разрешение на 8 чанков,для экономии памяти, но это не обязательно
		bool[,] liveChunks;

		//кол-во чанков в массиве
		int chunksX;
		int chunksY;

		Random rand = new Random();

		public GeologyFallingBlocks(ref byte[,] Map, ref byte[,] roadMap, ref bool[,] liveChunks, int chunksX, int chunksY, ref byte[,] UpdatedChunks)
		{
			this.Map = Map;
			this.roadMap = roadMap;
			this.liveChunks = liveChunks;
			this.chunksX = chunksX;
			this.chunksY = chunksY;
			this.UpdatedChunks = UpdatedChunks;
		}
		// public  byte[,] updatedchunks = new byte[64, 64];
		static int ty;//текущий игрек для параллельного выполнения
		static int sx = 0;//стартовое значение для итератора

		// функция, которую на сервере будет вызывать другая функция с таймером
		public void Update()
		{
			for (ty = 0; ty < chunksY; ty++)
				for (sx = 0; sx < 2; sx++)
					Parallel.For(0, chunksX, ChunkFalling);
		}


		//функция обработки падения сыпучек внутри одного чанка.
		//нужно учесть, что при параллелизации надо будет расчитывать, чтобы одновременно обрабатывались только чанки, не имеющие
		//прямого контакта друг с другом, чтобы исключить дубликации или ошибки доступа
		//падение сыпучки в пределах одного чанка
		//функция НЕ ВЕДЕТ ПРОВЕРКИ НА НУЛ ИЛИ ВЫХОД ИЗ МАССИВА.КРАЙНИЕ ЧАНКИ НЕ ДОЛЖНЫ ВХОДИТЬ В СПИСОК ПРОВЕРКИ
		void ChunkFalling(int tx)
		{
			if (tx % 2 == sx)
				if (liveChunks[tx, ty])
				{
					int startX = tx * 32;
					int startY = ty * 32;

					int endX = startX + 32;
					int endY = startY + 32;

					bool isChangedChunk = false;//были ли изменения в чанке

					for (int y = startY; y < endY; ++y)//проход по чанку
						for (int x = startX; x < endX; ++x)
						{
							//ид блока,чтобы реже использовать это обращение к массиву
							byte block = Map[x, y];
							//18-26 - сыпучие породы
							//1-5 - земля и покрытия
							if (IsFallingBlock(block))//если это сыпучка
							{

								//чтобы не расчитывать координату лярд раз
								int blockBelow = Map[x, y - 1];//блок ниже

								//если это булыжник
								if (block > 18 && block < 22)
								{
									if (blockBelow < 6)
									{//если блок ниже это пустота
										Map[x, y] = 1;
										Map[x, y - 1] = block;
										UpdatedChunks[tx, ty] = 1;
										UpdatedChunks[x / 32, (y - 1) / 32] = 1;
									}
									else if (blockBelow < 18 || blockBelow > 26)
									{//если блок ниже не сыпучка
									 //забить хер
									}
									//если по сторонам есть куда сыпаться
									else if (Map[x - 1, y - 1] == 1 && Map[x - 1, y] == 1)//если блоки левей пустуют, идет проверка и на правые, чтобы выбрать, куда падать
									{
										if (Map[x + 1, y - 1] == 1 && Map[x + 1, y] == 1)
										{//если и правый равен пустоте

											if ((rand.Next() % 2) == 0)//выбор куда падатб
											{
												Map[x, y] = 1;
												Map[x - 1, y - 1] = block;
												UpdatedChunks[tx, ty] = 1;
												UpdatedChunks[(x - 1) / 32, (y - 1) / 32] = 1;
											}
											else
											{
												Map[x, y] = 1;
												Map[x + 1, y - 1] = block;
												UpdatedChunks[tx, ty] = 1;
												UpdatedChunks[(x + 1) / 32, (y - 1) / 32] = 1;
											}
										}
										else
										{//если нехуя не равен
											Map[x, y] = 1;
											Map[x - 1, y - 1] = block;
											UpdatedChunks[tx, ty] = 1;
											UpdatedChunks[(x - 1) / 32, (y - 1) / 32] = 1;
										}
									}
									else if (Map[x + 1, y + 1] == 1 && Map[x + 1, y] == 1)
									{//если левый не пустует
										Map[x, y] = 1;
										Map[x + 1, y - 1] = block;
										UpdatedChunks[tx, ty] = 1;
										UpdatedChunks[(x + 1) / 32, (y - 1) / 32] = 1;
									}
								}
								//если это пыль (логика немного проще)
								else if (block != 27)
								{
									if (blockBelow < 6)
									{//если блок ниже это пустота
										Map[x, y] = 1;
										Map[x, y - 1] = block;
										UpdatedChunks[tx, ty] = 1;
										UpdatedChunks[x / 32, (y - 1) / 32] = 1;
									}
									else if (blockBelow < 18 || blockBelow > 26)
									{//если блок ниже не сыпучка
									 //забить хер
									}
									//если по сторонам есть куда сыпаться
									else if (Map[x - 1, y - 1] == 1)
									{//если блок левей пустует, идет проверка и на правый, чтобы выбрать, куда падать
										if (Map[x + 1, y - 1] == 1)
										{//если и правый равен пустоте

											if ((rand.Next() % 2) == 0)//выбор куда падатб
											{
												Map[x, y] = 1;
												Map[x - 1, y - 1] = block;
												UpdatedChunks[tx, ty] = 1;
												UpdatedChunks[(x - 1) / 32, (y - 1) / 32] = 1;
											}
											else
											{
												Map[x, y] = 1;
												Map[x + 1, y - 1] = block;
												UpdatedChunks[tx, ty] = 1;
												UpdatedChunks[(x + 1) / 32, (y - 1) / 32] = 1;
											}
										}
										else
										{//если нехуя не равен
											Map[x, y] = 1;
											Map[x - 1, y - 1] = block;
											UpdatedChunks[tx, ty] = 1;
											UpdatedChunks[(x - 1) / 32, (y - 1) / 32] = 1;
										}
									}
									else if (Map[x + 1, y - 1] == 1)
									{//если левый не пустует
										Map[x, y] = 1;
										Map[x + 1, y - 1] = block;
										UpdatedChunks[tx, ty] = 1;
										UpdatedChunks[(x + 1) / 32, (y - 1) / 32] = 1;
									}
								}
								//если это слизь Активная
								//прилипает к поверхности, имея низкий шанс отклеяться,
								//может приклеяться к слизи с еще меньшим шансом
								else
								{
									//пока нихера
								}
							}
						}
				}

			//возвращает тру, если блок входит в список сыпучих
			bool IsFallingBlock(int block)
			{
				return (block > 17 && block < 28) ? true : false;
			}
			//возвращает тру, если блок входит в список пустых
			bool IsVoid(int block)
			{
				return (block < 5) ? true : false;
			}
		}
	}
	#endregion

	//класс обработчика геологии живых кристаллов
	#region живки
	public class GeologyLivingCrystalls
	{
		// массив мира
		byte[,] Map;
		// Карта плотности(прочности) кристаллов. Используется еще как карта "плодовитости" живых кристаллов
		byte[,] MapDensity;
		//обновленные чанки. нужно для того, чтобы сервер знал, какие чанки нужно разослать
		byte[,] UpdatedChunks;
		//массив живых чанков. в нем хранятся переменные, разрешающие или запрещающие обновление чанков.
		//В будущем надо будет переделять, чтобы в 1 переменной хранилось разрешение на 8 чанков,для экономии памяти, но это не обязательно
		bool[,] liveChunks;

		//на самом деле это не радиациа, а излучение другого типа, но так проще в понимании.
		//массив хранит суммы излечений живок в чанке. сумма излучения чанка равна кол-ву живок перемноженых на их рейты радиации
		//радиация чанка делится на 256 при сохранении в массив, и используется в виде бита
		byte[,] LivCrysRadiation;

		//кол-во чанков в массиве
		int chunksX;
		int chunksY;

		//шаг геологии
		//0-й - Прохождение всех живых чанков, построение карты радиации, приплод живок
		//1-й - Прохождение только по активным радиационным чанкам, удаление рандомных жив в чанках с повышенной радиацией,приплод живок
		int step;

		Random rand = new Random();

		//28/радиации нет/скала (АКТИВНАЯ)
		//29/0/ЧЕРНАЯ живка
		//30/1/СИНЯЯ живка
		//31/2/КРАССНАЯ живка
		//32/3/БЕЛАЯ живка
		//33/4/ФИОЛЕТОВАЯ живка
		//34/5/ГОЛУБАЯ живка
		//35/6/РАДУЖНАЯ живка
		//Сила радиации отдельных живок
		byte[] radiationRate = new byte[] {
			2,1,1,3,6,7,10
		};
		//при максимальной плотности максимально рад-х живок выходит 10240 единицы радиации (что превращается в 102 единицы радиации,т.к. число делится на 100)
		//при радиации больше 12 живки перестают плодить
		byte maxRad = 12;

		byte[] cryDensity = new byte[] {//прочность кристаллов от живок (скалы тоже)
			5,3,10,6,15,4,3,3
		};

		#region Конструктор
		public GeologyLivingCrystalls(ref byte[,] Map, ref byte[,] MapDensity, ref bool[,] liveChunks, int chunksX, int chunksY, ref byte[,] UpdatedChunks, ref byte[,] LivCrysRadiation)
		{
			this.Map = Map;
			this.MapDensity = MapDensity;
			this.liveChunks = liveChunks;
			this.chunksX = chunksX;
			this.chunksY = chunksY;
			this.UpdatedChunks = UpdatedChunks;
			this.LivCrysRadiation = LivCrysRadiation;
		}
		#endregion

		#region запуск циклов
		static int ty;//текущий игрек для параллельного выполнения
		static int sx = 0;//стартовое значение для итератора
						  //обновление геологии
		public void Update()
		{
			if (step == 0)
			{
				for (ty = 0; ty < chunksY; ty++)
					for (sx = 0; sx < 2; sx++)
						Parallel.For(0, chunksX, FrstStep);
			}
			else
			{ //второй шаг повторяется на много чаще, потому что проверка на новые жэивки слишком дорогая и лишний расход мощности процессора не к чему
				for (ty = 0; ty < chunksY; ty++)
					for (sx = 0; sx < 2; sx++)
						Parallel.For(0, chunksX, ScndStep);
			}

			step = (step + 1) % 5;//переход на следующий шаг
		}

		//первый шаг геологического процесса
		void FrstStep(int tx)
		{
			if (liveChunks[tx, ty])
			{
				if (tx % 2 == sx)
				{
					LivCrysGrowth(tx, ty, 0);
				}
			}
		}

		//второй шаг геологического процесса
		void ScndStep(int tx)
		{
			if (liveChunks[tx, ty])
			{
				if (LivCrysRadiation[tx, ty] <= maxRad)
				{
					if (tx % 2 == sx)
					{
						LivCrysGrowth(tx, ty, 1);
					}
				}
			}
		}
		#endregion

		//приплод живых кристаллов в чанке
		void LivCrysGrowth(int chX, int chY, int step)
		{

			//переменная - метка. Показывает, было ли изменение в чанке
			bool chunIskUpdated = false;
			//полная радиация живок
			//в конце цикла поделится на 256
			short fullLivCryRad = 0;
			//буферная переменная. используется для расчета роста кристаллов,т.к. LivCrysRadiation может быть равным нулю из-за работы 1-го шага
			//
			byte LivCryRadBuf = LivCrysRadiation[chX, chY];

			if (step == 0)
			{
				LivCrysRadiation[chX, chY] = 0;
			}

			//получение реальной координаты чанка
			int startY = chY * 32;
			int startX = chX * 32;

			#region выполнение действий в зависимости от живки
			for (int y = startY; y < (startY + 32); y++)
				for (int x = startX; x < (startX + 32); x++)
				{
					switch (Map[x, y])
					{
						case 28://Живая скала
							++fullLivCryRad;
							#region Живая скала
							if (rand.Next(100) < 10)
							{
								for (int j = y - 1; j < (y + 2); j++)
									for (int i = x - 1; i < (x + 2); i++)
									{
										if (Map[i, j] < 6)
											if (33 < rand.Next(100))
											{
												Map[i, j] = 12;
												MapDensity[i, j] = cryDensity[0];
												chunIskUpdated = true;
											}
									}
							}
							#endregion
							; break;
						case 29://Чёрная живка
							fullLivCryRad += radiationRate[0];
							#region Чёрная живка
							if (rand.Next(100) < MapDensity[x, y])
							{

								//если по вертикали только один из блоков равен 29
								//(тут тру будет только если один из блоков равен 29)
								if ((Map[x, y + 1] == 29) == (Map[x, y - 1] != 29))
								{
									//тут будет тру только если оба блока по горизонтали не равны или равны живке
									if ((Map[x + 1, y] == 29) == (Map[x - 1, y] == 29))
									{
										//теперь определяется, какая из сторон на вертикали без живки
										if (Map[x, y + 1] != 29)
										{
											if (Map[x, y + 1] < 6)
											{
												if (rand.Next(0, 2) == 0)
												{
													Map[x, y + 1] = 17;
												}
												else
												{
													Map[x, y + 1] = 14;
												}

												MapDensity[x, y + 1] = cryDensity[1];
												chunIskUpdated = true;
											}
										}
										else
										{
											if (Map[x, y - 1] < 6)
											{
												if (rand.Next(0, 2) == 0)
												{
													Map[x, y - 1] = 17;
												}
												else
												{
													Map[x, y - 1] = 14;
												}
												MapDensity[x, y - 1] = cryDensity[1];
												chunIskUpdated = true;
											}
										}
									}
								}
								//та же песня, но проверка на горизонтали
								//если по горизонтали только один из блоков равен 29
								//(тут тру будет только если один из блоков равен 29)
								else if ((Map[x + 1, y] == 29) == (Map[x - 1, y] != 29))
								{
									//тут будет тру только если оба блока по вертикали не равны или равны живке
									if ((Map[x, y + 1] == 29) == (Map[x, y - 1] == 29))
									{
										//теперь определяется, какая из сторон на горизонтали без живки
										if (Map[x + 1, y] != 29)
										{
											if (Map[x + 1, y] < 6)
											{
												if (rand.Next(0, 1) == 0)
												{
													Map[x + 1, y] = 17;
												}
												else
												{
													Map[x + 1, y] = 14;
												}
												MapDensity[x + 1, y] = cryDensity[1];
												chunIskUpdated = true;
											}
										}
										else
										{
											if (Map[x - 1, y] < 6)
											{
												if (rand.Next(0, 1) == 0)
												{
													Map[x - 1, y] = 17;
												}
												else
												{
													Map[x - 1, y] = 14;
												}
												MapDensity[x - 1, y] = cryDensity[1];
												chunIskUpdated = true;
											}
										}
									}
								}
								else if (rand.Next(0, 12) == 0)
								{
									byte blackCount = 0;
									for (int j = y - 1; j < (y + 2); j++)
										for (int i = x - 1; i < (x + 2); i++)
										{
											if (Map[i, j] == 29)
											{
												++blackCount;
											}
										}
									if (blackCount > 6)
									{
										Map[x, y] = 28;
										chunIskUpdated = true;
									}
								}

							}
							#endregion
							; break;
						case 30://Синяя живка
							fullLivCryRad += radiationRate[1];
							#region Синяя живка
							if (rand.Next(100) < MapDensity[x, y])
							{
								//0-вверх,1-право,2-низ,3-Лево

								byte[] arr = new byte[4];//массив направлений, в которых есть пустотка. Синяя живка в первую очередь поглощает пустотку
								byte index = 0;//максимальный индекс, в котором можно считать направление
								bool ind = false;//метка. Если Тру, то в массиве есть куда направляться, если фолз-нету
								if (Map[x + 1, y] == 10)
								{
									arr[index] = 1;
									index++;
									ind = true;
								}
								if (Map[x - 1, y] == 10)
								{
									arr[index] = 3;
									index++;
									ind = true;
								}
								if (Map[x, y + 1] == 10)
								{
									arr[index] = 0;
									index++;
									ind = true;
								}
								if (Map[x, y - 1] == 10)
								{
									arr[index] = 2;
									index++;
									ind = true;
								}

								//если в массиве есть направления с пустоткой, синяя живка переместится на место этой пустотки, оставив позади кристалл
								//с повышенной прочностью
								if (ind)
								{
									switch (arr[rand.Next(0, index - 1)])
									{
										case 0:
											Map[x, y + 1] = 30;
											MapDensity[x, y + 1] = MapDensity[x, y];
											; break;
										case 1:
											Map[x + 1, y] = 30;
											MapDensity[x + 1, y] = MapDensity[x, y];
											; break;
										case 2:
											Map[x, y - 1] = 30;
											MapDensity[x, y - 1] = MapDensity[x, y];
											; break;
										case 3:
											Map[x - 1, y] = 30;
											MapDensity[x - 1, y] = MapDensity[x, y];
											; break;
									}

									Map[x, y] = 13;
									MapDensity[x, y] = (byte)(cryDensity[2] * 5);
									chunIskUpdated = true;
								}
								else
								{
									bool move = false;
									switch (rand.Next(0, 3))
									{
										case 0:
											if (Map[x, y + 1] < 6)
											{
												if (Map[x, y + 1] < 6)
												{
													Map[x, y + 1] = 30;
													MapDensity[x, y + 1] = MapDensity[x, y];
													move = true;
												}
											}
											; break;
										case 1:
											if (Map[x + 1, y] < 6)
											{
												if (Map[x + 1, y] < 6)
												{
													Map[x + 1, y] = 30;
													MapDensity[x + 1, y] = MapDensity[x, y];
													move = true;
												}
											}
											; break;
										case 2:
											if (Map[x, y - 1] < 6)
											{
												if (Map[x, y - 1] < 6)
												{
													Map[x, y - 1] = 30;
													MapDensity[x, y - 1] = MapDensity[x, y];
													move = true;
												}
											}
											; break;
										case 3:
											if (Map[x - 1, y] < 6)
											{
												if (Map[x - 1, y] < 6)
												{
													Map[x - 1, y] = 30;
													MapDensity[x - 1, y] = MapDensity[x, y];
													move = true;
												}
											}
											; break;
									}

									if (move)
									{
										Map[x, y] = 13;
										MapDensity[x, y] = cryDensity[2];
										chunIskUpdated = true;
									}
								}
							}
							#endregion
							; break;
						case 31://Красная живка
							fullLivCryRad += radiationRate[2];
							#region Красная живка
							if (rand.Next(100) < MapDensity[x, y])
							{
								bool rock = false;
								for (int j = y - 1; j < (y + 2); j++)
									for (int i = x - 1; i < (x + 2); i++)
									{
										if (Map[i, j] == 7 || Map[i, j] == 28)
										{
											rock = true;
											break;
										}
									}

								if (rock)
								{
									if (Map[x + 1, y] < 6)
									{
										Map[x + 1, y] = 14;
										MapDensity[x + 1, y] = cryDensity[3];
										chunIskUpdated = true;
									}
									if (Map[x - 1, y] < 6)
									{
										Map[x - 1, y] = 14;
										MapDensity[x - 1, y] = cryDensity[3];
										chunIskUpdated = true;
									}
									if (Map[x, y + 1] < 6)
									{
										Map[x, y + 1] = 14;
										MapDensity[x, y + 1] = cryDensity[3];
										chunIskUpdated = true;
									}
									if (Map[x, y - 1] < 6)
									{
										Map[x, y - 1] = 14;
										MapDensity[x, y - 1] = cryDensity[3];
										chunIskUpdated = true;
									}
								}
							}
							#endregion
							; break;
						case 32://Белая живка
							fullLivCryRad += radiationRate[3];
							#region Белая живка
							if (Map[x, y + 1] == 18 || Map[x, y + 1] == 27)
							{
								if (rand.Next(100) < MapDensity[x, y])
								{
									if (Map[x, y + 1] == 18)
									{
										Map[x, y + 1] = 1;
										for (int j = y - 1; j < (y + 2); j++)
											for (int i = x - 1; i < (x + 2); i++)
											{
												if (Map[i, j] < 6 && rand.Next(0, 100) < 33)
												{
													Map[i, j] = 15;
													MapDensity[i, j] = cryDensity[4];
													chunIskUpdated = true;
												}
											}
									}
									else
									{
										Map[x, y + 1] = 1;
										for (int j = y - 1; j < (y + 2); j++)
											for (int i = x - 1; i < (x + 2); i++)
											{
												if (Map[i, j] < 6 && rand.Next(0, 100) < 10)
												{
													Map[i, j] = 32;
													MapDensity[i, j] = MapDensity[x, y];
													chunIskUpdated = true;
												}
											}
									}
								}
							}
							#endregion
							; break;
						case 33://Фиолетовая живка
							fullLivCryRad += radiationRate[4];
							#region Фиолетовая живка
							if (rand.Next(100) < MapDensity[x, y])
							{
								bool rock = false;
								for (int j = y - 1; j < (y + 2); j++)
									for (int i = x - 1; i < (x + 2); i++)
									{
										if (Map[i, j] == 7 || Map[i, j] == 28)
										{
											rock = true;
											break;
										}
									}

								if (rock)
								{
									if (Map[x + 1, y] < 6)
									{
										Map[x + 1, y] = 16;
										MapDensity[x + 1, y] = cryDensity[5];
										chunIskUpdated = true;
									}
									if (Map[x - 1, y] < 6)
									{
										Map[x - 1, y] = 16;
										MapDensity[x - 1, y] = cryDensity[5];
										chunIskUpdated = true;
									}
									if (Map[x, y + 1] < 6)
									{
										Map[x, y + 1] = 16;
										MapDensity[x, y + 1] = cryDensity[5];
										chunIskUpdated = true;
									}
									if (Map[x, y - 1] < 6)
									{
										Map[x, y - 1] = 16;
										MapDensity[x, y - 1] = cryDensity[5];
										chunIskUpdated = true;
									}
								}
							}
							#endregion
							; break;
						case 34://Голубая живка
							fullLivCryRad += radiationRate[5];
							#region Голубая живка
							if (rand.Next(100) < MapDensity[x, y])
							{
								if (Map[x + 1, y] < 6)
								{
									Map[x + 1, y] = 17;
									MapDensity[x + 1, y] = cryDensity[6];
									chunIskUpdated = true;
								}
								if (Map[x - 1, y] < 6)
								{
									Map[x - 1, y] = 17;
									MapDensity[x - 1, y] = cryDensity[6];
									chunIskUpdated = true;
								}
								if (Map[x, y + 1] < 6)
								{
									Map[x, y + 1] = 17;
									MapDensity[x, y + 1] = cryDensity[6];
									chunIskUpdated = true;
								}
								if (Map[x, y - 1] < 6)
								{
									Map[x, y - 1] = 17;
									MapDensity[x, y - 1] = cryDensity[6];
									chunIskUpdated = true;
								}
							}
							#endregion
							; break;
						case 35://Радужная живка
							fullLivCryRad += radiationRate[6];
							#region Радужная живка
							if (rand.Next(100) < MapDensity[x, y])
							{
								byte IDCry = 0;

								for (int j = y - 1; j < (y + 2); j++)//проверка на однородность соседей.Если вокруг радужки что-то лишнее, то она не плодит
									for (int i = x - 1; i < (x + 2); i++)
									{
										if (IDCry != 0)
										{
											if ((IDCry != Map[i, j]) && (Map[i, j] > 11) && (Map[i, j] < 18))
											{
												IDCry = 0;
												break;
											}
										}
										else
										{
											if ((Map[i, j] > 11) && (Map[i, j] < 18))
											{
												IDCry = Map[i, j];
											}
										}
									}

								if (IDCry != 0)
								{
									//радужка плодит во все стороны вокруг себя
									for (int j = y - 1; j < (y + 2); j++)
										for (int i = x - 1; i < (x + 2); i++)
										{
											if (Map[i, j] < 6)
											{
												Map[i, j] = IDCry;
												MapDensity[x, y] = cryDensity[7];
												chunIskUpdated = true;
											}
										}
								}
							}
							#endregion
					; break;
					}
				}
			#endregion
			//получение значения радиации для массива радиации
			if (step == 0)
			{
				LivCrysRadiation[chX, chY] = (byte)(fullLivCryRad / 100);
			}

			if (chunIskUpdated)
			{
				UpdatedChunks[chX, chY] = 1;
			}
		}

		#region бесполезные, но мало ли еще пригодятся, функции
		////крестообразный приплод
		//void Growth(int x,int y) {
		//	if (Map[x + 1, y] < 6) Map[x + 1, y] = Map[x, y];
		//	if (Map[x - 1, y] < 6) Map[x - 1, y] = Map[x, y];
		//	if (Map[x, y + 1] < 6) Map[x, y + 1] = Map[x, y];
		//	if (Map[x, y - 1] < 6) Map[x, y - 1] = Map[x, y];
		//}
		////крестообразный приплод с рандомным шансом спавна кристалла
		//void Growth(int x, int y,int chance)
		//{
		//	if (Map[x + 1, y] < 6) if (chance < rand.Next(100)) Map[x + 1, y] = Map[x, y];
		//	if (Map[x - 1, y] < 6) if (chance < rand.Next(100)) Map[x - 1, y] = Map[x, y];
		//	if (Map[x, y + 1] < 6) if (chance < rand.Next(100)) Map[x, y + 1] = Map[x, y];
		//	if (Map[x, y - 1] < 6) if (chance < rand.Next(100)) Map[x, y - 1] = Map[x, y];
		//}

		////приплод в указанном направлении (без проверки на место установки. Это надо делать перед вызовом этой функции)
		//void Growth(int x, int y, byte ID, char dir)
		//{
		//	switch (dir)
		//	{
		//		case 'r':
		//			Map[x + 1, y] = ID;
		//			; break;
		//		case 'l':
		//			Map[x - 1, y] = ID;
		//			; break;
		//		case 'u':
		//			Map[x, y + 1] = ID;
		//			; break;
		//		case 'd':
		//			Map[x, y - 1] = ID;
		//			; break;
		//	}
		//}

		////полный приплод с рандомным шансом спавна кристалла
		//void Growth2(int x, int y, int chance)
		//{
		//	for (int j = y - 1; j < (y + 2); j++)
		//		for (int i = x - 1; i < (x + 2); i++) {
		//			if (Map[i, j] < 6) if (chance < rand.Next(100)) Map[i, j] = Map[x, y];
		//		}
		//}

		////полный приплод , с указанием айдишника кристалла
		//void Growth2(int x, int y, byte ID)
		//{
		//	for (int j = y - 1; j < (y + 2); j++)
		//		for (int i = x - 1; i < (x + 2); i++)
		//		{
		//			if (Map[i, j] < 6) Map[i, j] = ID;
		//		}
		//}

		////полный приплод с рандомным шансом спавна кристалла, с указанием айдишника кристалла
		//void Growth2(int x, int y, int chance,byte ID)
		//{
		//	for (int j = y - 1; j < (y + 2); j++)
		//		for (int i = x - 1; i < (x + 2); i++)
		//		{
		//			if (Map[i, j] < 6) if (chance < rand.Next(100)) Map[i, j] = ID;
		//		}
		//}
		#endregion

	}
	#endregion
}
