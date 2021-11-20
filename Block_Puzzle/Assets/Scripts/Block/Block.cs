using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour
{
    GameManager gameManager;
    BlockGroupStatus blockGroupStatus;
    Renderer renderer;
    const float maxRayDistance = 1.0f;

    Vector3 targetPos;
    
    private bool isFalling;
    public bool IsFalling { get => isFalling; }

    [SerializeField]
    bool isUnconnected;
    bool destroyed;

    private Vector3[] rayCastVec {
        get
        {
            Vector3[] _rayCastVec = new Vector3[6];
            _rayCastVec[0] = Vector3.forward;
            _rayCastVec[1] = Vector3.back;
            _rayCastVec[2] = Vector3.left;
            _rayCastVec[3] = Vector3.right;
            _rayCastVec[4] = Vector3.up;
            _rayCastVec[5] = Vector3.down;
            return _rayCastVec;
        }
    }

    public GameObject destroyEffect;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        gameManager.blocks.Add(gameObject);
        blockGroupStatus = FindObjectOfType<BlockGroupStatus>();
        blockGroupStatus.BlockCount++;

        renderer = GetComponent<Renderer>();

        isFalling = true;
        blockGroupStatus.FallingBlockCount++;
        StartCoroutine(MoveDown());
        targetPos = transform.localPosition;

        isUnconnected = false; // �ֺ��� ���� ������ ����� �� �ִ� ���� ���� ��� true
        destroyed = false;

        ResetColor();
    }

    // Update is called once per frame
    void Update()
    {
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, Time.deltaTime * blockGroupStatus.BlockFallingSpeed);
    }

    void ResetColor()
    {
        int numOfBlockColor = blockGroupStatus.NumOfBlockColor;
        int colorVal = Random.Range(0, numOfBlockColor);

        switch (colorVal)
        {
            case 0:
                renderer.material.color = Color.red;
                break;
            case 1:
                renderer.material.color = Color.green;
                break;
            case 2:
                renderer.material.color = Color.blue;
                break;
            case 3:
                renderer.material.color = Color.yellow;
                break;
            case 4:
                renderer.material.color = Color.black;
                break;
            case 5:
                renderer.material.color = Color.white;
                break;
            default:
                break;
        }
    }

    public IEnumerator MoveDown()
    {
        while (!Physics.Raycast(transform.position, Vector3.down, maxRayDistance))
        {
            targetPos += Vector3.down;

            if (!isFalling)
            {
                blockGroupStatus.FallingBlockCount++;
                isFalling = true;
            }

            yield return new WaitUntil(() => transform.localPosition.y - targetPos.y <= 0.1f);
        }

        if (isFalling)
        {
            blockGroupStatus.FallingBlockCount--;
            isFalling = false;
        }
    }

    public void CheckmateCheck()
    {
        RaycastHit hit;

        for (int i = 0; i < rayCastVec.Length; i++)
        {
            if (Physics.Raycast(transform.position, rayCastVec[i], out hit, maxRayDistance))
            {
                if (hit.transform.CompareTag("Block")
                    && hit.transform.GetComponent<Renderer>().material.color == renderer.material.color)
                {
                    if (isUnconnected)
                        blockGroupStatus.UnconnectedBlockCount--;

                    isUnconnected = false;
                    return;
                }
            }
        }

        // 6�������� RayCast �� Block�� �ε�ġ�� �ʾҰų�, �ε�ģ Block�� ���� �ٸ� ���
        if (!isUnconnected)
            blockGroupStatus.UnconnectedBlockCount++;

        isUnconnected = true;
    }

    public void DestroyBlocks()
    {
        if (destroyed) return;

        RaycastHit hit;

        for (int i = 0; i < rayCastVec.Length; i++)
        {
            if (Physics.Raycast(transform.position, rayCastVec[i], out hit, maxRayDistance))
            {
                if (hit.transform.CompareTag("Block")
                    && hit.transform.GetComponent<Renderer>().material.color == renderer.material.color)
                {
                    destroyed = true;
                    hit.transform.GetComponent<Block>().DestroyBlocks();

                    // �� �� ���� ��� �ٽ� �� �Լ��� ������� �����Ƿ� ���� �������־�� �Ѵ�.
                    // �� �� ���� �ƴ� ��� �̹� ������ Destory ó���� ���� ���̹Ƿ� ����ó���� �Ѵ�.
                }
            }
        }

        if (destroyed) DestroyThis();
    }

    private void DestroyThis()
    {
        blockGroupStatus.BlockCount--;
        Destroy(Instantiate(destroyEffect, transform.position, Quaternion.identity, transform.parent), 0.8f);
        Destroy(gameObject);
    }
}
