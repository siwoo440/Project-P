using UnityEngine;

namespace ProjectP.Data
{
    /// <summary>
    /// StaticData 묶음. DataManager는 이 에셋 하나를 통해 모든 정의 데이터를 조회한다.
    /// Resources 폴더를 쓰지 않고 00_Boot의 Bootstrapper에 직접 연결한다.
    ///
    /// 5일차부터 보석·캐릭터·스킬·적·스테이지 데이터 목록을 추가한다.
    /// </summary>
    [CreateAssetMenu(fileName = "GameDatabase", menuName = "Project P/Game Database")]
    public class GameDatabase : ScriptableObject
    {
    }
}
