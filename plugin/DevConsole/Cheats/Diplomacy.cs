using Amplitude.Mercury.Data.Simulation;
using Amplitude.Mercury.Interop;

namespace DevConsole.Cheats
{
    /// <summary>Relations, quests, and the order that hands you another empire to play.</summary>
    internal static class Diplomacy
    {
        public static void DeclareWar(int otherEmpire) =>
            Orders.Post(new EditorOrderForceWar { LeftEmpireIndex = Sim.LocalEmpireIndex, RightEmpireIndex = otherEmpire },
                        $"war with empire #{otherEmpire}");

        public static void ForcePeace(int otherEmpire) =>
            Orders.Post(new EditorOrderForcePeace { LeftEmpireIndex = Sim.LocalEmpireIndex, RightEmpireIndex = otherEmpire },
                        $"peace with empire #{otherEmpire}");

        public static void AllTreaties(int otherEmpire) =>
            Orders.Post(new EditorOrderForceAllGenericTreaties { LeftEmpireIndex = Sim.LocalEmpireIndex, RightEmpireIndex = otherEmpire },
                        $"all treaties with empire #{otherEmpire}");

        public static void ForceSurrender(int otherEmpire) =>
            Orders.Post(new EditorOrderForceSurrenderOffer { InitiatorEmpireIndex = otherEmpire, OtherEmpireIndex = Sim.LocalEmpireIndex },
                        $"empire #{otherEmpire} offers surrender");

        public static void MeetEverybody() =>
            Orders.Post(new EditorOrderMeetEverybody { EmpireIndex = Sim.LocalEmpireIndex }, "meet everybody");

        public static void ChangeWarScore(int otherEmpire, int delta) =>
            Orders.Post(new EditorOrderChangeGodWarScore
            {
                EmpireIndex = Sim.LocalEmpireIndex,
                OtherEmpireIndex = otherEmpire,
                WarScoreToAdd = delta,
            }, $"war score {delta:+#;-#;0} vs empire #{otherEmpire}");

        public static void SelectVictoryPath(EndGameVictoryPath path) =>
            Orders.Post(new EditorOrderSelectEndGameVictoryPath { EmpireIndex = Sim.LocalEmpireIndex, SelectedPath = path },
                        $"victory path = {path}");

        public static void PacifyMinorEmpires() =>
            Orders.Post(new EditorOrderPacifyAllMinorEmpires(), "pacify all minor empires");

        /// <summary>Hands control to another empire — the console follows, since it always targets the local one.</summary>
        public static void PlayAs(int empireIndex) =>
            Orders.Post(new EditorOrderChangeLocalEmpire(empireIndex), $"play as empire #{empireIndex}");
    }
}
