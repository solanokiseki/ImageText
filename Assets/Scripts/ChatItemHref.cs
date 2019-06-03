using System.Collections.Generic;

namespace ImageText
{
    public class ChatItemHref
    {
        Dictionary<ChatHrefType, System.Action<string, string>> _callBacks = new Dictionary<ChatHrefType, System.Action<string, string>>();
        public void AddCallBack(ChatHrefType chatHrefType, System.Action<string, string> callBack)
        {
            if (!_callBacks.ContainsKey(chatHrefType))
            {
                _callBacks.Add(chatHrefType, callBack);
            }
            else
            {
                UnityEngine.Debug.LogError("has same key in ChatItemHref.AddCallBack()");
            }
        }

        public void OnHrefClick(string obj)
        {
            var strTpv = obj.Split(',');
            ChatHrefType cht = (ChatHrefType)int.Parse(strTpv[0]);

            if (_callBacks.ContainsKey(cht))
            {
                _callBacks[cht].Invoke(strTpv[1], strTpv[2]);
            }
        }
    }
}
