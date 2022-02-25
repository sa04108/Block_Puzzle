using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tutorial : MonoBehaviour
{
    int sceneNum;
    public RectTransform panel;
    public Text tutorialText;

    private void Start() {
        sceneNum = 0;

        if (PlayerPrefs.GetInt("Newbie", 1) == 0)
            gameObject.SetActive(false);
        else
            PlayerPrefs.SetInt("Newbie", 0);
    }

    public void NextStep()
    {
        switch (++sceneNum)
        {
            case 1:
                tutorialText.text = "���� ������ ����Ǿ��ִ� ��ϵ鸸 �ı��˴ϴ�!";
                break;
            case 2:
                tutorialText.text = "��, �� �����̵�� ť�긦 �¿�� ȸ���� �� �ֽ��ϴ�.";
                break;
            case 3:
                tutorialText.text = "��, �Ʒ� �����̵�� ť���� õ��� ���� �����ư��鼭 �� �� �ֽ��ϴ�.";
                break;
            case 4:
                MovePanelUp();
                tutorialText.text = "�Ʒ��� �ִ�\nSee Through ��ư��\n3x3x3 �̻��� ť�꿡�� Ȱ��ȭ�Ǹ� ť���� ���θ� �� �� �ֽ��ϴ�.";
                break;
            case 5:
                MovePanelDown();
                tutorialText.text = "�������� ���ӿ����� ��� ����� �ı��ؾ� Ŭ������ �� �ֽ��ϴ�.";
                break;
            case 6:
                tutorialText.text = "���� ���ӿ����� �ִ��� ���� ����� �ı��ϸ� �˴ϴ�!";
                break;            
            case 7:
                tutorialText.text = "�ð� ������ �����ϴ�!\n����� ���������ɷ��� ����� �����غ�����!";
                break;
            default:
                gameObject.SetActive(false);
                break;
        }
    }

    void MovePanelUp()
    {
        panel.sizeDelta = Vector2.up * 2000f;
        panel.anchorMin = Vector2.up;
        panel.anchorMax = Vector2.one;
        panel.pivot = new Vector2(0.5f, 1.0f);
    }

    void MovePanelDown()
    {
        panel.sizeDelta = Vector2.up * 1100f;
        panel.anchorMin = Vector2.zero;
        panel.anchorMax = Vector2.right;
        panel.pivot = new Vector2(0.5f, 0.0f);
    }
}
