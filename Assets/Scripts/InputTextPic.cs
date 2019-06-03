using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using System;
namespace ImageText
{
    public enum ChatHrefType
    {
        CHT_ShowItem,
        CHT_ShowEquip,
        CHT_JoinTeam,
    }

    public class InputTextPic
    {
        private static readonly Regex _nameRegex =
            new Regex(@"<color=(.+?)</color>", RegexOptions.Singleline);
        //new Regex(@"\[(.+?)\]", RegexOptions.Singleline);


        InputField _input;
        TextPic _textPic;
        string _backText;
        string _showText;
        public string Text
        {
            get { return _input.text; }
            set { _input.text = value; }
        }

        public string SendText
        {
            get { return _backText; }
        }


        public void Init(InputField input, Color color, Font font, Func<string, Sprite> loadFace,
            int emojiSize = 30, int fontSize = 18, VerticalWrapMode verticalWrapMode = VerticalWrapMode.Overflow)
        {
            _input = input;
            _input.onValueChanged.AddListener(OnTextChange);
            _backText = string.Empty;
            _showText = string.Empty;
            GameObject go = new GameObject("TextPic");
            _textPic = go.AddComponent<TextPic>();
            _textPic.InitTextPic(color, font, loadFace, emojiSize, fontSize, verticalWrapMode);

            Vector2 newSize = _input.transform.Find("Text").GetComponent<RectTransform>().sizeDelta;
            float newX = newSize.x * 0.05f;
            newSize.x += newX;
            //可能需要自己设置宽高Text的锚点不在一个点上sizeDelta的大小不对
            _textPic.rectTransform.sizeDelta = newSize;
            _textPic.alignment = TextAnchor.MiddleLeft;
            _textPic.transform.SetParent(_input.transform);
            Vector2 newPos = Vector2.zero;
            newPos.x += newX * 0.5f;
            _textPic.rectTransform.anchoredPosition = newPos;
            _input.textComponent = _textPic;
        }

        void OnTextChange(string text)
        {
            _showText = text;
            string tempBackText = _backText;
            //截取超链接数据
            Match hrefMatch = TextPic.HrefRegex.Match(_backText);
            Match hrefNameMatch = _nameRegex.Match(hrefMatch.Value);
            //检查当前内容是否有名称
            Match nameMatch = _nameRegex.Match(text);

            _backText = _showText;

            //有超链接数据并且超链接名称能对应.
            if (hrefMatch.Success)
            {
                if (hrefNameMatch.Success && nameMatch.Success
                    && hrefNameMatch.Groups[1].Value.CompareTo(nameMatch.Groups[1].Value) == 0)
                {
                    _backText += hrefMatch.Value;
                }
            }
            _textPic.text = _backText;
        }

        public void SendItem(Equip equip,ulong roleid)
        {
            if (equip == null)
            {
                return;
            }
            string text = _showText;
            //text内容示例:<color = red>[装备]</color>  "[]"中括号只是显示用。无特殊含义。
            string name = equip.Name;
            name = name.Insert(name.IndexOf(">") + 1, "[");
            name = name.Insert(name.IndexOf("</color>"), "]");
            bool hasHref = false;
            foreach (Match match in _nameRegex.Matches(text))
            {
                //匹配到的文字是不是与超链接一致.不一致则相当于重新插入.一致则是替换超链接
                if (name.CompareTo(_textPic.GetHrefName()) != 0)
                {
                    text = text.Replace(match.Value, name);
                    hasHref = true;
                }
            }

            if (!hasHref)
            {
                text += name;
            }
            _showText = text;
            int type = (int)ChatHrefType.CHT_ShowItem;
            string val = equip.DataID.ToString();

            if (equip.IsEquip)
            {
                type = (int)ChatHrefType.CHT_ShowEquip;
                val = equip.ServID.ToString();
            }
            text += "<href=" + name + " tpv=" + type.ToString() + "," + roleid + "," + val + " />";
            _backText = text;
            Debug.LogError(text);
            if (_showText.Length > _input.characterLimit)
            {
                _input.characterLimit = _showText.Length;
            }
            _input.text = _showText;
            //text内容示例:看我的装备吧<111>[装备]<123><333><href=装备 tpv=0,11111,22222 />

        }

        public void Clear()
        {
            _showText = string.Empty;
            _backText = string.Empty;
            _input.text = string.Empty;
        }

    }
}