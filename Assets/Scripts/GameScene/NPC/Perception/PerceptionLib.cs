using System.Collections.Generic;
using UnityEngine;

public interface IPerceptionAlgorithm
{
    void UpdatePerception(NPCPerception perception);
}

[DisallowMultipleComponent]
public class PerceptionLib : MonoBehaviour
{
    [SerializeField] private int defaultAlgorithmIndex = 0;
    public List<IPerceptionAlgorithm> algorithms = new List<IPerceptionAlgorithm>();

    void Awake()
    {
    // 注册可用的感知算法（按需扩展）
    // 默认只注册 ForwardSee 和 SphereScan。AlwaysSee 可用于调试但不在默认列表中。
    algorithms.Add(new ForwardSee());
    algorithms.Add(new SphereScan());
    }

    public void RunPerception(NPCPerception perception)
    {
        if (perception == null) return;

        // 清除先前状态，算法可以设置为已见
        perception.ClearSeen();

        // 依次运行算法，直到有算法报告看见玩家
        for (int i = 0; i < algorithms.Count; i++)
        {
            algorithms[i]?.UpdatePerception(perception);
            if (perception.PlayerSeen) return;
        }
    }
}
