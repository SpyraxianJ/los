using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SoldierPortrait : MonoBehaviour
{
    public void Init(Soldier s)
    {
        GetComponent<Image>().sprite = s.soldierPortrait;
        transform.Find("SoldierName").GetComponent<TextMeshProUGUI>().text = s.soldierName;
        if (s.soldierTeam == 1)
            transform.Find("TeamIndicator").GetComponent<TextMeshProUGUI>().text = $"<color=red>{s.soldierTeam}</color>";
        else
            transform.Find("TeamIndicator").GetComponent<TextMeshProUGUI>().text = $"<color=blue>{s.soldierTeam}</color>";
        transform.Find("RankIndicator").GetComponent<Image>().sprite = s.LoadInsignia(s.rank);
    }
}
