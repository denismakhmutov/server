using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Models
{
    public class Infa
    {


        public class Vec2
        {
            float x { get; set; }
            float y { get; set; }

            public Vec2(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
        }
        public class Vec2i
        {
            float x { get; set; }
            float y { get; set; }

            public Vec2i(int x, int y)
            {
                this.x = x;
                this.y = y;
            }
        }
        public struct PersonInfo
        {

			public int[][] skillsInfoArray;//массив скиллов персонажа

			//координаты игрока в мире
			public Vec2i Coord { get; set; }

            public ulong Money { get; set; }
            public ulong Credits { get; set; }

            public ushort HP { get; set; }

            public int[] Cristalls { get; set; }
            public int corp { get; set; }

            public Vec2i respCoords { get; set; }
            public int respID { get; set; }

            public float[] skillsProperties { get; set; }
            public bool modsArray { get; set; }
            public short[] skinsArray { get; set; }
            public bool[] charactSettings { get; set; }
            public bool GodMode { get; set; }
            public bool ModerMode { get; set; }
            public uint bannedFromChat { get; set; }
            public uint bannedFromGame { get; set; }

            public List<Vec2i>[] charactBuildingsList { get; set; }//список координат зданий игрока для упрощения поиска их по миру 
            public List<ConstructData>[] characterInventoryList { get; set; }//массив предметов игрока

        }
        public class FramePattern
        {
            public byte[,] pattern { get; set; }
            public Vec2i angle { get; set; }

            public FramePattern(byte[,] pattern, Vec2i angle)
            {
                this.pattern = pattern;
                this.angle = angle;
            }
        }
        FramePattern[] framePattern = new FramePattern[5];
        void FramePatternFilling()
        {
            //0-не трогать,1 - фрейм здания, 2 - запретная зона

            //УЧИТЫВАТЬ, ЧТО В МАССИВАХ ОТОБРАЖЕНЫ СХЕМЫ, РАЗВЕРНУТЫЕ НА 90 ГРАДУСОВ ПО ЧАСОВОЙ СТРЕЛКЕ

            //лека, респаун,магазин бумов и диззов
            framePattern[0].pattern = new byte[4, 3] {
            {1,1,1},
            {1,2,1},
            {1,2,1},
            {0,2,0},
        };
            framePattern[0].angle = new Vec2i(-1, -1);

            //склад
            framePattern[1].pattern = new byte[3, 3] {
            {0,1,1},
            {2,2,1},
            {0,1,1},
        };
            framePattern[1].angle = new Vec2i(-1, -1);

            //Маркет, Пакс, Репакс, Экспрес
            framePattern[2].pattern = new byte[7, 7] {
            {0,0,0,2,0,0,0},
            {0,0,1,2,1,0,0},
            {0,1,1,2,1,1,0},
            {2,2,2,2,2,2,2},
            {0,1,1,2,1,1,0},
            {0,0,1,2,1,0,0},
            {0,0,0,2,0,0,0},
        };
            framePattern[2].angle = new Vec2i(-3, -3);

            //Ворота положение 1
            framePattern[3].pattern = new byte[3, 3] {
            {1,0,1},
            {1,1,1},
            {1,0,1},
        };
            framePattern[3].angle = new Vec2i(-1, -1);

            //Ворота положение 2
            framePattern[4].pattern = new byte[3, 3] {
            {1,1,1},
            {0,1,0},
            {1,1,1},
        };
            framePattern[4].angle = new Vec2i(-1, -1);

            //Пушка
            framePattern[5].pattern = new byte[5, 5] {
            {0,0,2,0,0},
            {0,1,2,1,0},
            {2,2,2,2,2},
            {0,2,1,2,0},
            {0,0,2,0,0},
        };
            framePattern[5].angle = new Vec2i(-2, -2);

            //АП, МОДС
            framePattern[6].pattern = new byte[3, 5] {
            {0,1,1,1,1},
            {2,2,2,1,1},
            {0,1,1,1,1},
        };
            framePattern[6].angle = new Vec2i(-1, -2);

            //Скинс
            framePattern[7].pattern = new byte[3, 4] {
            {0,1,1,1},
            {2,2,2,1},
            {0,1,1,1},
        };
            framePattern[7].angle = new Vec2i(-1, -2);

            //Башня клана
            framePattern[8].pattern = new byte[7, 7] {
            {0,0,0,2,0,0,0},
            {0,1,1,2,1,1,0},
            {0,1,1,2,1,1,0},
            {2,2,2,2,2,2,2},
            {0,1,1,2,1,1,0},
            {0,1,1,2,1,1,0},
            {0,0,0,2,0,0,0},
        };
            framePattern[8].angle = new Vec2i(-3, -3);

            //Фед здание
            framePattern[9].pattern = new byte[9, 5] {
            {1,1,1,0,0},
            {1,1,1,1,0},
            {1,1,1,1,1},
            {2,2,1,1,1},
            {2,2,2,2,1},
            {2,2,1,1,1},
            {1,1,1,1,1},
            {1,1,1,1,0},
            {1,1,1,0,0},
        };
            framePattern[9].angle = new Vec2i(-3, -4);

        }
    }
}
