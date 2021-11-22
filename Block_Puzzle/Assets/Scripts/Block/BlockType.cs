using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlockType : MonoBehaviour
{
    public GameObject destroyEffect;

    // Block Type ������ �ݵ�� Block ��ü�� �ν��Ͻ�ȭ �Ǳ� ���� �̸� �Ǿ��־�� �մϴ�.
    void Awake()
    {
        if (PatternedGameManager.Instance.gameObject.activeSelf)
            gameObject.AddComponent<PatternedBlock>();
        else
            gameObject.AddComponent<RandomizedBlock>();
    }
}
