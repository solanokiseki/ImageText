using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace ImageText
{
    /// <summary>
    /// 文本控件，支持超链接、图片
    /// </summary>
    [AddComponentMenu("UI/TextPic", 10)]
    public class TextPic : Text, IPointerClickHandler
    {
        /// <summary>
        /// 解析完最终的文本
        /// 控制显示
        /// </summary>
        private string _outputText = string.Empty;

        /// <summary>
        /// 最终发送的文本
        /// </summary>
        private string _sourceText = string.Empty;

        //<1,123456,654321>
        //2,123123,1
        //3,12312,1
        //type类型,如道具展示.入队申请.入会申请等,param 参数,道具展示填角色id,入队填队伍id,入会填行会id.val装备id,1,1
        //  .与除 \n 之外的任何单个字符匹配,要匹配'.'使用\.

        float _emojiSize = 0;

        /// <summary>
        /// 图片池
        /// </summary>
        protected readonly List<Image> m_ImagesPool = new List<Image>();

        /// <summary>
        /// 图片的最后一个顶点的索引
        /// </summary>
        private readonly List<ImageVertexIndex> _imagesVertexIndexs = new List<ImageVertexIndex>();

        struct ImageVertexIndex
        {
            public ImageVertexIndex(int start, int end)
            {
                startIndex = start;
                endIndex = end;
            }
            public int startIndex;
            public int endIndex;
        }

        /// <summary>
        /// 超链接信息列表
        /// </summary>
        private HrefInfo _hrefInfo;

        public string GetHrefName()
        {
            if (_hrefInfo!=null)
            {
                return _hrefInfo.name;
            }
            return string.Empty;
        }

        [Serializable]
        public class HrefClickEvent : UnityEvent<string> { }

        [SerializeField]
        private HrefClickEvent _onHrefClick = new HrefClickEvent();

        /// <summary>
        /// 超链接点击事件
        /// </summary>
        public HrefClickEvent onHrefClick
        {
            get { return _onHrefClick; }
            set { _onHrefClick = value; }
        }

        Func<string,Sprite> _loadFace = null;
        Sprite LoadFace(string name)
        {
            Sprite s = null;
            if (_loadFace!=null)
            {
                s = _loadFace.Invoke(name);
            }

            return s;
        }

        public void InitTextPic(Color color, Font font, Func<string, Sprite> loadFace, int emojiSize, int fontSize = 18, VerticalWrapMode verticalWrapMode = VerticalWrapMode.Truncate)
        {
            this.font = font;
            this.fontSize = fontSize;
            _emojiSize = emojiSize;
            this.color = color;
            _loadFace = loadFace;
            verticalOverflow = verticalWrapMode;
        }

        public override void SetVerticesDirty()
        {
            if (!_outputText.Equals( text))
            {
                _sourceText = text;
                //sourceText示例:看我的装备吧<111>[装备]<123><333><href=装备 tpv=0,11111,22222 />
                _outputText = text;
                DisposeText();
                m_Text = _outputText;
            }
            base.SetVerticesDirty();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
        }

        protected override void OnPopulateMesh(VertexHelper toFill)
        {
            UIVertex vert = new UIVertex();

            base.OnPopulateMesh(toFill);
            m_Text = _sourceText;
            //这里是先让基类去填充toFill.再自己修改tofill的顶点.让一个标签的顶点都在一个位置上.
            //来实现显示.有点浪费顶点.
            for (var i = 0; i < _imagesVertexIndexs.Count; i++)
            {
                int imgStartIndex = _imagesVertexIndexs[i].startIndex + 3;
                var rt = m_ImagesPool[i].rectTransform;
                var size = rt.sizeDelta;
                UIVertex imgPosVer = new UIVertex();
                toFill.PopulateUIVertex(ref vert, imgStartIndex);
                toFill.PopulateUIVertex(ref imgPosVer, _imagesVertexIndexs[i].endIndex - 1);
                //锚点中心点在正中间.第一个顶点在左下角.最后一个顶点在右下角
                rt.anchoredPosition = new Vector2(vert.position.x + (imgPosVer.position.x - vert.position.x) * 0.5f, vert.position.y + size.y * 0.5f);
                for (int j = _imagesVertexIndexs[i].startIndex; j < _imagesVertexIndexs[i].endIndex + 1; j++)
                {
                    UIVertex newVer = new UIVertex();
                    toFill.PopulateUIVertex(ref newVer, j);
                    newVer.uv0 = Vector2.zero;
                    toFill.SetUIVertex(newVer, j);
                }
            }

            if (_imagesVertexIndexs.Count != 0)
            {
                _imagesVertexIndexs.Clear();
            }
            // 处理超链接包围框

            if (_hrefInfo != null)
            {
                _hrefInfo.boxes.Clear();
                // 将超链接里面的文本顶点索引坐标加入到包围框
                toFill.PopulateUIVertex(ref vert, _hrefInfo.startIndex);
                var pos = vert.position;
                var bounds = new Bounds(pos, Vector3.zero);
                for (int i = _hrefInfo.startIndex, m = _hrefInfo.endIndex; i < m; i++)
                {
                    if (i >= toFill.currentVertCount)
                    {
                        break;
                    }
                    toFill.PopulateUIVertex(ref vert, i);
                    pos = vert.position;
                    if (pos.x < bounds.min.x) // 换行重新添加包围框
                    {
                        _hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
                        bounds = new Bounds(pos, Vector3.zero);
                    }
                    else
                    {
                        bounds.Encapsulate(pos); // 扩展包围框
                    }
                }
                _hrefInfo.boxes.Add(new Rect(bounds.min, bounds.size));
            }
        }

        /// <summary>
        /// 点击事件检测是否点击到超链接文本
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (_hrefInfo != null)
            {
                Vector2 lp;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    rectTransform, eventData.position, eventData.pressEventCamera, out lp);

                var boxes = _hrefInfo.boxes;
                for (var i = 0; i < boxes.Count; ++i)
                {
                    if (boxes[i].Contains(lp))
                    {
                       // Hades.Base.EvtDispatcher.Dispatch(Hades.Logic.EvtType.ChatHrefClick,_hrefInfo.hrefStr);
                        _onHrefClick.Invoke(_hrefInfo.hrefStr);
                        return;
                    }
                }
            }
        }

        void DisposeText()
        {
            _hrefInfo = null;
            DisposeOutPut();
            DisposeEmoji();
            DisposeHref();
        }

        /// <summary>
        /// 正则取出所需要的属性
        /// </summary>
        private static readonly Regex _imageRegex = new Regex(@"<(\d+?)>", RegexOptions.Singleline);
        /// <summary>
        /// 超链接正则<quad />为unity的富文本.这玩意之间的内容会占一个位置.也就是4个顶点,所以
        /// 超链接用这个东西来匹配,计算下字数,输入字数个<quad />,另外再传装备信息来.
        ///
        /// </summary>

        //  +?匹配上一个元素一次或多次，但次数尽可能少。

        void DisposeOutPut()
        {
            //_outputText内容示例:看我的装备吧<111>[装备]<123><333><href=装备 tpv=0,11111,22222 />
            string tempOut = string.Copy(_outputText);
            string emojiQuadTag = "<quad name= size="+_emojiSize+" wide=1 /> ";

            foreach (Match match in _imageRegex.Matches(_outputText))
            {
                int matchIndex = tempOut.IndexOf(match.Value);
                tempOut = tempOut.Insert(matchIndex, emojiQuadTag);
                tempOut = tempOut.Insert(matchIndex + 11, match.Groups[1].Value);//"<quad name=".length为11
                tempOut = tempOut.Remove(matchIndex + emojiQuadTag.Length + match.Groups[1].Value.Length, match.Value.Length);
            }
            _outputText = tempOut;
            //_outputText内容示例:看我的装备吧<quad name=111 size=30 wide=1 />[装备]<quad name=123 size=30 wide=1 /><quad name=333 size=30 wide=1 /><href=装备 tpv=0,11111,22222 />
        }

        public static readonly Regex EmojiRegex = new Regex(@"<quad name=(\d+?) size=(\d+?) wide=(\d+?) />", RegexOptions.Singleline);
        void DisposeEmoji()
        {
            _imagesVertexIndexs.Clear();
            foreach (Match match in EmojiRegex.Matches(_outputText))
            {
                string spriteName = string.Copy(match.Groups[1].Value);
               
                int startIndex = match.Index * 4;
                int endIndex = (match.Index + match.Value.Length) * 4-1;
                _imagesVertexIndexs.Add(new ImageVertexIndex(startIndex, endIndex));

                m_ImagesPool.RemoveAll(image => image == null);
                if (m_ImagesPool.Count == 0)
                {
                    GetComponentsInChildren<Image>(m_ImagesPool);
                }
                if (_imagesVertexIndexs.Count > m_ImagesPool.Count)
                {
                    var resources = new DefaultControls.Resources();
                    var go = DefaultControls.CreateImage(resources);
                    go.layer = gameObject.layer;
                    var rt = go.transform as RectTransform;
                    if (rt)
                    {
                        rt.SetParent(rectTransform);
                        //rt.localPosition = Vector3.zero;
                        //rt.localRotation = Quaternion.identity;
                        //rt.localScale = Vector3.one;
                    }
                    m_ImagesPool.Add(go.GetComponent<Image>());
                }

                var img = m_ImagesPool[_imagesVertexIndexs.Count - 1];
                img.sprite = LoadFace(spriteName);
                img.rectTransform.sizeDelta = new Vector2(_emojiSize, _emojiSize);
                img.enabled = true;
            }
            for (var i = _imagesVertexIndexs.Count; i < m_ImagesPool.Count; i++)
            {
                if (m_ImagesPool[i])
                {
                    m_ImagesPool[i].enabled = false;
                }
            }
        }

        public static readonly Regex HrefRegex = new Regex(@"<href=(.+?) tpv=(.+?) />", RegexOptions.Singleline);
        void DisposeHref()
        {
            //_outputText内容示例:看我的装备吧<quad name=111 size=30 wide=1 /><color=#11111111>[装备]</color><quad name=123 size=30 wide=1 /><quad name=333 size=30 wide=1 /><href=<color=#11111111>[装备]</color> tpv=0,11111,22222 />
            _hrefInfo = null;
            string tempText = string.Copy(_outputText);
            foreach (Match match in HrefRegex.Matches(_outputText))
            {
                //填充超链接信息
                int index = _outputText.IndexOf(match.Groups[1].Value);
                int hrefStartIndex = index * 4;
                int hrefEndIndex = (index + match.Groups[1].Value.Length) * 4 - 1;
                tempText = tempText.Remove(tempText.IndexOf(match.Groups[0].Value), match.Groups[0].Value.Length);
                string name = match.Groups[1].Value;

                _hrefInfo = new HrefInfo
                {
                    startIndex = hrefStartIndex, // 超链接里的文本起始顶点索引
                    endIndex = hrefEndIndex,
                    name = name,
                    hrefStr = match.Groups[2].Value
                };
            }
            _outputText = tempText;
        }

        /// <summary>
        /// 超链接信息类
        /// </summary>
        private class HrefInfo
        {
            public int startIndex;
            public int endIndex;
            public string name;
            public string hrefStr;
            public readonly List<Rect> boxes = new List<Rect>();
        }
    }
}
