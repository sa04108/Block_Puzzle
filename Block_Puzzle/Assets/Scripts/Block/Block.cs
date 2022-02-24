using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Block : MonoBehaviour, IBlockType {
    [SerializeField] GameObject destroyEffect;

    ENUM_BLOCK_TYPE blockType;

    private BlockGroupStatus blockGroupStatus;
    new private Renderer renderer;
    private UIManager uiManager;
    private GameManager gameManager;

    private const float maxRayDistance = 1.0f;

    private Vector3 targetPos;

    private bool isFalling;
    public bool IsFalling { get => isFalling; }

    private bool isUnconnected;
    private bool destroyed;

    private Vector3[] rayCastVec {
        get {
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

    // Start is called before the first frame update
    private void Awake() {
        blockGroupStatus = BlockGroupStatus.Instance;
        uiManager = UIManager.Instance;
        gameManager = GameManager.Instance;
    }

    private void Start() {
        blockGroupStatus.BlockCount++;
        renderer = GetComponent<Renderer>();

        isFalling = true;
        blockGroupStatus.FallingBlockCount++;

        StartCoroutine(MoveDown());
        targetPos = transform.localPosition;

        isUnconnected = false; // �ֺ��� ���� ������ ����� �� �ִ� ���� ���� ��� true
        destroyed = false;

        SelectBlockType();
        gameManager.blocks.Add(gameObject);
    }

    // Update is called once per frame
    private void Update() {
        transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPos, Time.deltaTime * blockGroupStatus.BlockFallingSpeed);
    }

    public void SelectBlockType() {
        blockType = gameManager.blockType;
        if (blockType == ENUM_BLOCK_TYPE.UNDEFINED) {
            Debug.LogError("Block Type�� �������� �ʾҽ��ϴ�.");
            return;
        }
        ResetBlockColor();
    }

    public void ResetBlockColor() {
        if (blockType == ENUM_BLOCK_TYPE.RANDOMIZED) {
            int numOfBlockColor = blockGroupStatus.NumOfBlockColor;
            int colorVal = Random.Range(0, numOfBlockColor);

            renderer.material.color = BlockColors.colors[colorVal];
        }
        else {
            return;
        }
    }

    private bool CompareColor(Renderer r1, Renderer r2) {
        // �� �Լ��� alpha ���� �����ϰ� ���� ���ϴ� �Լ��Դϴ�.
        // ���� ���� ��� true, �ٸ� ��� false�� ��ȯ�մϴ�.

        if (r1.material.color.r == r2.material.color.r
            && r1.material.color.g == r2.material.color.g
            && r1.material.color.b == r2.material.color.b)
            return true;

        return false;
    }

    public IEnumerator MoveDown() {
        while (!Physics.Raycast(transform.position, Vector3.down, maxRayDistance)) {
            targetPos += Vector3.down;

            if (!isFalling) {
                blockGroupStatus.FallingBlockCount++;
                isFalling = true;
            }

            yield return new WaitUntil(() => transform.localPosition.y - targetPos.y <= 0.1f);
        }

        if (isFalling) {
            blockGroupStatus.FallingBlockCount--;
            isFalling = false;
        }
    }

    public void CheckmateCheck() {
        RaycastHit hit;

        for (int i = 0; i < rayCastVec.Length; i++) {
            if (Physics.Raycast(transform.position, rayCastVec[i], out hit, maxRayDistance, 1 << 6)) {
                if (CompareColor(hit.transform.GetComponent<Renderer>(), renderer)) {
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

    public void DestroyBlocks() {
        if (destroyed) return;

        RaycastHit hit;

        for (int i = 0; i < rayCastVec.Length; i++) {
            if (Physics.Raycast(transform.position, rayCastVec[i], out hit, maxRayDistance, 1 << 6)) {
                if (CompareColor(hit.transform.GetComponent<Renderer>(), renderer)) {
                    destroyed = true;
                    hit.transform.GetComponent<Block>().DestroyBlocks();

                    // �� �� ���� ��� �ٽ� �� �Լ��� ������� �����Ƿ� ���� �������־�� �Ѵ�.
                    // �� �� ���� �ƴ� ��� �̹� ������ Destory ó���� ���� ���̹Ƿ� ����ó���� �Ѵ�.
                }
            }
        }

        if (destroyed) DestroyThis();
    }

    private void DestroyThis() {
        uiManager.ScoreUp();
        blockGroupStatus.BlockCount--;
        Destroy(Instantiate(destroyEffect, transform.position, Quaternion.identity, transform.parent), 0.8f);
        Destroy(gameObject);
    }
}
