﻿using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GameClient
{
    [HarmonyPatch(typeof(GameDataSaveLoader), "SaveGame", typeof(string))]
    public static class SaveOnlineGame
    {
        [HarmonyPrefix]
        public static bool DoPre(ref string fileName, ref int ___lastSaveTick)
        {
            if (!Network.isConnectedToServer) return true;
            if (ClientValues.currentlySavingGame || ClientValues.currentlySendingSaveToServer) return true;

            ClientValues.currentlySavingGame = true;

            ClientValues.ForcePermadeath();
            ClientValues.ManageDevOptions();
            CustomDifficultyManager.EnforceCustomDifficulty();

            SaveManager.customSaveName = $"Server - {Network.ip} - {ChatManager.username}";
            fileName = SaveManager.customSaveName;

            try
            {
                SafeSaver.Save(GenFilePaths.FilePathForSavedGame(fileName), "savegame", delegate
                {
                    ScribeMetaHeaderUtility.WriteMetaHeader();
                    Game target = Current.Game;
                    Scribe_Deep.Look(ref target, "game");
                }, Find.GameInfo.permadeathMode);
                ___lastSaveTick = Find.TickManager.TicksGame;
            }
            catch (Exception ex) { Log.Error("Exception while saving game: " + ex); }

            MapManager.SendPlayerMapsToServer();
            SaveManager.SendSavePartToServer(fileName);

            ClientValues.currentlySavingGame = false;

            return false;
        }
    }

    [HarmonyPatch(typeof(Autosaver), "AutosaverTick")]
    public static class Autosave
    {
        [HarmonyPrefix]
        public static bool DoPre()
        {
            if (!Network.isConnectedToServer) return true;
            else
            {
                ClientValues.autosaveCurrentTicks++;
                if (ClientValues.autosaveCurrentTicks >= ClientValues.autosaveInternalTicks && !GameDataSaveLoader.SavingIsTemporarilyDisabled)
                {
                    SaveManager.ForceSave();
                }

                return false;
            }
        }
    }
}
