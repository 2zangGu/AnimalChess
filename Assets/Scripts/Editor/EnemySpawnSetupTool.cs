#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using AnimalChess.Game;

namespace AnimalChess.EditorTools
{
    /// <summary>
    /// 라운드마다 적 유닛을 자동으로 배치해주는 'EnemySpawner'를 씬에 준비해주는 도구.
    /// RoundManager/PlayerRoster와 같은 방식으로, 이미 있으면 그대로 두고 없으면 새로 만든다.
    ///
    /// 먼저 'Tools > AnimalChess > 적 유닛 로스터(26종) 만들기'로 EnemyUnitData 애셋들을,
    /// 그리고 'Tools > AnimalChess > 라운드 HUD 만들기'로 RoundManager를 만들어둬야
    /// EnemySpawner가 라운드에 맞는 웨이브를 제대로 배치할 수 있다.
    ///
    /// 메뉴: Tools > AnimalChess > 적 웨이브 스포너 만들기
    /// </summary>
    public static class EnemySpawnSetupTool
    {
        [MenuItem("Tools/AnimalChess/적 웨이브 스포너 만들기")]
        public static void SetupEnemySpawner()
        {
            bool created = EnsureEnemySpawner(out GameObject spawnerGO);
            bool waveSizeUpdated = EnsureWaveSizeBackfill(spawnerGO.GetComponent<EnemySpawner>());

            string msg = created
                ? "'EnemySpawner' 오브젝트를 새로 만들었습니다.\n\n" +
                  "이제 Play를 누르면 지금 라운드에 맞는 적 유닛들이 EnemyUnitData의 minRound/maxRound에 " +
                  "따라 자동으로 골라져서 보드 적 존에 배치됩니다. 근접(사거리가 짧은) 유닛은 앞줄에, " +
                  "원거리(사거리가 긴) 유닛은 뒷줄에 서게 되고, 라운드가 바뀔 때마다 다시 계산돼서 배치됩니다."
                : "기존 'EnemySpawner'를 그대로 사용합니다." +
                  (waveSizeUpdated ? "\n\n(적 마릿수 증가 설정(roundsPerExtraEnemy=1)을 최신값으로 갱신했습니다.)" : "");

            EditorUtility.DisplayDialog("AnimalChess", msg, "확인");
            Selection.activeGameObject = spawnerGO;
        }

        private static bool EnsureEnemySpawner(out GameObject go)
        {
            var existing = Object.FindFirstObjectByType<EnemySpawner>();
            if (existing != null) { go = existing.gameObject; return false; }

            go = new GameObject("EnemySpawner");
            Undo.RegisterCreatedObjectUndo(go, "Create EnemySpawner");
            Undo.AddComponent<EnemySpawner>(go);
            return true;
        }

        /// <summary>
        /// 씬에 이미 있던 EnemySpawner는 컴포넌트 추가 시점의 값이 그대로 직렬화돼 있어서,
        /// EnemySpawner.cs의 필드 기본값을 코드에서 바꿔도 반영이 안 된다. 그래서 이 도구를
        /// 다시 실행할 때마다 "적 스탯을 절반으로 낮추는 대신 마릿수를 늘린다" 밸런스 조정값
        /// (roundsPerExtraEnemy=1)을 강제로 다시 맞춰준다 (사용자가 인스펙터에서 직접 손댄 값도
        /// 덮어써지니, 커스텀 값을 쓰고 싶다면 이 도구를 다시 실행하지 않으면 된다).
        /// </summary>
        private static bool EnsureWaveSizeBackfill(EnemySpawner spawner)
        {
            if (spawner == null) return false;
            if (spawner.roundsPerExtraEnemy == 1) return false;

            Undo.RecordObject(spawner, "Update EnemySpawner Wave Size");
            spawner.roundsPerExtraEnemy = 1;
            EditorUtility.SetDirty(spawner);
            return true;
        }
    }
}
#endif
