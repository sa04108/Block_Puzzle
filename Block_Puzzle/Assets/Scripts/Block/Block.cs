using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class Block : MonoBehaviour
{
    protected BlockGroupStatus blockGroupStatus;
    protected Renderer renderer;
    private UIManager uiManager;

    private const float maxRayDistance = 1.0f;

    private Vector3 targetPos;

    private bool isFalling;
    public bool IsFalling { get => isFalling; }

    [SerializeField]
    private bool isUnconnected;
    private bool destroyed;

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
    virtual protected void Awake()
    {
        blockGroupStatus = FindObjectOfType<BlockGroupStatus>();
        blockGroupStatus.BlockCount++;
        renderer = GetComponent<Renderer>();
        uiManager = FindObjectOfType<UIManager>();

        isFalling = true;
        blockGroupStatus.FallingBlockCount++;
        StartCoroutine(MoveDown());
        targetPos = transform.localPosition;

        isUnconnected = false; // �ֺ��� ���� ������ ����� �� �ִ� ���� ���� ��� true
        destroyed = false;

        InitGameManager();
    }

    // Update is called once per frame
    private void Update()
    {
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, Time.deltaTime * blockGroupStatus.BlockFallingSpeed);
    }

    abstract public void InitGameManager();

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
        uiManager.ScoreUp();
        blockGroupStatus.BlockCount--;
        Destroy(Instantiate(destroyEffect, transform.position, Quaternion.identity, transform.parent), 0.8f);
        Destroy(gameObject);
    }
}
