
using System.Collections.Generic;
using System.Linq;
//using System.Web;
using xNet;
using Newtonsoft.Json;
namespace Server
{

    public class VkAPI
    {
        private const string __APPID = "6427427";  //ID приложения
        private const string __VKAPIURL = "https://api.vk.com/method/";  //Ссылка для запросов
        private const string __VKAPITURL = "https://oauth.vk.com/authorize?client_id=6427427&display=page&redirect_uri=https://oauth.vk.com/blank.html&scope=friends&response_type=token&v=5.52";  //Ссылка для запросов токена
        
        private string Token;  //Токен, использующийся при запросах

        public string GetToken()
        {
            HttpRequest GetToken = new HttpRequest();
            string Result = GetToken.Get(__VKAPITURL).ToString(); 
            char[] Symbols = { '=', '&' };
            string[] URL = Result.Split(Symbols);
            
            Token=URL[1].ToString();
            return Token;
            //
        }


        public Dictionary<string,string> GetID()  //Получение заданной информации о пользователе с заданным ID 
        {
            HttpRequest GetID = new HttpRequest();
            //GetID.AddUrlParam("user_ids", UserId);
            GetID.AddUrlParam("access_token", Token);
            

            GetID.AddUrlParam("version", "5.73");
            string Result = GetID.Get(__VKAPIURL + "users.get").ToString();
            Result = Result.Substring(13, Result.Length - 15);
            Dictionary<string, string> Dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(Result);
            return Dict;
        }
    }

}
