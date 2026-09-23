using System;
using ProjectP.Data;
using UnityEngine;

namespace ProjectP.Core
{
    /// <summary>
    /// StaticData(읽기 전용 게임 정의 데이터)의 소유자. CLAUDE.md 2장
    /// 런타임에 데이터를 수정하는 기능을 두지 않는다.
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        public GameDatabase Database { get; private set; }

        public void Initialize(GameDatabase database)
        {
            if (database == null)
            {
                throw new InvalidOperationException(
                    "GameDatabase가 연결되지 않았습니다. 00_Boot의 [Bootstrap] 오브젝트 인스펙터를 확인하세요.");
            }

            Database = database;
        }
    }
}
