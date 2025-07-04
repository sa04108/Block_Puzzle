using DG.Tweening;
using System.Collections;
using UnityEngine;

namespace Cubreak
{
    public class Block : MonoBehaviour
    {
        [Header("Effects")]
        [SerializeField] GameObject destroyEffect;

        private BlockWatcher blockWatcher => BlockWatcher.Instance;
        private UIManager uiManager => UIManager.Instance;
        private new Renderer renderer => GetComponent<Renderer>();

        private const float maxRayDistance = 1.0f;

        private Vector3 targetPos;

        public (int, int, int) Coord { get; private set; }
        public int ColorIndex { get; private set; } = 0;
        private bool isFalling = false;
        public bool IsFalling { get => isFalling; }
        private bool isUnconnected = false;

        private bool dirty;

        private Vector3[] rayCastVec
        {
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

        private void Start()
        {
            blockWatcher.Subscribe(this);
            targetPos = transform.position;

            isUnconnected = false; // 주변에 같은 색으로 연결될 수 있는 블럭이 없는 경우 true
            dirty = false;
        }

        // Update is called once per frame
        private void Update()
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, Time.deltaTime * 3f);
        }

        public void SetCoord(int x, int y, int z)
        {
            Coord = (x, y, z);
        }

        public void SetColor(ENUM_COLOR color)
        {
            renderer.material.color = BlockColors.colors[(int)color];
            ColorIndex = (int)color;
        }

        public void SetHintAnimation(bool active)
        {
            DOTween.Kill(this);

            if (active)
            {
                PlayHintAnimation();
            }
            else
            {
                renderer.material.SetColor("_EmissionColor", Color.black);
            }
        }

        private void PlayHintAnimation()
        {
            DOTween.To(() => renderer.material.GetColor("_EmissionColor"), x => renderer.material.SetColor("_EmissionColor", x), Color.gray, 0.5f)
                .SetId(this)
                .OnComplete(() =>
                {
                    DOTween.To(() => renderer.material.GetColor("_EmissionColor"), x => renderer.material.SetColor("_EmissionColor", x), Color.black, 0.5f)
                    .SetId(this)
                    .OnComplete(PlayHintAnimation);
                });
        }

        public void SetAlpha(float alpha)
        {
            Color color = renderer.material.color;
            color.a = alpha;
            renderer.material.color = color;
        }

        public void MoveDown()
        {
            StartCoroutine(CoMoveDown());
        }

        private IEnumerator CoMoveDown()
        {
            while (!Physics.Raycast(transform.position, Vector3.down, maxRayDistance))
            {
                targetPos += Vector3.down;
                Coord = blockWatcher.OnBlockMovedDown(this);

                if (!isFalling)
                {
                    blockWatcher.FallingBlockCount++;
                    isFalling = true;
                }

                yield return new WaitUntil(() => transform.position.y - targetPos.y <= 0.1f);
            }

            if (isFalling)
            {
                blockWatcher.FallingBlockCount--;
                isFalling = false;
            }
        }

        public void CheckmateCheck()
        {
            RaycastHit hit;

            for (int i = 0; i < rayCastVec.Length; i++)
            {
                if (Physics.Raycast(transform.position, rayCastVec[i], out hit, maxRayDistance, 1 << 6))
                {
                    if (hit.transform.GetComponent<Block>().ColorIndex == ColorIndex)
                    {
                        if (isUnconnected)
                            blockWatcher.UnconnectedBlockCount--;

                        isUnconnected = false;
                        return;
                    }
                }
            }

            // 6방향으로 RayCast 후 Block과 부딪치지 않았거나, 부딪친 Block과 색이 다른 경우
            if (!isUnconnected)
                blockWatcher.UnconnectedBlockCount++;

            isUnconnected = true;
        }

        public bool DestroyBlocks()
        {
            if (dirty)
                return true;

            RaycastHit hit;

            for (int i = 0; i < rayCastVec.Length; i++)
            {
                if (Physics.Raycast(transform.position, rayCastVec[i], out hit, maxRayDistance, 1 << 6))
                {
                    if (hit.transform.GetComponent<Block>().ColorIndex == ColorIndex)
                    {
                        dirty = true;
                        hit.transform.GetComponent<Block>().DestroyBlocks();

                        // 맨 끝 블럭의 경우 다시 이 함수가 실행되지 않으므로 따로 제거해주어야 한다.
                        // 맨 끝 블럭이 아닌 경우 이미 위에서 Destory 처리가 됐을 것이므로 예외처리를 한다.
                    }
                }
            }

            if (dirty)
            {
                DestroyAndScore();
                return true;
            }

            return false;
        }

        private void DestroyAndScore()
        {
            uiManager.ScoreUp();

            var effectObj = Instantiate(destroyEffect, transform.position, Quaternion.identity);
            effectObj.transform.localScale = Vector3.one * 0.3f;
            Destroy(effectObj, 0.8f);
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            DOTween.Kill(this);
            StopAllCoroutines();
            blockWatcher?.Unsubscribe(this);
        }
    } 
}
