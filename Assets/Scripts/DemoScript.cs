using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ImageText;
public class DemoScript : MonoBehaviour {

    public InputField _input;
    public Transform _content;
    public Transform _faceList;
    public Transform _chat;
    public Transform _itemInfo;
    public Button _send;
    public Button _face;
    // Use this for initialization
    InputTextPic _inputTextPic;
    ChatItemHref _chatItemHref;
    Dictionary<ulong, List<ulong>> _roleEqups = new Dictionary<ulong, List<ulong>>();//角色对应装备
    Dictionary<ulong, Equip> _equips = new Dictionary<ulong, Equip>();   //装备列表.
    Dictionary<int, Equip> _items = new Dictionary<int, Equip>();//道具列表 
    Font _font;
    private void Awake()
    {
        _inputTextPic = new InputTextPic();
        _chatItemHref = new ChatItemHref();
        _chatItemHref.AddCallBack(ChatHrefType.CHT_ShowEquip, EquipHref);
        _chatItemHref.AddCallBack(ChatHrefType.CHT_ShowItem, ItemHref);
        _font = Resources.Load<GameObject>("font/defaultFont").GetComponent<Text>().font;
    }

    void Start ()
    {
        _send.onClick.AddListener(Send);
        _inputTextPic.Init(_input, Color.red, _font, LoadFace, 20, 30);
        GameObject obj = Resources.Load<GameObject>("Prefabs/faceBtn");
        for (int i = 101; i < 129; i++)
        {
            CreateFace(i.ToString(), obj);
        }
        _face.onClick.AddListener(ShowFace);

        ulong player1 = 11000000001;
        List<ulong> equips1 = new List<ulong>();
        Equip player1_1 = new Equip("<color=cyan>一把剑</color>", true, 1001, 1, 1000000001, 50);
        equips1.Add(1000000001);
        _inputTextPic.SendItem(player1_1, player1);
        _input.text += "<107>";
        Send();

        Equip player1_2 = new Equip("<color=cyan>一件盔甲</color>", true, 1002, 1, 1000000002, 0);
        equips1.Add(1000000002);
        _input.text += "<106>";
        _inputTextPic.SendItem(player1_2, player1);
        Send();
        Equip player1_3 = new Equip("<color=yellow>金创药</color>", false, 2001, 63, 1000000003, 0);
        equips1.Add(1000000003);
        _inputTextPic.SendItem(player1_3, player1);
        Send();
        Equip player1_4 = new Equip("<color=yellow>回蓝药</color>", false, 2002, 3, 1000000004, 0);
        equips1.Add(1000000004);
        _inputTextPic.SendItem(player1_4, player1);
        Send();
        _equips.Add(1000000001, player1_1);
        _equips.Add(1000000002, player1_2);
        _items.Add(2001, player1_3);
        _items.Add(2002, player1_4);
        _roleEqups.Add(player1,equips1);

        ulong player2 = 11000000002;
        List<ulong> equips2 = new List<ulong>();
        Equip player2_1 = new Equip("<color=green>好剑</color>", true, 1011, 1, 1000000011, 90);
        equips2.Add(1000000011);
        _input.text += "来瞧瞧我的";
        _inputTextPic.SendItem(player2_1, player2);
        _input.text += "<108>";
        Send();

        Equip player2_2 = new Equip("<color=green>荆棘铠甲</color>", true, 1012, 1, 1000000012, 5);
        equips2.Add(1000000012);
        _input.text += "衣服还行";
        _inputTextPic.SendItem(player2_2, player2);
        Send();

        Equip player2_3 = new Equip("<color=red>神圣药水</color>", false, 2011, 44, 1000000013, 0);
        equips2.Add(1000000013);
        _input.text += "看看药水";
        _inputTextPic.SendItem(player2_3, player2);
        Send();

        Equip player2_4 = new Equip("<color=red>完全恢复药水</color>", false, 2012, 12, 1000000014, 0);
        equips2.Add(1000000014);
        _inputTextPic.SendItem(player2_4, player2);
        Send();

        _roleEqups.Add(player2, equips2);
        _equips.Add(1000000011, player2_1);
        _equips.Add(1000000012, player2_2);
        _items.Add(2011, player2_3);
        _items.Add(2012, player2_4);
    }
	
    void ShowFace()
    {
        _faceList.gameObject.SetActive(!_faceList.gameObject.activeSelf);
    }

    void CreateFace(string faceName,GameObject obj)
    {
        GameObject go = Instantiate(obj);
        go.transform.SetParent(_faceList);
        go.GetComponent<Image>().sprite = Resources.Load<Sprite>("face/" + faceName);
        go.GetComponent<Button>().onClick.AddListener(()=>OnFaceClick(faceName));
    }

    void OnFaceClick(string faceName)
    {
        string text = "<" + faceName.ToString() + ">";
        if (_input.text.Length + text.Length < _input.characterLimit)
            _input.text += text;
    }

    void EquipHref(string roleID, string equipID)
    {
        Debug.LogError("点到装备了");
        ShowItemInfo(_equips[ulong.Parse( equipID)],ulong.Parse(roleID));
    }

    void ItemHref(string roleID, string itemID)
    {
        Debug.LogError("点到道具了");
        ShowItemInfo(_items[int.Parse(itemID)], ulong.Parse(roleID));
    }

    void TeamHref(string teamID, string leaderID)
    {
        Debug.LogError(leaderID+"邀请你加入"+teamID+"队伍");
    }


    void Send()
    {
       string msg = _inputTextPic.SendText;
        GameObject obj = Resources.Load<GameObject>("Prefabs/chatitem");
        GameObject go = Instantiate(obj);
        TextPic textPic = go.transform.Find("Text").gameObject.AddComponent<TextPic>();
        textPic.InitTextPic(Color.black, _font,LoadFace, 40,30);
        textPic.onHrefClick.AddListener(_chatItemHref.OnHrefClick);
        go.transform.SetParent(_chat);
        textPic.text = msg;
        _inputTextPic.Clear();
    }


    void ShowItemInfo(Equip item, ulong roleID)
    {
        _itemInfo.gameObject.SetActive(true);
        for (int i = 0; i < _itemInfo.childCount; i++)
        {
            Destroy(_itemInfo.GetChild(i).gameObject);
        }
        GameObject obj = Resources.Load<GameObject>("font/defaultFont");
        SetText(obj, item.Name);
        SetText(obj, "所属:" + roleID.ToString());
        SetText(obj, "全局id:" + item.ServID.ToString());
        if (item.IsEquip)
        {
            SetText(obj, "攻击力:" + item.ATK.ToString());
        }
    }

    void SetText(GameObject obj, string val)
    {
        GameObject go = Instantiate(obj);
        go.GetComponent<Text>().text = val;
        go.transform.SetParent(_itemInfo);
    }


    public static Sprite LoadFace(string name)
    {
        string path = "face/" + name;
        return Resources.Load<Sprite>(path);
    }
}
