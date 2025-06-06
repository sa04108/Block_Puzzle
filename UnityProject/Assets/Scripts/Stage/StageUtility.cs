using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Cubreak
{
    /// <summary>
    /// Cube Stage에 대하여 다음 기능을 제공합니다.
    /// 1. 클리어 가능한지 확인
    /// 2. 클리어가 불가능하다면 임의로 클리어가 가능하도록 스테이지 수정
    /// 3. 현재 상태에서 클리어를 위해 파괴해야 하는 위치 알림 (Hint 시스템)
    /// </summary>
    public static class StageUtility
    {
        // Solve 과정에서의 블록 탐색 횟수
        private static int Tick = 0;

        private struct BlockGroup
        {
            public int color;
            public List<Tuple<int, int, int>> blocks;

            public BlockGroup(int color, List<Tuple<int, int, int>> blocks)
            {
                this.color = color;
                this.blocks = blocks;
            }
        }
        /// <summary>
        /// 외부 호출용: 스테이지에 있는 큐브의 "파괴 가능(true) / 불가(false)" 판별.
        /// 색깔은 1,2,3,... 처럼 양의 정수로 가정하고, 빈 칸(제거된 칸)은 0. 
        /// </summary>
        /// <returns>이 큐브를 차례대로 그룹 제거 후 빈 상태(전부 0)로 만들 수 있으면 true, 아니면 false.</returns>
        public static bool IsClearable(this CubeStage stage, out int tick)
        {
            // 재귀 탐색 전용 함수 호출
            return Solve(stage.ToInt3DArray(), out _, out tick);
        }

        /// <summary>
        /// 외부 호출용: 스테이지에 있는 큐브가 파괴 불가능하다면 파괴가 가능할 때까지
        /// 무작위 두 위치에 있는 블록의 색을 서로 교체
        /// </summary>
        /// <returns></returns>
        public static bool Fix(this CubeStage stage, int maxAttempt = 20)
        {
            int N = stage.Dimension;
            int[,,] grid = stage.ToInt3DArray();
            System.Random rnd = new();

            // 난이도 별 세부 제약을 두고 싶으면 여기서 각 difficulty 케이스를 나눠서 구현
            // 예: Medium 이상은 “레이어마다 색 종류를 최소 2개 이상 유지” 등.

            // 현재 블록 좌표 목록(0이 아닌 위치) 추출
            var filledCoords = new List<(int x, int y, int z)>();
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    for (int z = 0; z < N; z++)
                        if (grid[x, y, z] != 0)
                            filledCoords.Add((x, y, z));

            int attempt = 0;
            while (attempt < maxAttempt)
            {
                attempt++;

                // 1) 무작위로 두 서로 다른 블록 좌표를 골라서 swap
                int idxA = rnd.Next(filledCoords.Count);
                int idxB = rnd.Next(filledCoords.Count);
                if (idxA == idxB) continue;

                var a = filledCoords[idxA];
                var b = filledCoords[idxB];

                // “난이도 고려” 예: 같은 레이어(z) 안에만 swap하거나,
                // 서로 다른 색만 교환하는 등의 간단한 규칙을 추가할 수 있다.
                if (grid[a.x, a.y, a.z] == grid[b.x, b.y, b.z])
                    continue; // 같은 색끼리 교환은 무의미하다고 보고 skip

                // 실제 swap
                int temp = grid[a.x, a.y, a.z];
                grid[a.x, a.y, a.z] = grid[b.x, b.y, b.z];
                grid[b.x, b.y, b.z] = temp;

                // 2) swap 이후 “클리어 가능?” 판정
                if (Solve(grid, out _, out int tick))
                {
                    stage.ApplyGrid(grid);
                    Debug.Log($"<color=green>Stage {stage.Id} Fixed: {attempt}회 시도 후 성공.</color>");
                    Debug.Log("Tick: " + tick);
                    return true;
                }

                // 3) 실패했으면 원복하고 다음 시도
                temp = grid[a.x, a.y, a.z];
                grid[a.x, a.y, a.z] = grid[b.x, b.y, b.z];
                grid[b.x, b.y, b.z] = temp;
            }

            return false;
        }

        /// <summary>
        /// 재귀 탐색을 통해 Grid에 있는 모든 블록이 파괴 가능한지 확인한다.
        /// </summary>
        /// <param name="hint">hint: 파괴가 가능하다면 현재 상태에서 처음으로 파괴해야할 블록 리스트가 반환되며 그렇지 않으면 null</param>
        /// <param name="heuristic">휴리스틱 알고리즘을 사용할 것인지 여부</param>
        /// <returns>true: 파괴가 가능하다. / false: 불가능하다.</returns>
        public static bool Solve(int[,,] grid, out List<Tuple<int, int, int>> hint, out int tick, bool heuristic = false)
        {
            Tick = 0;

            if (heuristic)
            {
                SortedSet<int> fList = new();

                do
                {
                    hint = SolveIDAStar(grid, fList);
                    fList.Remove(fList.Min);
                } while (fList.Count > 0);
            }
            else
            {
                hint = SolveBruteForce(grid);
            }

            tick = Tick;

            if (hint == null)
                return false;

            return true;
        }

        /// <summary>
        /// 완전탐색을 통해 Grid의 파괴 여부를 확인한다.
        /// </summary>
        private static List<Tuple<int, int, int>> SolveBruteForce(int[,,] grid, Func<BlockGroup, bool> pruning = null)
        {
            // 만약 이미 모두 빈 상태라면 파괴 가능
            if (IsEmpty(grid))
            {
                // 더 이상 제거할 그룹이 없으므로, 빈 리스트로 초기화
                return new();
            }

            // 제거할 수 있는 모든 그룹을 찾는다.
            var allGroups = FindAllGroups(grid, out _);
            var unitGroups = allGroups.Where(group => group.blocks.Count == 1).ToArray();

            // 그룹이 하나도 없다면 더 이상 제거할 수 없으므로 불가
            if (allGroups.Count == 0)
            {
                return null;
            }

            // 동일한 색의 단일 블록이 적은 그룹부터 파괴한다.
            // 만약 단일 블록 수가 같다면 블록 개수가 많은 그룹부터 파괴한다.
            allGroups.Sort((a, b) =>
            {
                // 1) 한 개 이상의 그룹이 단일 블록이면 블록 개수에 따른 처리
                if (a.blocks.Count == 1 || b.blocks.Count == 1)
                {
                    if (a.blocks.Count > b.blocks.Count)
                        return -1; // 순서를 바꾸지 않는다. a -> b 유지
                    else if (a.blocks.Count < b.blocks.Count)
                        return 1; // 앞 뒤 순서를 바꾼다. b -> a
                    else
                        return 0;
                }

                // 2) 동일한 색의 단일 블록이 적은 그룹을 앞으로 배치
                int aColorUnits = 0;
                int bColorUnits = 0;
                foreach (var unit in unitGroups)
                {
                    if (unit.color == a.color)
                        aColorUnits++;
                    else if (unit.color == b.color)
                        bColorUnits++;
                }

                if (aColorUnits < bColorUnits)
                    return -1;
                else if (aColorUnits > bColorUnits)
                    return 1;
                else
                {
                    // 2) 블록 개수 비교. 블록 개수가 많은 그룹을 앞으로 배치
                    if (a.blocks.Count > b.blocks.Count)
                        return -1;
                    else if (a.blocks.Count < b.blocks.Count)
                        return 1;
                    else
                        return 0;
                }
            });

            // 각 그룹을 하나씩 제거해 보고, 재귀 호출
            foreach (var group in allGroups)
            {
                // 단일 블록은 파괴할 수 없으므로 제외
                if (group.blocks.Count == 1)
                    continue;

                // IDA* 등 가지치기 조건 확인
                if (pruning != null && pruning(group))
                    continue;

                // 현재 그룹을 제거한 다음 상태를 만들어 본다.
                int[,,] next = CloneGrid(grid);
                RemoveGroup(next, group);
                ApplyGravity(next);

                // 재귀 호출: next 상태에서 클리어 가능한지, 그리고 현재 그룹을 hint로 한다.
                if (SolveBruteForce(next) != null)
                {
                    return group.blocks;
                }
                // 만약 재귀에서 실패하면 그대로 다음 그룹을 시도
            }

            // 어느 경우에도 빈 상태로 못 이끌었으면 실패
            return null;
        }

        /// <summary>
        /// IDA* 알고리즘을 활용한다. IDDFS와의 차이는 여기서는 깊이 한계값을 정할 때 휴리스틱을 활용한다.
        /// </summary>
        /// <param name="fList">방문한 f score 목록. f = g + h</param>
        private static List<Tuple<int, int, int>> SolveIDAStar(int[,,] grid, SortedSet<int> fList)
        {
            Func<BlockGroup, bool> pruning = group =>
            {
                // f score = g + h
                // f1) g = (파괴한 블록 수), h = (이 그룹이 파괴된 후 남은 블록 수)
                int f = grid.Length - group.blocks.Count;
                // 이 그룹의 f가 방문 목록의 가장 작은 f보다 크면 가지치기(Pruning)
                if (fList.Count > 0 && f > fList.Min)
                {
                    fList.Add(f);
                    return true;
                }

                if (group.blocks.Count < 2)
                    return true;

                return false;
            };

            return SolveBruteForce(grid, pruning);
        }

        /// <summary>
        /// CubeStage 객체를 받아서 int[,,] 배열로 변환합니다.
        /// 반환되는 배열 grid[x, y, z]에서
        ///   - x, y, z ∈ [0 .. N-1]
        ///   - z = 0이 바닥층(1층), z가 커질수록 위층을 의미
        ///   - grid[x,y,z] == 0 은 빈 칸, >0 은 ENUM_COLOR를 (int)로 변환한 값입니다.
        /// </summary>
        /// <returns>크기 N×N×N인 int[,,] 배열</returns>
        private static int[,,] ToInt3DArray(this CubeStage stage)
        {
            int N = stage.Dimension;
            // N×N×N 크기의 3차원 배열을 0으로 초기화
            int[,,] grid = new int[N, N, N];

            // 각 Layer를 순회하면서
            //   layer.Index → z 인덱스로 사용
            //   arrangement.Color → (int)로 변환하여 색 번호로 사용
            //   arrangement.Positions → 1-based index를 x,y로 매핑
            foreach (var layer in stage.Layers)
            {
                int z = layer.Index;
                if (z < 0 || z >= N)
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(stage.Layers),
                        $"Layer.Index 값이 0~{N - 1} 범위를 벗어났습니다: {z}"
                    );
                }

                foreach (var arr in layer.Arrangements)
                {
                    // ENUM_COLOR → int
                    int colorValue = (int)arr.Color;

                    // positions 배열 순회
                    foreach (int pos in arr.Positions)
                    {
                        if (pos < 1 || pos > N * N)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(arr.Positions),
                                $"Position 값이 1~{N * N} 범위를 벗어났습니다: {pos}"
                            );
                        }

                        int zeroIndex = pos - 1;
                        int x = zeroIndex % N;       // 열(column)
                        int y = zeroIndex / N;       // 행(row)

                        // 0을 빈칸으로 해야 하므로 Color 값을 1-based로 변경
                        grid[x, y, z] = colorValue + 1;
                    }
                }
            }

            return grid;
        }

        /// <summary>
        /// 그리드 정보를 스테이지 객체에 덮어 씌운다.
        /// </summary>
        public static void ApplyGrid(this CubeStage stage, int[,,] grid)
        {
            int N = grid.GetLength(0);
            // stage.Dimension을 동기화(필요하다면)
            stage.Dimension = N;

            // 기존 Layers를 모두 지우고, 다시 채운다
            stage.Layers.Clear();

            for (int z = 0; z < N; z++)
            {
                var layer = new CubeLayer
                {
                    Index = z,
                    Arrangements = new List<Arrangement>()
                };

                // 이 층(z)에 있는 블록들을 색깔별로 모으기
                // key: ENUM_COLOR, value: List<1-based 위치 인덱스>
                var dict = new Dictionary<ENUM_COLOR, List<int>>();

                for (int y = 0; y < N; y++)
                {
                    for (int x = 0; x < N; x++)
                    {
                        int raw = grid[x, y, z];
                        var color = (ENUM_COLOR)(raw - 1);

                        // 1-based 포지션 계산: (y * N + x) + 1
                        int pos = y * N + x + 1;

                        if (!dict.TryGetValue(color, out var list))
                        {
                            list = new List<int>();
                            dict[color] = list;
                        }
                        list.Add(pos);
                    }
                }

                // dict에 모인 색깔별 위치 정보를 Arrangement로 변환
                foreach (var kv in dict)
                {
                    var arr = new Arrangement
                    {
                        Color = kv.Key,
                        Positions = kv.Value.ToArray()
                    };
                    layer.Arrangements.Add(arr);
                }

                stage.Layers.Add(layer);
            }
        }

        /// <summary>
        /// grid 내부가 모두 0인지(빈 상태) 확인.
        /// </summary>
        private static bool IsEmpty(int[,,] grid)
        {
            int N = grid.GetLength(0);

            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    for (int z = 0; z < N; z++)
                        if (grid[x, y, z] != 0)
                            return false;
            return true;
        }

        /// <summary>
        /// 현재 grid에서 "모든 연결된 같은 색 그룹"을 찾아서 List로 반환.
        /// 각 그룹은 (x,y,z) 좌표 튜플의 리스트.
        /// </summary>
        private static List<BlockGroup> FindAllGroups(int[,,] grid, out int emptyCount)
        {
            emptyCount = 0;

            int N = grid.GetLength(0);
            bool[,,] visited = new bool[N, N, N];
            var groups = new List<BlockGroup>();

            int[] dx = { 1, -1, 0, 0, 0, 0 };
            int[] dy = { 0, 0, 1, -1, 0, 0 };
            int[] dz = { 0, 0, 0, 0, 1, -1 };

            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {
                    for (int z = 0; z < N; z++)
                    {
                        if (grid[x, y, z] == 0)
                        {
                            emptyCount++;
                            continue;
                        }
                        if (!visited[x, y, z])
                        {
                            int color = grid[x, y, z];
                            // BFS/DFS로 연결된 같은 색 블록을 모두 모은다.
                            var stack = new Stack<Tuple<int, int, int>>();
                            var component = new BlockGroup(color, new());
                            stack.Push(Tuple.Create(x, y, z));
                            visited[x, y, z] = true;
                            Tick++;

                            while (stack.Count > 0)
                            {
                                var cur = stack.Pop();
                                component.blocks.Add(cur);
                                int cx = cur.Item1;
                                int cy = cur.Item2;
                                int cz = cur.Item3;

                                for (int dir = 0; dir < 6; dir++)
                                {
                                    int nx = cx + dx[dir];
                                    int ny = cy + dy[dir];
                                    int nz = cz + dz[dir];
                                    // 범위 확인
                                    if (nx < 0 || nx >= N || ny < 0 || ny >= N || nz < 0 || nz >= N)
                                        continue;
                                    if (visited[nx, ny, nz]) continue;
                                    if (grid[nx, ny, nz] == color)
                                    {
                                        visited[nx, ny, nz] = true;
                                        Tick++;
                                        stack.Push(Tuple.Create(nx, ny, nz));
                                    }
                                }
                            }

                            // 블록 개수 상관없이 일단 그룹으로 등록
                            groups.Add(component);
                        }
                    }
                }
            }

            return groups;
        }

        /// <summary>
        /// 주어진 group 리스트(연결된 같은 색 블록 좌표들)를 모두 제거(0으로 설정).
        /// </summary>
        private static void RemoveGroup(int[,,] grid, BlockGroup group)
        {
            foreach (var coord in group.blocks)
            {
                int x = coord.Item1;
                int y = coord.Item2;
                int z = coord.Item3;
                grid[x, y, z] = 0;
            }
        }

        /// <summary>
        /// 중력 적용: z=0이 바닥, z가 클수록 위. 
        /// x,y 고정된 축마다 “빈 칸(0 아닌 블록)들을 모두 아래로 내린다.”
        /// </summary>
        private static void ApplyGravity(int[,,] grid)
        {
            int N = grid.GetLength(0);
            // x와 y를 고정하고 z축 방향(0→N-1)으로 블록을 모아서 아래로 채움.
            for (int x = 0; x < N; x++)
            {
                for (int y = 0; y < N; y++)
                {
                    // 1) 해당 (x,y)축 상의 블록 색 번호를 모두 수집
                    var stack = new List<int>();
                    for (int z = 0; z < N; z++)
                    {
                        if (grid[x, y, z] != 0)
                        {
                            stack.Add(grid[x, y, z]);
                            grid[x, y, z] = 0; // 일단 비워 놓고
                        }
                    }

                    // 2) 모은 색 번호를 다시 z=0부터 채워 넣는다
                    for (int i = 0; i < stack.Count; i++)
                    {
                        grid[x, y, i] = stack[i];
                    }
                    // 나머지 z = stack.Count ~ N-1는 0(빈 칸) 상태 유지됨
                }
            }
        }

        /// <summary>
        /// 3차원 배열을 깊은 복사하여 새로운 int[,,]를 반환.
        /// </summary>
        private static int[,,] CloneGrid(int[,,] original)
        {
            int N = original.GetLength(0);
            int[,,] copy = new int[N, N, N];
            for (int x = 0; x < N; x++)
                for (int y = 0; y < N; y++)
                    for (int z = 0; z < N; z++)
                        copy[x, y, z] = original[x, y, z];
            return copy;
        }
    }
}
