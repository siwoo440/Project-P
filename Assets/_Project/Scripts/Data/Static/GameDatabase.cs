using System.Collections.Generic;
using UnityEngine;

namespace ProjectP.Data
{
    /// <summary>
    /// StaticData 묶음. DataManager는 이 에셋 하나를 통해 모든 정의 데이터를 조회한다.
    /// Resources 폴더를 쓰지 않고 00_Boot의 Bootstrapper에 직접 연결한다.
    ///
    /// 캐릭터·스킬·적·스테이지 데이터는 해당 일차에 목록을 추가한다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "Project P/Game Database")]
    public class GameDatabase : ScriptableObject
    {
        [SerializeField] private PuzzleConfig puzzleConfig;
        [SerializeField] private List<GemData> gems = new List<GemData>();

        public PuzzleConfig Puzzle => puzzleConfig;
        public IReadOnlyList<GemData> Gems => gems;

        /// <summary>종류에 해당하는 보석 정의. 없으면 null.</summary>
        public GemData GetGem(GemType type)
        {
            foreach (var gem in gems)
            {
                if (gem != null && gem.Type == type) return gem;
            }

            return null;
        }
    }
}
