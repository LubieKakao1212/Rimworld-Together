﻿using RimWorld;
using Shared;
using System;
using Verse;
using static Shared.CommonEnumerators;

namespace GameClient
{
    public static class AidManager
    {
        public static void ParsePacket(Packet packet)
        {
            AidData data = Serializer.ConvertBytesToObject<AidData>(packet.contents);

            switch (data.stepMode)
            {
                case AidStepMode.Send:
                    //Empty
                    break;

                case AidStepMode.Receive:
                    ReceiveAidRequest(data);
                    break;

                case AidStepMode.Accept:
                    OnAidAccept();
                    break;

                case AidStepMode.Reject:
                    OnAidReject(data);
                    break;
            }
        }

        private static void ReceiveAidRequest(AidData data)
        {
            Action toDoYes = delegate { AcceptAid(data); };
            Action toDoNo = delegate { RejectAid(data); };

            DialogManager.PushNewDialog(new RT_Dialog_YesNo("You are receiving aid, accept?", toDoYes, toDoNo));
        }

        public static void SendAidRequest()
        {
            AidData aidData = new AidData();
            aidData.stepMode = AidStepMode.Send;
            aidData.fromTile = Find.AnyPlayerHomeMap.Tile;
            aidData.toTile = ClientValues.chosenSettlement.Tile;

            Pawn toGet = RimworldManager.GetAllSettlementPawns(Faction.OfPlayer, false)[DialogManager.dialogButtonListingResultInt];
            aidData.humanData = HumanScribeManager.HumanToString(toGet);
            RimworldManager.RemovePawnFromGame(toGet);

            Packet packet = Packet.CreatePacketFromObject(nameof(PacketHandler.AidPacket), aidData);
            Network.listener.EnqueuePacket(packet);

            DialogManager.PushNewDialog(new RT_Dialog_Wait("Waiting for server response"));
        }

        private static void OnAidAccept()
        {
            DialogManager.PopWaitDialog();

            RimworldManager.GenerateLetter("Sent aid",
                "You have sent aid towards a settlement! The owner will receive the news soon",
                LetterDefOf.PositiveEvent);

            SaveManager.ForceSave();
        }

        private static void OnAidReject(AidData data)
        {
            DialogManager.PopWaitDialog();

            Map map = Find.World.worldObjects.SettlementAt(data.fromTile).Map;
            Pawn pawn = HumanScribeManager.StringToHuman(data.humanData);
            RimworldManager.PlaceThingIntoMap(pawn, map, ThingPlaceMode.Near, true);

            DialogManager.PushNewDialog(new RT_Dialog_Error("Player is not currently available!"));
        }

        private static void AcceptAid(AidData data)
        {
            Map map = Find.World.worldObjects.SettlementAt(data.toTile).Map;
            Pawn pawn = HumanScribeManager.StringToHuman(data.humanData);
            RimworldManager.PlaceThingIntoMap(pawn, map, ThingPlaceMode.Near, true);

            data.stepMode = AidStepMode.Accept;
            Packet packet = Packet.CreatePacketFromObject(nameof(PacketHandler.AidPacket), data);
            Network.listener.EnqueuePacket(packet);

            RimworldManager.GenerateLetter("Received aid",
                "You have received aid from a player! The pawn should come to help soon",
                LetterDefOf.PositiveEvent);

            SaveManager.ForceSave();
        }

        private static void RejectAid(AidData data)
        {
            data.stepMode = AidStepMode.Reject;
            Packet packet = Packet.CreatePacketFromObject(nameof(PacketHandler.AidPacket), data);
            Network.listener.EnqueuePacket(packet);
        }
    }
}
