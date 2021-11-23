using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    int sceneNum;
    public RectTransform panel;
    public Text tutorialText;

    private void Awake()
    {
        sceneNum = 0;
    }

    public void NextStep()
    {
        switch (++sceneNum)
        {
            case 1:
                tutorialText.text = "���� ������ ����Ǿ��ִ� ��ϵ鸸 �ı��˴ϴ�!";
                break;
            case 2:
                panel.sizeDelta = Vector2.up * 2000f;
                panel.anchorMin = Vector2.up;
                panel.anchorMax = Vector2.one;
                panel.pivot = new Vector2(0.5f, 1.0f);
                tutorialText.text = "Left�� Right ��ư���� ť�긦 �¿�� ȸ����ų �� �ֽ��ϴ�.";
                break;
            case 3:
                panel.sizeDelta = Vector2.up * 1200f;
                panel.anchorMin = Vector2.zero;
                panel.anchorMax = Vector2.right;
                panel.pivot = new Vector2(0.5f, 0.0f);
                tutorialText.text = "�������� ���ӿ����� ��� ����� �ı��ؾ� Ŭ������ �� �ֽ��ϴ�.";
                break;
            case 4:
                tutorialText.text = "���� ���ӿ����� �ִ��� ���� ����� �ı��ϸ� �˴ϴ�!";
                break;            
            case 5:
                tutorialText.text = "�ð� ������ �����ϴ�!\n\n����� ���������ɷ��� ����� �����غ�����!";
                break;
            default:
                gameObject.SetActive(false);
                break;
        }
    }
}
