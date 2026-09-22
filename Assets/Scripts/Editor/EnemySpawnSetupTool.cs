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

            string msg = created
                ? "'EnemySpawner' 오브젝트를 새로 만들었습니다.\n\n" +
                  "이제 Play를 누르면 지금 라운드에 맞는 적 유닛들이 EnemyUnitData의 minRound/maxRound에 " +
                  "따라 자동으로 골라져서 보드 적 존에 배치됩니다. 근접(사거리가 짧은) 유닛은 앞줄에, " +
                  "원거리(사거리가 긴) 유닛은 뒷줄에 서게 되고, 라운드가 바뀔 때마다 다시 계산돼서 배치됩니다."
                : "기존 'EnemySpawner'를 그대로 사용합니다.";

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
    }
}
#endif
