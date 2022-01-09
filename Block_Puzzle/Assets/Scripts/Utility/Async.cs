using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

public class Async : MonoBehaviour {
    void Start() {
        TaskRun();
        TaskFromResult();
    }

    // async Ű����� �ش� �޼ҵ尡 await Ű���带 ������ ������ ǥ��
    // await Ű���尡 ����� ����� ����� ���� �Ҵ���� ������ ��ٸ��� �ʰ� ���α׷��� �����ȴ�(�ٸ� Task ����). �׸��� ��󿡰� ���� �ο��Ǹ� �ߴܵ� ��ġ���� �ٽ� ����
    async void TaskRun() {
        var task = Task.Run(() => TaskRunMethod(3));
        int count = await task;
        Debug.Log("Count : " + task.Result); // (3����) ���: 3
    }

    private int TaskRunMethod(int limit) {
        int count = 0;
        for (int i = 0; i < limit; i++) {
            ++count;
            Thread.Sleep(1000);
        }

        return count;
    }

    async void TaskFromResult() {
        int sum = await Task.FromResult(Add(4, 5));
        Debug.Log(sum); // ���: 9
    }

    private int Add(int a, int b) {
        return a + b;
    }
}