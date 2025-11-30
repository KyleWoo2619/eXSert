using UnityEngine.UI;
using UnityEngine;

public class NavigationImageSwapper : MonoBehaviour
{
    public enum MenuType {Log, Diary, Main}
    private MenuType currentMenuType;
    [SerializeField] private LogScrollingList logScrollingList;
    [SerializeField] private DiaryScrollingList diaryScrollingList;
    [SerializeField] private Image navigationImage;

    private void Awake()
    {
        logScrollingList = GetComponent<LogScrollingList>();
        diaryScrollingList = GetComponent<DiaryScrollingList>();

    }

    

    
}
