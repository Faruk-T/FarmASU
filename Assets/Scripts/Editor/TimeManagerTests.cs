#if UNITY_EDITOR
using FarmASU.Core;
using UnityEditor;
using UnityEngine;

namespace FarmASU.Editor
{
    /// <summary>
    /// Test suite validating all 10 required Game Clock Engine specifications for Phase 2.
    /// Can be executed via Unity Editor menu: FarmASU -> Run Game Clock Tests
    /// </summary>
    public static class TimeManagerTests
    {
        [MenuItem("FarmASU/Run Game Clock Tests")]
        public static void RunAllTests()
        {
            Debug.Log("==================================================");
            Debug.Log("[TimeManagerTests] Starting Phase 2 Game Clock Test Suite...");
            int passed = 0;
            int failed = 0;

            RunTest("Test 1: Initial State (Day 1 - 06:00)", TestInitialState, ref passed, ref failed);
            RunTest("Test 2: +1 Real Second = +1 Game Minute (06:01)", TestOneMinuteAdvance, ref passed, ref failed);
            RunTest("Test 3: +60 Real Seconds = +1 Game Hour (07:00)", TestOneHourAdvance, ref passed, ref failed);
            RunTest("Test 4: +24 Game Hours = Next Day at Same Hour (Day 2 - 06:00)", TestFullDayCycle, ref passed, ref failed);
            RunTest("Test 5: Pause prevents time advancement", TestPause, ref passed, ref failed);
            RunTest("Test 6: Resume resumes time advancement", TestResume, ref passed, ref failed);
            RunTest("Test 7: 2x speed doubles progression rate", TestDoubleSpeed, ref passed, ref failed);
            RunTest("Test 8: 5x speed quintuples progression rate", TestFiveTimesSpeed, ref passed, ref failed);
            RunTest("Test 9: 23:59 -> 00:00 midnight day rollover", TestMidnightDayRollover, ref passed, ref failed);
            RunTest("Test 10: Frame-rate independence & fractional accumulation", TestFrameRateIndependence, ref passed, ref failed);

            Debug.Log("--------------------------------------------------");
            if (failed == 0)
            {
                Debug.Log($"[TimeManagerTests] ALL {passed} TESTS PASSED SUCCESSFULLY! (0 Failures)");
            }
            else
            {
                Debug.LogError($"[TimeManagerTests] TEST RUN FINISHED: {passed} Passed, {failed} FAILED.");
            }
            Debug.Log("==================================================");
        }

        private static void RunTest(string testName, System.Action testAction, ref int passed, ref int failed)
        {
            try
            {
                testAction.Invoke();
                Debug.Log($"[PASS] {testName}");
                passed++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[FAIL] {testName}: {ex.Message}");
                failed++;
            }
        }

        private static (GameObject go, TimeManager tm, GameTimeConfig config) CreateTestRig()
        {
            GameObject go = new GameObject("Test_TimeManager");
            TimeManager tm = go.AddComponent<TimeManager>();

            GameTimeConfig config = ScriptableObject.CreateInstance<GameTimeConfig>();
            tm.SetConfig(config);
            tm.InitializeState();

            return (go, tm, config);
        }

        private static void CleanupTestRig(GameObject go, GameTimeConfig config)
        {
            if (go != null) Object.DestroyImmediate(go);
            if (config != null) Object.DestroyImmediate(config);
        }

        private static void TestInitialState()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                Assert(tm.Day == 1, $"Expected Day 1, got {tm.Day}");
                Assert(tm.Hour == 6, $"Expected Hour 6, got {tm.Hour}");
                Assert(tm.Minute == 0, $"Expected Minute 0, got {tm.Minute}");
                Assert(!tm.IsPaused, "Expected unpaused initially");
                Assert(Mathf.Approximately(tm.SpeedMultiplier, 1.0f), "Expected 1.0 speed initially");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void TestOneMinuteAdvance()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                int minuteEventCount = 0;
                tm.OnMinuteChanged += (m, h) => minuteEventCount++;

                tm.AdvanceRealSeconds(1.0f); // 1 real sec = 1 game min

                Assert(tm.Day == 1, $"Expected Day 1, got {tm.Day}");
                Assert(tm.Hour == 6, $"Expected Hour 6, got {tm.Hour}");
                Assert(tm.Minute == 1, $"Expected Minute 1, got {tm.Minute}");
                Assert(minuteEventCount == 1, $"Expected 1 minute event, got {minuteEventCount}");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void TestOneHourAdvance()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                int hourEventCount = 0;
                tm.OnHourChanged += (h, d) => hourEventCount++;

                tm.AdvanceRealSeconds(60.0f); // 60 real sec = 60 game min = 1 game hour

                Assert(tm.Day == 1, $"Expected Day 1, got {tm.Day}");
                Assert(tm.Hour == 7, $"Expected Hour 7, got {tm.Hour}");
                Assert(tm.Minute == 0, $"Expected Minute 0, got {tm.Minute}");
                Assert(hourEventCount == 1, $"Expected 1 hour event, got {hourEventCount}");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void TestFullDayCycle()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                int dayEventCount = 0;
                tm.OnDayChanged += (d) => dayEventCount++;

                // 24 game hours = 24 * 60 = 1440 real seconds
                tm.AdvanceRealSeconds(1440.0f);

                Assert(tm.Day == 2, $"Expected Day 2, got {tm.Day}");
                Assert(tm.Hour == 6, $"Expected Hour 6, got {tm.Hour}");
                Assert(tm.Minute == 0, $"Expected Minute 0, got {tm.Minute}");
                Assert(dayEventCount == 1, $"Expected 1 day event, got {dayEventCount}");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void TestPause()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                tm.Pause();
                Assert(tm.IsPaused, "Expected IsPaused to be true");

                tm.AdvanceRealSeconds(10.0f);

                Assert(tm.Day == 1, "Day should not advance while paused");
                Assert(tm.Hour == 6, "Hour should not advance while paused");
                Assert(tm.Minute == 0, "Minute should not advance while paused");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void TestResume()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                tm.Pause();
                tm.AdvanceRealSeconds(5.0f);
                Assert(tm.Minute == 0, "Minute should not advance while paused");

                tm.Resume();
                Assert(!tm.IsPaused, "Expected IsPaused to be false after resume");

                tm.AdvanceRealSeconds(1.0f);
                Assert(tm.Minute == 1, $"Expected Minute 1 after resume, got {tm.Minute}");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void TestDoubleSpeed()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                tm.SetSpeedMultiplier(2.0f);
                Assert(Mathf.Approximately(tm.SpeedMultiplier, 2.0f), "Speed multiplier not set");

                // At 2x speed, 0.5 real seconds should advance 1 game minute
                tm.AdvanceRealSeconds(0.5f);

                Assert(tm.Minute == 1, $"Expected Minute 1 at 2x speed after 0.5s, got {tm.Minute}");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void TestFiveTimesSpeed()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                tm.SetSpeedMultiplier(5.0f);

                // At 5x speed, 0.2 real seconds = 1 game minute
                tm.AdvanceRealSeconds(0.2f);
                Assert(tm.Minute == 1, $"Expected Minute 1 at 5x speed after 0.2s, got {tm.Minute}");

                // 2.0 real seconds = 10 game minutes
                tm.AdvanceRealSeconds(2.0f);
                Assert(tm.Minute == 11, $"Expected Minute 11, got {tm.Minute}");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void TestMidnightDayRollover()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                // Set state directly to Day 1, 23:59
                tm.SetState(new GameTimeState(1, 23, 59));
                Assert(tm.Day == 1 && tm.Hour == 23 && tm.Minute == 59, "Failed to set pre-midnight state");

                bool dayChangedFired = false;
                int newDayValue = -1;
                tm.OnDayChanged += (d) => { dayChangedFired = true; newDayValue = d; };

                // Advance 1 game minute (1 real second)
                tm.AdvanceRealSeconds(1.0f);

                Assert(tm.Day == 2, $"Expected Day 2 after midnight, got {tm.Day}");
                Assert(tm.Hour == 0, $"Expected Hour 00 after midnight, got {tm.Hour}");
                Assert(tm.Minute == 0, $"Expected Minute 00 after midnight, got {tm.Minute}");
                Assert(dayChangedFired, "OnDayChanged event was not fired at midnight");
                Assert(newDayValue == 2, $"Expected OnDayChanged parameter 2, got {newDayValue}");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void TestFrameRateIndependence()
        {
            var (go, tm, config) = CreateTestRig();
            try
            {
                // Simulate 60 small frames of 1/60th second each (total 1.0 real second)
                float frameDelta60 = 1.0f / 60.0f;
                for (int i = 0; i < 60; i++)
                {
                    tm.AdvanceRealSeconds(frameDelta60);
                }

                Assert(tm.Minute == 1, $"Expected Minute 1 after 60 frames of 1/60s, got {tm.Minute}");

                // Simulate 144 small frames of 1/144th second each (total 1.0 real second)
                float frameDelta144 = 1.0f / 144.0f;
                for (int i = 0; i < 144; i++)
                {
                    tm.AdvanceRealSeconds(frameDelta144);
                }

                Assert(tm.Minute == 2, $"Expected Minute 2 after 144 frames of 1/144s, got {tm.Minute}");
            }
            finally
            {
                CleanupTestRig(go, config);
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new System.Exception(message);
            }
        }
    }
}
#endif
