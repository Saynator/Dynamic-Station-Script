using Newtonsoft.Json;
using Sandbox.Game.EntityComponents;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        //
        //0 is the script tag
        //1 is the sending programmable block
        //2 is the instruction type
        //3 and onwards is data
        //
        //
        public class StationPacket
        {
            public static readonly string ScriptTag = "Cargo";
            public long PBID { get; set; }
            public Instruction Instruction { get; set; }
            public static bool SendPacket(StationPacket packet, IMyProgrammableBlock scriptManager)
            {
                string instructionData = "";
                switch (packet.Instruction.Type)
                {
                    case "Update":
                        instructionData = packet.Instruction.Data2.Count().ToString();
                        break;
                    case "GraphingRequest":
                        foreach (var item in packet.Instruction.Data1)
                        {
                            instructionData += $"{item.SubtypeId};{item.TypeId} ";
                        }
                        break;
                    case "Echo":
                        instructionData = packet.Instruction.DataString;
                        break;
                    case "Return":
                        instructionData = packet.Instruction.DataString;
                        break; //TODO: Finish
                }
                string instructionType = packet.Instruction.Type;
                string arg = $"-isCommand {StationPacket.ScriptTag} {packet.PBID} {instructionType} {instructionData}";
                return scriptManager.TryRun(arg);
            }
        }

        private readonly MyCommandLine _commandLine = new MyCommandLine();
        public class Instruction
        {
            public string Type { get; set; }
            public List<MyItemType> Data1 { get; set; }
            public List<IMyInventoryOwner> Data2 { get; set; }
            public string DataString { get; set; }
        }
        private Dictionary<string, Action> _commands = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase);
        int refineryAmount = 0;
        int assemblerAmount = 0;
        int cargoAmount = 0;
        List<MyItemType> trackedItems = new List<MyItemType>();
        List<IMyInventoryOwner> containers = new List<IMyInventoryOwner>();
        long managerBlockID = 0;
        IMyProgrammableBlock _scriptManager;
        private UpdateType updateSource;

        public Program()
        {
            _commands["Update"] = Update;
            _commands["Echo"] = LocalEcho;
            _commands["Request"] = Request;
            _commands["Command"] = Command;

            if (!String.IsNullOrEmpty(Storage))
            {
                try { managerBlockID = long.Parse(Storage); }
                catch (Exception) { Echo("Invalid Manager ID"); return; }
                _scriptManager = GridTerminalSystem.GetBlockWithId(managerBlockID) as IMyProgrammableBlock;
                Echo("Set Manager to " + _scriptManager.Name);
            }
            else
            {
                _scriptManager = GridTerminalSystem.GetBlockWithName("$SYS Manager") as IMyProgrammableBlock;
                managerBlockID = _scriptManager.GetId();
            }
            _scriptManager.TryRun("SYS$"+ Me.GetId().ToString());
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            Update();
            
        }

        private void Command()
        {
            return;
        }

        private void Request()
        {
            IMyProgrammableBlock pb = GridTerminalSystem.GetBlockWithId(long.Parse(_commandLine.Argument(1))) as IMyProgrammableBlock;
            if (updateSource == UpdateType.Script)
            {
                Runtime.UpdateFrequency |= UpdateFrequency.Once;
                Echo($"{pb.Name} issued a request");
                return;
            }
            Echo("Return:" + _commandLine.Argument(3));
            StationPacket.SendPacket(new StationPacket
            {
                PBID = Me.EntityId,
                Instruction = new Instruction
                {
                    Type = "Return",
                }
            }, pb);
        }

        private void LocalEcho()
        {
            string sender = GridTerminalSystem.GetBlockWithId(long.Parse(_commandLine.Argument(1))).Name; //gets the sender
            Echo($"Incoming Echo from {sender}({_commandLine.Argument(0)}):\n{_commandLine.Argument(3)}");
        }

        public void Save()
        {
            Storage = managerBlockID.ToString();
        }

        //
        //0 is the script tag
        //1 is the sending programmable block
        //2 is the instruction type
        //3 and onwards is data
        //
        public void Main(string argument, UpdateType updateSource)
        {

            this.updateSource = updateSource;
            switch (updateSource) {
                case UpdateType.Script:
                    if (_commandLine.TryParse(argument))
                    {
                        if (!_commandLine.Switch("isCommand")) { BasicCommands(argument, updateSource); } //Executes if the argument isn't an advanced command (packet)
                        Action action;
                        string command = _commandLine.Argument(2);
                        if (command == null)
                        {
                            Echo("No command specified");
                        }
                        else if (_commands.TryGetValue(command, out action))
                        {
                            action();
                        }
                        else
                        {
                            Echo($"Unknown command {command}");
                        }
                        _commandLine.Clear();
                    }
                    break;
                case UpdateType.Terminal:
                    BasicCommands(argument, updateSource);
                    break;
                case UpdateType.Update100:
                    CountResources();
                    break;
                case UpdateType.Once:
                    if (_commandLine.Switch("isCommand")) {
                        StationPacket.SendPacket(new StationPacket
                        {
                            PBID = Me.GetId(),
                            Instruction = new Instruction
                            {
                                Type = "Return",
                                DataString = "Hello World"
                            }
                        }, _scriptManager);
                    }
                    break;
            }
        }

        private void BasicCommands(string argument, UpdateType updateSource)
        {
            switch (argument)
            {
                case "TestTrackItems":
                    trackedItems.Add(MyItemType.MakeIngot("Stone"));
                    trackedItems.Add(MyItemType.MakeIngot("Iron"));
                    trackedItems.Add(MyItemType.MakeIngot("Gold"));
                    trackedItems.Add(MyItemType.MakeIngot("Cobalt"));
                    trackedItems.Add(MyItemType.MakeIngot("Silver"));
                    trackedItems.Add(MyItemType.MakeIngot("Platinum"));
                    trackedItems.Add(MyItemType.MakeIngot("Magnesium"));
                    trackedItems.Add(MyItemType.MakeOre("Stone"));
                    trackedItems.Add(MyItemType.MakeOre("Ice"));
                    break;
                case "TestSend":
                    StationPacket.SendPacket(new StationPacket
                    {
                        PBID = Me.GetId(),
                        Instruction = new Instruction
                        {
                            Type = "Echo",
                            DataString = "\"Hello World\""
                        }
                    }, _scriptManager);
                    break;
            }
        }

        private void CountResources()
        {
            MyFixedPoint[] countingArray = new MyFixedPoint[trackedItems.Count];
            bool notSet = true;
            foreach (var block in containers)
            {
                for (int i = 0; i < block.InventoryCount;i++)
                {
                    IMyInventory inv = block.GetInventory(i);
                    MyFixedPoint[] countingArrayTemp = GetItems(trackedItems, inv).ToArray();
                    for (int j = 0; j < countingArrayTemp.Length;j++) {
                        if(notSet) { countingArray[j] = countingArrayTemp[j]; }
                        countingArray[j] += countingArrayTemp[j];
                    }
                    notSet = false;
                }
            }
            foreach (var item in trackedItems)
            {
                Echo(item.SubtypeId + ": " + countingArray[trackedItems.IndexOf(item)] + "kg");
            }
        }
        private List<MyFixedPoint> GetItems(List<MyItemType> items, IMyInventory inv)
        {
            List<MyFixedPoint> itemAmounts = new List<MyFixedPoint>();
            foreach (var item in items) { itemAmounts.Add(inv.GetItemAmount(item)); } //Echo(item.SubtypeId + ": " + inv.GetItemAmount(item) + "kg\n");
            return itemAmounts; 
        }

        public void Update() //please call when connecting or disconnecting a Grid as well as when any block change is detected by an event controller
        {
            containers.Clear();
            GridTerminalSystem.GetBlocksOfType(containers);
            int oldRefineryAmount = refineryAmount;
            int oldAssemblerAmount = assemblerAmount;
            int oldCargoAmount = cargoAmount;
            refineryAmount = 0;
            assemblerAmount = 0;
            cargoAmount = 0;
            foreach (var block in containers)
            {
                if (block is IMyRefinery){ refineryAmount++; }
                else if(block is IMyAssembler) { assemblerAmount++; }
                else if(block is IMyCargoContainer) { cargoAmount++; }
            }
            Echo("Refineries: " + refineryAmount +
                "\nAssembler: " + assemblerAmount +
                "\nCargo: " + cargoAmount);
        }
    }
}
